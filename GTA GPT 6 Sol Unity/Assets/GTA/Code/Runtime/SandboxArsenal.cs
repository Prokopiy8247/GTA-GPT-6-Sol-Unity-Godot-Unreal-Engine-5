using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Inventory, combat, ammunition and weapon switching for free roam.</summary>
[RequireComponent(typeof(SandboxPlayer))]
public class SandboxArsenal : MonoBehaviour
{
    public enum Weapon { Fists, Baton, Pistol, HeavyPistol, SMG, Shotgun, Rifle, Marksman, Sniper, Grenade, Rocket }
    [System.Serializable] public struct WeaponDefinition
    {
        public string Name;
        public int Magazine;
        public float Damage, Range, Interval, Spread;
        public bool Automatic;
        public WeaponDefinition(string name, int magazine, float damage, float range, float interval, float spread, bool automatic)
        { Name = name; Magazine = magazine; Damage = damage; Range = range; Interval = interval; Spread = spread; Automatic = automatic; }
    }
    public static readonly WeaponDefinition[] Definitions =
    {
        new WeaponDefinition("Fists", 0, 18, 2.2f, .55f, 0, false),
        new WeaponDefinition("Baton", 0, 28, 2.8f, .7f, 0, false),
        new WeaponDefinition("Sidearm", 15, 24, 85, .22f, .02f, false),
        new WeaponDefinition("Longshot", 8, 38, 95, .43f, .016f, false),
        new WeaponDefinition("Vector SMG", 30, 13, 70, .09f, .043f, true),
        new WeaponDefinition("Dock Shotgun", 8, 10, 48, .72f, .13f, false),
        new WeaponDefinition("Civic Rifle", 30, 21, 130, .12f, .023f, true),
        new WeaponDefinition("Scout Rifle", 12, 40, 180, .42f, .014f, false),
        new WeaponDefinition("Surveyor", 5, 75, 260, 1.15f, .003f, false),
        new WeaponDefinition("Grenade", 1, 75, 25, 1.1f, 0, false),
        new WeaponDefinition("Launcher", 1, 100, 160, 1.6f, 0, false)
    };

    public bool[] Owned = new bool[Definitions.Length];
    public int[] Loaded = new int[Definitions.Length];
    public int[] Reserve = new int[Definitions.Length];
    public Weapon Selected = Weapon.Pistol;
    public bool IsReloading { get; private set; }
    public int SelectedIndex => (int)Selected;
    public string SelectedName => Definitions[SelectedIndex].Name;
    public int CurrentAmmo => Loaded[SelectedIndex];
    public int ReserveAmmo => Reserve[SelectedIndex];
    public bool Suppressor, Grip, ExtendedMagazine, Scope;

    SandboxPlayer player;
    float nextShot, reloadEnd;
    GameObject pistolVisual, carbineVisual, smgVisual, shotgunVisual;

    void Awake()
    {
        player = GetComponent<SandboxPlayer>();
        if (!Owned[0])
        {
            Owned[0] = Owned[2] = true;
            Loaded[2] = Definitions[2].Magazine;
            Reserve[2] = 90;
        }
    }

