using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Third person free-roam controller. The character model can be any child of this object.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class SandboxPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float walkSpeed = 4.2f;
    [SerializeField] private float sprintSpeed = 7.4f;
    [SerializeField] private float crouchSpeed = 2.1f;
    [SerializeField] private float swimSpeed = 3.2f;
    [SerializeField] private float acceleration = 24f;
    [SerializeField] private float jumpHeight = 1.35f;
    [SerializeField] private float gravity = -22f;
    [SerializeField] private float interactionRange = 4f;
    [SerializeField] private float coverSearchDistance = 1.65f;

    [Header("Survival")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxArmor = 100f;
    [SerializeField] private float health = 100f;
    [SerializeField] private float armor;
    [SerializeField] private int cash = 2500;
    [SerializeField] private float waterLevel = 0f;
    [SerializeField] private float lungCapacitySeconds = 24f;
    [SerializeField] private float respawnDelay = 2.5f;

    private CharacterController characterController;
    private readonly Collider[] standingHits = new Collider[16];
    private SandboxCamera cameraRig;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float lungRemaining;
    private bool dead;
    private bool swimming;
    private bool crouched;
    private bool stealth;
    private bool inCover;
    private bool lowCover;
    private bool parachuteDeployed;
    private Vector3 coverNormal;
    private Vector3 coverWallPoint;
    private GameObject parachuteVisual;
    private Mesh parachuteMesh;
    private Material parachuteMaterial;
    private bool wasGrounded;
    private Vector3 defaultRespawnPoint;
    private bool mounted;
    private Renderer[] renderers;
    private Vector3 originalVisualScale;
    private Coroutine respawnRoutine;

    public static SandboxPlayer Instance { get; private set; }
    public SandboxVehicle CurrentVehicle { get; private set; }
    public bool IsInVehicle => mounted && CurrentVehicle != null;
    public bool IsDead => dead;
    public bool IsSwimming => swimming;
    public bool IsCrouched => crouched;
    public bool IsStealth => stealth;
    public bool IsInCover => inCover;
    public bool IsLowCover => inCover && lowCover;
    public bool HasParachute { get; set; }
    public bool ParachuteDeployed => parachuteDeployed;
    public float NoiseRadius => IsInVehicle ? 28f : stealth || crouched ? 2.5f : horizontalVelocity.magnitude > walkSpeed + 0.5f ? 18f : horizontalVelocity.magnitude > 0.5f ? 8f : 1f;
    public bool InputEnabled { get; set; } = true;
    public bool Invulnerable { get; set; }
    public bool Scuba { get; set; }
    public float Health { get => health; set { health = Mathf.Clamp(value, 0f, maxHealth); if (health <= 0f && !dead) Die(); } }
    public float Armor { get => armor; set => armor = Mathf.Clamp(value, 0f, maxArmor); }
    public int Cash { get => cash; set => cash = Mathf.Max(0, value); }
    public float MaxHealth => maxHealth;
    public float MaxArmor => maxArmor;
    public float LungRemaining => lungRemaining;
    public Vector3 DefaultRespawnPoint { get => defaultRespawnPoint; set => defaultRespawnPoint = value; }
    public float WaterLevel { get => waterLevel; set => waterLevel = value; }
    public SandboxCamera CameraRig { get => cameraRig; set => cameraRig = value; }
    public Transform VisualRoot { get => visualRoot; set { visualRoot = value; originalVisualScale = value != null ? value.localScale : Vector3.one; renderers = GetComponentsInChildren<Renderer>(true); } }

    public event Action<SandboxPlayer> Died;
    public event Action<SandboxPlayer> Respawned;
    public event Action<SandboxVehicle> VehicleEntered;
    public event Action<SandboxVehicle> VehicleExited;

    private void Awake()
    {
        Instance = this;
        characterController = GetComponent<CharacterController>();
        characterController.height = Mathf.Max(1.6f, characterController.height);
        characterController.radius = Mathf.Clamp(characterController.radius, 0.25f, 0.48f);
        characterController.center = Vector3.up * characterController.height * 0.5f;
        characterController.stepOffset = Mathf.Min(0.35f, characterController.height * 0.25f);
        defaultRespawnPoint = transform.position;
        if (visualRoot == null && transform.childCount > 0) visualRoot = transform.GetChild(0);
        renderers = GetComponentsInChildren<Renderer>(true);
        if (visualRoot != null) originalVisualScale = visualRoot.localScale;
        lungRemaining = lungCapacitySeconds;
        health = Mathf.Clamp(health, 1f, maxHealth);
        armor = Mathf.Clamp(armor, 0f, maxArmor);
        cameraRig = cameraRig != null ? cameraRig : FindAnyObjectByType<SandboxCamera>();
        if (GetComponent<SandboxLimbAnimator>() == null) gameObject.AddComponent<SandboxLimbAnimator>();
    }

    private void Start()
    {
        if (cameraRig != null && cameraRig.TargetPlayer == null) cameraRig.TargetPlayer = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (parachuteMesh != null) Destroy(parachuteMesh);
        if (parachuteMaterial != null) Destroy(parachuteMaterial);
    }

    private void Update()
    {
        if (dead) return;
        if (mounted && CurrentVehicle == null)
        {
            mounted = false;
            CurrentVehicle = null;
            foreach (Renderer renderer in renderers) if (renderer != null) renderer.enabled = true;
            characterController.enabled = true;
            Respawn(defaultRespawnPoint);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && InputEnabled && keyboard.fKey.wasPressedThisFrame)
        {
            if (IsInVehicle) ExitVehicle();
            else EnterNearestVehicle();
        }

        if (!IsInVehicle && keyboard != null && InputEnabled)
        {
            if (keyboard.zKey.wasPressedThisFrame) SetStealth(!stealth);
            if (keyboard.qKey.wasPressedThisFrame) ToggleCover();
            if (keyboard.pKey.wasPressedThisFrame) DeployParachute();
        }

        if (IsInVehicle)
        {
            Transform seat = CurrentVehicle.DriverSeat != null ? CurrentVehicle.DriverSeat : CurrentVehicle.transform;
            transform.position = seat.position + (CurrentVehicle.DriverSeat == null ? Vector3.up * 1.1f : Vector3.zero);
            transform.rotation = seat.rotation;
            if (!CurrentVehicle.IsDestroyed) CurrentVehicle.SetPlayerInputEnabled(InputEnabled);
            return;
        }

        if (characterController == null || !characterController.enabled) return;
        MoveCharacter(keyboard);
    }

    private void MoveCharacter(Keyboard keyboard)
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        Vector2 input = Vector2.zero;
        if (InputEnabled && keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        }
        input = Vector2.ClampMagnitude(input, 1f);

        bool groundedBeforeMove = characterController.isGrounded;
        swimming = transform.position.y < waterLevel - 0.18f && !groundedBeforeMove;
        if (swimming && inCover) LeaveCover();
        if (swimming && parachuteDeployed) RetractParachute();
        bool wantsCrouch = !swimming && ((InputEnabled && keyboard != null && (keyboard.cKey.isPressed || keyboard.leftCtrlKey.isPressed)) || stealth || inCover && lowCover);
        UpdateCrouch(wantsCrouch, dt);

        Vector3 forward = cameraRig != null ? cameraRig.transform.forward : Vector3.forward;
        Vector3 right = cameraRig != null ? cameraRig.transform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        Vector3 moveDirection = (forward * input.y + right * input.x).normalized;
        if (inCover)
        {
            if (keyboard != null && InputEnabled && (keyboard.sKey.isPressed || keyboard.spaceKey.wasPressedThisFrame)) LeaveCover();
            else
            {
                Vector3 tangent = Vector3.Cross(coverNormal, Vector3.up).normalized;
                moveDirection = tangent * input.x;
                input.y = 0f;
                if (!RefreshCover()) LeaveCover();
            }
        }
        bool sprint = InputEnabled && keyboard != null && keyboard.leftShiftKey.isPressed && !crouched;
        float speed = parachuteDeployed ? 5f : swimming ? swimSpeed : inCover ? 2.5f : crouched ? crouchSpeed : stealth ? walkSpeed * 0.58f : sprint ? sprintSpeed : walkSpeed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, moveDirection * (speed * input.magnitude), acceleration * dt);

        if (swimming)
        {
            float swimVertical = 0f;
            if (InputEnabled && keyboard != null)
            {
                if (keyboard.spaceKey.isPressed) swimVertical += 1f;
                if (keyboard.leftCtrlKey.isPressed) swimVertical -= 1f;
            }
            float surfaceFeetHeight = waterLevel - 0.55f;
            if (swimVertical == 0f) swimVertical = Mathf.Clamp((surfaceFeetHeight - transform.position.y) * 2f, -0.7f, 0.7f);
            verticalVelocity = swimVertical * swimSpeed;
            bool underwater = transform.position.y < waterLevel - 1.45f;
            lungRemaining = underwater && !Scuba ? Mathf.Max(0f, lungRemaining - dt) : Mathf.Min(lungCapacitySeconds, lungRemaining + dt * 3f);
            if (lungRemaining <= 0f) Damage(8f * dt);
        }
        else if (parachuteDeployed)
        {
            float descent = InputEnabled && keyboard != null && keyboard.spaceKey.isPressed ? -2.5f : -5.4f;
            verticalVelocity = Mathf.MoveTowards(verticalVelocity, descent, 20f * dt);
            lungRemaining = Mathf.Min(lungCapacitySeconds, lungRemaining + dt * 3f);
        }
        else
        {
            lungRemaining = Mathf.Min(lungCapacitySeconds, lungRemaining + dt * 3f);
            if (groundedBeforeMove && verticalVelocity < 0f) verticalVelocity = -2f;
            if (groundedBeforeMove && InputEnabled && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * dt;
        }

        Vector3 frameMotion = (horizontalVelocity + Vector3.up * verticalVelocity) * dt;
        if (inCover) frameMotion += CoverSpacingCorrection(dt);
        characterController.Move(frameMotion);
        if (!wasGrounded && characterController.isGrounded && verticalVelocity < -14f)
            Damage((Mathf.Abs(verticalVelocity) - 14f) * 3.5f);
        if (characterController.isGrounded && parachuteDeployed) RetractParachute();
        wasGrounded = characterController.isGrounded;

        if (visualRoot != null && (inCover || moveDirection.sqrMagnitude > 0.01f))
        {
            Quaternion target = Quaternion.LookRotation(inCover ? -coverNormal : moveDirection, Vector3.up);
            visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, target, 1f - Mathf.Exp(-12f * dt));
        }
        if (transform.position.y < waterLevel - 12f && !swimming) Damage(5f * dt);
    }

    private void UpdateCrouch(bool requested, float dt)
    {
        if (!requested && crouched)
        {
            Vector3 feet = transform.position + Vector3.up * characterController.radius;
            Vector3 head = transform.position + Vector3.up * (1.9f - characterController.radius);
            int count = Physics.OverlapCapsuleNonAlloc(feet, head, characterController.radius * 0.9f, standingHits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (standingHits[i] != null && !standingHits[i].transform.IsChildOf(transform)) { requested = true; break; }
        }
        crouched = requested;
        float targetHeight = crouched ? 1.1f : 1.9f;
        characterController.height = Mathf.MoveTowards(characterController.height, targetHeight, dt * 5f);
        characterController.center = Vector3.up * characterController.height * 0.5f;
        if (visualRoot != null)
        {
            Vector3 targetScale = originalVisualScale;
            if (crouched) targetScale.y *= 0.72f;
            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale, 1f - Mathf.Exp(-12f * dt));
        }
    }

    private void EnterNearestVehicle()
    {
        SandboxVehicle closest = null;
        float bestDistance = interactionRange * interactionRange;
        foreach (SandboxVehicle vehicle in SandboxVehicle.All)
        {
            if (vehicle == null || vehicle.IsDestroyed || vehicle.Driver != null) continue;
            float distance = (vehicle.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance) { bestDistance = distance; closest = vehicle; }
        }
        if (closest != null) EnterVehicle(closest);
    }

    public bool EnterVehicle(SandboxVehicle vehicle)
    {
        if (dead || IsInVehicle || vehicle == null || !vehicle.TryEnter(this)) return false;
        LeaveCover();
        RetractParachute();
        if (vehicle.Kind == SandboxVehicle.VehicleKind.Helicopter || vehicle.Kind == SandboxVehicle.VehicleKind.Plane) HasParachute = true;
        CurrentVehicle = vehicle;
        mounted = true;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        swimming = false;
        characterController.enabled = false;
        foreach (Renderer renderer in renderers) if (renderer != null) renderer.enabled = false;
        Transform seat = vehicle.DriverSeat != null ? vehicle.DriverSeat : vehicle.transform;
        transform.position = seat.position + (vehicle.DriverSeat == null ? Vector3.up * 1.1f : Vector3.zero);
        transform.rotation = seat.rotation;
        VehicleEntered?.Invoke(vehicle);
        return true;
    }

    public void ExitVehicle()
    {
        SandboxVehicle vehicle = CurrentVehicle;
        if (vehicle == null) return;
        Vector3 exitPosition = vehicle.GetSafeExitPosition();
        transform.position = exitPosition;
        transform.rotation = Quaternion.Euler(0f, vehicle.transform.eulerAngles.y, 0f);
        CurrentVehicle = null;
        mounted = false;
        vehicle.ReleaseDriver(this);
        foreach (Renderer renderer in renderers) if (renderer != null) renderer.enabled = true;
        characterController.enabled = true;
        verticalVelocity = -2f;
        VehicleExited?.Invoke(vehicle);
    }

    public void Damage(float amount)
    {
        if (dead || Invulnerable || amount <= 0f) return;
        float armored = Mathf.Min(armor, amount);
        armor -= armored;
        health = Mathf.Max(0f, health - (amount - armored));
        if (health <= 0f) Die();
    }

    private void Die()
    {
        if (dead) return;
        dead = true;
        LeaveCover();
        RetractParachute();
        if (IsInVehicle) ExitVehicle();
        Died?.Invoke(this);
        if (respawnRoutine != null) StopCoroutine(respawnRoutine);
        respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSecondsRealtime(respawnDelay);
        Respawn(defaultRespawnPoint);
    }

    public void Respawn(Vector3 position)
    {
        if (respawnRoutine != null) { StopCoroutine(respawnRoutine); respawnRoutine = null; }
        if (IsInVehicle) ExitVehicle();
        dead = false;
        health = maxHealth;
        armor = 0f;
        lungRemaining = lungCapacitySeconds;
        swimming = false;
        LeaveCover();
        RetractParachute();
        Teleport(position);
        Respawned?.Invoke(this);
    }

    public void Teleport(Vector3 position)
    {
        if (IsInVehicle) ExitVehicle();
        LeaveCover();
        RetractParachute();
        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled) characterController.enabled = false;
        transform.position = position;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = -2f;
        if (wasEnabled) characterController.enabled = true;
        if (cameraRig != null) cameraRig.SnapToTarget();
    }

    public void RefillLungs() => lungRemaining = lungCapacitySeconds;

    public void SetStealth(bool enabled)
    {
        stealth = enabled;
        if (enabled && inCover) LeaveCover();
    }

    public bool ToggleCover()
    {
        if (inCover) { LeaveCover(); return false; }
        if (dead || IsInVehicle || swimming || parachuteDeployed) return false;
        Vector3 direction = cameraRig != null ? cameraRig.transform.forward : visualRoot != null ? visualRoot.forward : transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return false;
        Vector3 origin = transform.position + Vector3.up * 1.05f;
        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, coverSearchDistance, ~0, QueryTriggerInteraction.Ignore)
            || Mathf.Abs(hit.normal.y) > 0.35f || hit.collider.transform.IsChildOf(transform)) return false;
        inCover = true;
        lowCover = hit.collider.bounds.max.y < transform.position.y + 1.45f;
        coverNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
        coverWallPoint = hit.point;
        stealth = false;
        return true;
    }

    public void LeaveCover()
    {
        inCover = false;
        lowCover = false;
    }

    private bool RefreshCover()
    {
        Vector3 origin = transform.position + Vector3.up * (lowCover ? 0.7f : 1.05f) + coverNormal * 0.3f;
        if (!Physics.Raycast(origin, -coverNormal, out RaycastHit hit, coverSearchDistance + 0.55f, ~0, QueryTriggerInteraction.Ignore)
            || Mathf.Abs(hit.normal.y) > 0.4f || hit.collider.transform.IsChildOf(transform)) return false;
        coverNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
        coverWallPoint = hit.point;
        return true;
    }

    private Vector3 CoverSpacingCorrection(float dt)
    {
        Vector3 desired = coverWallPoint + coverNormal * 0.55f;
        desired.y = transform.position.y;
        return Vector3.ClampMagnitude(desired - transform.position, dt * 2.5f);
    }

    public void GiveParachute() => HasParachute = true;

    public bool DeployParachute()
    {
        if (!HasParachute || parachuteDeployed || dead || IsInVehicle || swimming || characterController.isGrounded || verticalVelocity > -2.5f)
            return false;
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit ground, 150f, ~0, QueryTriggerInteraction.Ignore)
            && ground.distance < 3.5f) return false;
        LeaveCover();
        parachuteDeployed = true;
        HasParachute = false;
        if (parachuteVisual == null) BuildParachuteVisual();
        if (parachuteVisual != null) parachuteVisual.SetActive(true);
        return true;
    }

    public void RetractParachute()
    {
        parachuteDeployed = false;
        if (parachuteVisual != null) parachuteVisual.SetActive(false);
    }

    private void BuildParachuteVisual()
    {
        const int segments = 20;
        const int rings = 5;
        parachuteVisual = new GameObject("Aerial canopy | teal");
        parachuteVisual.transform.SetParent(transform, false);
        MeshFilter filter = parachuteVisual.AddComponent<MeshFilter>();
        MeshRenderer renderer = parachuteVisual.AddComponent<MeshRenderer>();
        Vector3[] vertices = new Vector3[(rings + 1) * (segments + 1)];
        int[] triangles = new int[rings * segments * 6];
        for (int ring = 0; ring <= rings; ring++)
        {
            float angle = Mathf.PI * 0.5f * ring / rings;
            float radius = 1.75f * Mathf.Sin(angle);
            float y = 3.15f + 1.05f * Mathf.Cos(angle);
            for (int segment = 0; segment <= segments; segment++)
            {
                float a = segment * Mathf.PI * 2f / segments;
                vertices[ring * (segments + 1) + segment] = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
            }
        }
        int index = 0;
        for (int ring = 0; ring < rings; ring++)
        for (int segment = 0; segment < segments; segment++)
        {
            int a = ring * (segments + 1) + segment;
            int b = a + 1;
            int c = a + segments + 1;
            int d = c + 1;
            triangles[index++] = a; triangles[index++] = b; triangles[index++] = c;
            triangles[index++] = b; triangles[index++] = d; triangles[index++] = c;
        }
        parachuteMesh = new Mesh { name = "Teal segmented canopy" };
        parachuteMesh.vertices = vertices;
        parachuteMesh.triangles = triangles;
        parachuteMesh.RecalculateNormals();
        filter.sharedMesh = parachuteMesh;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        if (shader == null) return;
        parachuteMaterial = new Material(shader) { name = "Teal canopy material", color = new Color(0.16f, 0.78f, 0.72f) };
        renderer.sharedMaterial = parachuteMaterial;
        for (int i = 0; i < 8; i++)
        {
            GameObject cord = new GameObject("Canopy cord " + i);
            cord.transform.SetParent(parachuteVisual.transform, false);
            LineRenderer line = cord.AddComponent<LineRenderer>();
            line.sharedMaterial = parachuteMaterial;
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = 0.018f;
            line.endWidth = 0.018f;
            float angle = i * Mathf.PI * 2f / 8f;
            line.SetPosition(0, new Vector3(Mathf.Cos(angle) * 1.75f, 3.15f, Mathf.Sin(angle) * 1.75f));
            line.SetPosition(1, new Vector3(Mathf.Cos(angle) * 0.19f, 1.1f, Mathf.Sin(angle) * 0.16f));
        }
    }
}