    void Start()
    {
        foreach (Transform piece in GetComponentsInChildren<Transform>(true))
        {
            if (piece.name == "Equipped Pistol Visual") pistolVisual = piece.gameObject;
            else if (piece.name == "Stowed Carbine Visual") carbineVisual = piece.gameObject;
            else if (piece.name == "Stowed SMG Visual") smgVisual = piece.gameObject;
            else if (piece.name == "Stowed Shotgun Visual") shotgunVisual = piece.gameObject;
        }
        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (pistolVisual != null) pistolVisual.SetActive(Selected == Weapon.Pistol || Selected == Weapon.HeavyPistol);
        if (smgVisual != null) smgVisual.SetActive(Selected == Weapon.SMG);
        if (shotgunVisual != null) shotgunVisual.SetActive(Selected == Weapon.Shotgun);
        if (carbineVisual != null) carbineVisual.SetActive(Selected == Weapon.Rifle || Selected == Weapon.Marksman || Selected == Weapon.Sniper);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || player == null || !player.InputEnabled) return;
        if (IsReloading && Time.time >= reloadEnd) CompleteReload();
        if (keyboard.digit1Key.wasPressedThisFrame) Select(0);
        if (keyboard.digit2Key.wasPressedThisFrame) Select(2);
        if (keyboard.digit3Key.wasPressedThisFrame) Select(4);
        if (keyboard.digit4Key.wasPressedThisFrame) Select(5);
        if (keyboard.digit5Key.wasPressedThisFrame) Select(6);
        if (keyboard.digit6Key.wasPressedThisFrame) Select(8);
        if (keyboard.digit7Key.wasPressedThisFrame) Select(9);
        if (keyboard.digit8Key.wasPressedThisFrame) Select(10);
        if (keyboard.rKey.wasPressedThisFrame) Reload();
        if (Mouse.current != null && Mouse.current.scroll.ReadValue().y != 0f)
            Cycle(Mouse.current.scroll.ReadValue().y > 0f ? 1 : -1);
        if (Mouse.current == null || IsReloading) return;
        bool fire = Definitions[SelectedIndex].Automatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame;
        if (fire && Time.time >= nextShot) Fire();
    }

    public void Select(int index)
    {
        if (index >= 0 && index < Owned.Length && Owned[index]) { Selected = (Weapon)index; IsReloading = false; RefreshVisual(); }
    }
    public void Cycle(int direction)
    {
        for (int i = 1; i <= Owned.Length; i++)
        {
            int candidate = (SelectedIndex + direction * i + Owned.Length * 2) % Owned.Length;
            if (Owned[candidate]) { Select(candidate); return; }
        }
    }
    public void Give(Weapon weapon, int ammo = 90)
    {
        int index = (int)weapon;
        Owned[index] = true;
        Loaded[index] = Definitions[index].Magazine;
        Reserve[index] += ammo;
        Select(index);
    }
    public void RefillAll()
    {
        for (int i = 0; i < Owned.Length; i++) if (Owned[i]) { Loaded[i] = Definitions[i].Magazine; Reserve[i] = Mathf.Max(Reserve[i], 240); }
    }
    public void Reload()
    {
        int i = SelectedIndex;
        if (Definitions[i].Magazine <= 0 || Loaded[i] >= MagazineCapacity(i) || Reserve[i] <= 0 || IsReloading) return;
        IsReloading = true;
        reloadEnd = Time.time + (i == 5 || i == 8 ? 1.8f : 1.15f);
        SandboxFX.Tone(transform.position, "reload", 450f, .11f, .08f);
    }
    int MagazineCapacity(int i) => Definitions[i].Magazine + (ExtendedMagazine && Definitions[i].Magazine > 1 ? Mathf.CeilToInt(Definitions[i].Magazine * .4f) : 0);
    void CompleteReload()
    {
        int i = SelectedIndex, amount = Mathf.Min(MagazineCapacity(i) - Loaded[i], Reserve[i]);
        Loaded[i] += amount;
        Reserve[i] -= amount;
        IsReloading = false;
    }

    void Fire()
    {
        int i = SelectedIndex;
        if (player.IsInVehicle && i != 0 && i != 2 && i != 3 && i != 4 && i != 9) return;
        if (i > 1 && Loaded[i] <= 0) { Reload(); return; }
        nextShot = Time.time + Definitions[i].Interval;
        if (i > 1) Loaded[i]--;
        if (SandboxDirector.Instance != null)
        {
            int skill = i <= 1 ? 2 : 1;
            SandboxDirector.Instance.Skills[skill] = Mathf.Min(100f, SandboxDirector.Instance.Skills[skill] + (i <= 1 ? .08f : .045f));
        }
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 origin = player.IsInVehicle ? player.transform.position + Vector3.up * 1.3f : player.transform.position + Vector3.up * 1.45f;
        Vector3 direction = cam.transform.forward;
        if (i <= 1)
        {
            if (Physics.SphereCast(origin, .38f, direction, out var meleeHit, Definitions[i].Range))
            {
                var ped = meleeHit.collider.GetComponentInParent<SandboxPedestrian>();
                if (ped != null) ped.TakeDamage(Definitions[i].Damage, meleeHit.point);
            }
            SandboxFX.Tone(origin, "melee", 180f, .1f, .08f);
            return;
        }
        float spread = Definitions[i].Spread * (Grip ? .7f : 1f) * (player.IsInVehicle ? 1.9f : 1f);
        direction = (direction + cam.transform.right * Random.Range(-spread, spread) + cam.transform.up * Random.Range(-spread, spread)).normalized;
        if (i == 9 || i == 10) { StartCoroutine(ExplosiveProjectile(origin, direction, i == 9)); return; }
        SandboxFX.Muzzle(origin + direction * .35f, direction);
        int pelletCount = i == 5 ? 8 : 1;
        for (int pellet = 0; pellet < pelletCount; pellet++)
        {
            Vector3 shotDir = pelletCount > 1 ? (direction + cam.transform.right * Random.Range(-.09f, .09f) + cam.transform.up * Random.Range(-.09f, .09f)).normalized : direction;
            var hits = Physics.RaycastAll(cam.transform.position, shotDir, Definitions[i].Range, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<SandboxPlayer>() == player) continue;
                var ped = hit.collider.GetComponentInParent<SandboxPedestrian>();
                if (ped != null) ped.TakeDamage(Definitions[i].Damage * (hit.collider.name.ToLowerInvariant().Contains("head") ? 1.7f : 1f), hit.point);
                var vehicle = hit.collider.GetComponentInParent<SandboxVehicle>();
                if (vehicle != null) vehicle.Damage(Definitions[i].Damage * .55f);
                if (hit.rigidbody != null) hit.rigidbody.AddForceAtPosition(shotDir * 90f, hit.point);
                SandboxFX.Impact(hit.point, vehicle != null ? Color.yellow : new Color(.65f, .62f, .5f), .1f);
                break;
            }
        }
        if (SandboxDirector.Instance != null) SandboxDirector.Instance.ReportGunshot(origin, Suppressor ? 12f : 55f);
    }

    IEnumerator ExplosiveProjectile(Vector3 origin, Vector3 direction, bool grenade)
    {
        var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = grenade ? "Thrown grenade" : "Rocket";
        shell.transform.position = origin + direction;
        shell.transform.localScale = Vector3.one * (grenade ? .18f : .26f);
        var collider = shell.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;
        float elapsed = 0f;
        Vector3 velocity = direction * (grenade ? 17f : 70f);
        while (elapsed < (grenade ? 2.3f : 2.7f))
        {
            Vector3 before = shell.transform.position;
            velocity += Vector3.down * (grenade ? 14f : 2f) * Time.deltaTime;
            Vector3 step = velocity * Time.deltaTime;
            if (Physics.Raycast(before, step.normalized, out var hit, step.magnitude) && hit.collider.GetComponentInParent<SandboxPlayer>() != player)
            { shell.transform.position = hit.point; break; }
            shell.transform.position += step;
            elapsed += Time.deltaTime;
            yield return null;
        }
        SandboxFX.Explosion(shell.transform.position, grenade ? 6f : 8f, grenade ? 70f : 100f);
        Destroy(shell);
    }
}
