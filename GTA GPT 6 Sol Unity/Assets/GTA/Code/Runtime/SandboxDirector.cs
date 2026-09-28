using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Owns free-roam simulation, crime reporting, search, weather, services and persistence.</summary>
public class SandboxDirector : MonoBehaviour
{
    [Serializable] class SaveData
    {
        public Vector3 position;
        public float health, armor, hour, heat;
        public int cash;
        public int weather, selectedWeapon;
        public bool[] owned;
        public int[] loaded, reserve;
        public float[] skills;
    }

    public enum WeatherState { Clear, Cloudy, Rain, Fog, Storm }
    public static SandboxDirector Instance { get; private set; }
    public SandboxPlayer Player { get; private set; }
    public int WantedLevel { get; private set; }
    public bool InPursuit { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public bool TrafficEnabled = true;
    public bool PedestriansEnabled = true;
    public bool WildlifeEnabled = true;
    public float Hour = 13.4f;
    public WeatherState Weather = WeatherState.Clear;
    public float[] Skills = new float[7]; // stamina, shooting, strength, stealth, driving, flying, lung
    public SandboxLocation ActiveLocation { get; private set; }
    public string Toast { get; private set; }
    public float ToastUntil { get; private set; }
    public IReadOnlyList<SandboxVehicle> VehicleCatalog => vehicleCatalog;

    readonly List<SandboxVehicle> vehicleCatalog = new List<SandboxVehicle>();
    readonly List<SandboxPedestrian> police = new List<SandboxPedestrian>();
    readonly List<SandboxPedestrian> civilians = new List<SandboxPedestrian>();
    readonly List<SandboxTrafficAgent> traffic = new List<SandboxTrafficAgent>();
    readonly List<Light> nightLights = new List<Light>();
    float heat, lastSeen, nextPoliceSpawn, nextCheck, nextTrafficSpawn, nextVisualUpdate, nextAutosave, crimePendingUntil, crimePendingAmount;
    SandboxPedestrian policeTemplate;
    SandboxPedestrian civilianTemplate;
    Light sun;
    ParticleSystem rain;
    Transform[] route;
    Transform roadsRoot;
    CharacterController playerController;
    Vector3 lastGroundedPosition;
    bool smokeCaptureRun, smokeSaveTest;
    Vector3 respawnPoint = new Vector3(42f, 1.5f, 110f);
    string SavePath => smokeSaveTest
        ? Path.Combine(Directory.GetCurrentDirectory(), ".sol-run", "harborline-smoke-save.json")
        : Path.Combine(Application.persistentDataPath, "harborline-save.json");

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        smokeCaptureRun = Array.Exists(Environment.GetCommandLineArgs(),
            argument => string.Equals(argument, "-smokecapture", StringComparison.OrdinalIgnoreCase));
        smokeSaveTest = smokeCaptureRun && Array.Exists(Environment.GetCommandLineArgs(),
            argument => string.Equals(argument, "-smokesave", StringComparison.OrdinalIgnoreCase));
    }

    void Start()
    {
        Player = FindFirstObjectByType<SandboxPlayer>();
        sun = RenderSettings.sun;
        if (sun == null)
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) { sun = light; break; }
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.name == "Night pool") nightLights.Add(light);
        if (Player != null)
        {
            playerController = Player.GetComponent<CharacterController>();
            lastGroundedPosition = Player.transform.position;
            Player.DefaultRespawnPoint = respawnPoint;
            Player.Respawned += _ => SetWantedLevel(0);
            Player.VehicleEntered += vehicle => { if (vehicle.IsPolice) ReportCrime(38f, vehicle.transform.position, true); };
        }
        foreach (var actor in FindObjectsByType<SandboxPedestrian>(FindObjectsSortMode.None))
        {
            if (actor.Role == SandboxPedestrian.ActorRole.Police || actor.Role == SandboxPedestrian.ActorRole.Tactical)
            { police.Add(actor); if (policeTemplate == null) policeTemplate = actor; }
            else { civilians.Add(actor); if (civilianTemplate == null) civilianTemplate = actor; }
        }
        foreach (var vehicle in FindObjectsByType<SandboxVehicle>(FindObjectsSortMode.None))
            if (!vehicleCatalog.Contains(vehicle)) { vehicleCatalog.Add(vehicle); WireVehicle(vehicle); }
        traffic.AddRange(FindObjectsByType<SandboxTrafficAgent>(FindObjectsSortMode.None));
        roadsRoot = GameObject.Find("Roads and Promenade")?.transform;
        Transform trafficRoot = GameObject.Find("Traffic Routes")?.transform;
        if (trafficRoot != null && trafficRoot.childCount > 0)
        {
            Transform loop = trafficRoot.GetChild(0);
            route = new Transform[loop.childCount];
            for (int i = 0; i < route.Length; i++) route[i] = loop.GetChild(i);
        }
        foreach (var agent in traffic) if (agent.Waypoints == null || agent.Waypoints.Length == 0) agent.Waypoints = route;
        SetupRain();
        if (!smokeCaptureRun) LoadGame(false);
        SetWeather(Weather);
        ShowToast("HARBORLINE  •  FREE ROAM   |   F1: SANDBOX MENU", 6f);
    }

    void Update()
    {
        if (Player == null) return;
        float dt = Time.deltaTime;
        Hour = (Hour + dt / 105f) % 24f;
        if (Time.time > nextVisualUpdate) { nextVisualUpdate = Time.time + .35f; UpdateSky(); }
        if (!Player.IsInVehicle && playerController != null && playerController.isGrounded)
            lastGroundedPosition = Player.transform.position;
        if (Player.InputEnabled && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && ActiveLocation == null)
            TryInteract();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && ActiveLocation != null) ActiveLocation = null;
        if (crimePendingUntil > 0f && Time.time > crimePendingUntil)
        {
            crimePendingUntil = 0f;
            ReportCrime(crimePendingAmount, Player.transform.position, true);
            crimePendingAmount = 0f;
        }
        if (Time.time > nextCheck) { nextCheck = Time.time + .55f; UpdateWanted(); UpdatePopulation(); }
        if (!smokeCaptureRun && Time.time > nextAutosave) { nextAutosave = Time.time + 90f; SaveGame(); }
        if (Player.IsInVehicle)
        {
            int index = Player.CurrentVehicle.Kind == SandboxVehicle.VehicleKind.Helicopter || Player.CurrentVehicle.Kind == SandboxVehicle.VehicleKind.Plane ? 5 : 4;
            Skills[index] = Mathf.Min(100f, Skills[index] + dt * .018f);
        }
        else if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            Skills[0] = Mathf.Min(100f, Skills[0] + dt * .025f);
        if (Player.IsStealth && !Player.IsInVehicle)
            Skills[3] = Mathf.Min(100f, Skills[3] + dt * .014f);
    }

    public void ShowToast(string message, float seconds = 3.5f) { Toast = message; ToastUntil = Time.time + seconds; }
    public void AwardCash(int amount) { if (Player != null) { Player.Cash += amount; ShowToast("+$" + amount); } }

    public void ReportGunshot(Vector3 position, float noiseRadius)
    {
        bool policeHeard = false, witness = false;
        foreach (var officer in police)
            if (officer != null && !officer.IsDead && Vector3.Distance(officer.transform.position, position) < noiseRadius)
                policeHeard = true;
        foreach (var civilian in civilians)
            if (civilian != null && !civilian.IsDead && Vector3.Distance(civilian.transform.position, position) < noiseRadius)
            { civilian.WitnessCrime(); witness = true; }
        if (policeHeard) ReportCrime(7f, position, true);
        else if (witness) { crimePendingAmount = Mathf.Max(crimePendingAmount, 7f); crimePendingUntil = Time.time + 3.8f; }
        else if (noiseRadius > 30f) { crimePendingAmount = Mathf.Max(crimePendingAmount, 4f); crimePendingUntil = Time.time + 8f; }
    }

    public void ReportCrime(float severity, Vector3 position, bool immediate = false)
    {
        if (!immediate)
        {
            bool witnessed = false;
            foreach (var actor in civilians)
                if (actor != null && !actor.IsDead && Vector3.Distance(actor.transform.position, position) < 55f)
                { actor.WitnessCrime(); witnessed = true; }
            if (witnessed) { crimePendingAmount = Mathf.Max(crimePendingAmount, severity); crimePendingUntil = Time.time + 3f; return; }
            crimePendingAmount = Mathf.Max(crimePendingAmount, severity * .65f);
            crimePendingUntil = Time.time + 6f;
            return;
        }
        heat = Mathf.Clamp(heat + severity, 0f, 150f);
        WantedLevel = Mathf.Clamp(Mathf.CeilToInt(heat / 30f), 0, 5);
        LastKnownPosition = position;
        InPursuit = true;
        lastSeen = Time.time;
        ShowToast("POLICE ALERT  •  WANTED " + WantedLevel, 2.8f);
        nextPoliceSpawn = Mathf.Min(nextPoliceSpawn, Time.time + 1f);
    }

    public void SetWantedLevel(int stars)
    {
        WantedLevel = Mathf.Clamp(stars, 0, 5);
        heat = WantedLevel * 30f;
        InPursuit = WantedLevel > 0;
        lastSeen = Time.time;
        LastKnownPosition = Player != null ? Player.transform.position : Vector3.zero;
        if (WantedLevel == 0) { crimePendingUntil = 0f; crimePendingAmount = 0f; }
    }

    public void PoliceSighted(Vector3 position)
    {
        if (WantedLevel <= 0) return;
        LastKnownPosition = position;
        lastSeen = Time.time;
        InPursuit = true;
    }

    void UpdateWanted()
    {
        if (WantedLevel <= 0) return;
        bool seen = false;
        Vector3 target = Player.IsInVehicle ? Player.CurrentVehicle.transform.position + Vector3.up * 1.3f : Player.transform.position + Vector3.up;
        foreach (var officer in police)
        {
            if (officer == null || officer.IsDead) continue;
            Vector3 eye = officer.transform.position + Vector3.up * 1.5f;
            Vector3 ray = target - eye;
            if (ray.magnitude > (Weather == WeatherState.Fog ? 36f : 75f)) continue;
            if (Physics.Raycast(eye, ray.normalized, out var hit, ray.magnitude + .1f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<SandboxPlayer>() == Player || Player.IsInVehicle && hit.collider.GetComponentInParent<SandboxVehicle>() == Player.CurrentVehicle)
                { seen = true; break; }
            }
        }
        if (seen) PoliceSighted(Player.transform.position);
        else if (Time.time - lastSeen > 2f) InPursuit = false;
        if (!InPursuit && Time.time - lastSeen > 12f + WantedLevel * 9f)
        {
            heat = Mathf.Max(0f, heat - 11f * .55f);
            int previous = WantedLevel;
            WantedLevel = Mathf.Clamp(Mathf.CeilToInt(heat / 30f), 0, 5);
            if (WantedLevel < previous) ShowToast(WantedLevel == 0 ? "SEARCH ENDED" : "WANTED REDUCED", 2f);
        }
        if (WantedLevel > 0 && Time.time > nextPoliceSpawn)
        {
            nextPoliceSpawn = Time.time + Mathf.Lerp(12f, 5f, WantedLevel / 5f);
            int active = 0;
            foreach (var officer in police) if (officer != null && !officer.IsDead && Vector3.Distance(officer.transform.position, Player.transform.position) < 125f) active++;
            if (active < 1 + WantedLevel * 2) SpawnPolice();
        }
    }

    public void SpawnPolice()
    {
        if (policeTemplate == null || Player == null) return;
        if (!TryHiddenSpawn(47f, 78f, out Vector3 location)) return;
        var spawned = Instantiate(policeTemplate.gameObject, location, Quaternion.identity).GetComponent<SandboxPedestrian>();
        spawned.name = WantedLevel >= 4 ? "Tactical Response" : "Police Response";
        spawned.Role = WantedLevel >= 4 ? SandboxPedestrian.ActorRole.Tactical : SandboxPedestrian.ActorRole.Police;
        police.Add(spawned);
        if (WantedLevel >= 3 && vehicleCatalog.Count > 0)
        {
            var cruiser = FindPoliceVehicle();
            if (cruiser != null)
            {
                var car = SpawnVehicle(cruiser, location + Vector3.right * 4f);
                var ai = car.GetComponent<SandboxTrafficAgent>() ?? car.gameObject.AddComponent<SandboxTrafficAgent>();
                ai.Waypoints = route;
                ai.Emergency = true;
                traffic.Add(ai);
            }
        }
    }

    SandboxVehicle FindPoliceVehicle()
    {
        foreach (var vehicle in vehicleCatalog) if (vehicle != null && vehicle.IsPolice) return vehicle;
        return null;
    }

    bool TryHiddenSpawn(float min, float max, out Vector3 spawn)
    {
        spawn = default;
        if (roadsRoot == null) return false;
        var camera = Camera.main;
        for (int i = 0; i < 64; i++)
        {
            Vector2 ring = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(min, max);
            Vector3 point = Player.transform.position + new Vector3(ring.x, 0f, ring.y);
            point.x = Mathf.Clamp(point.x, -275f, 275f); point.z = Mathf.Clamp(point.z, -275f, 275f);
            // Street geometry is near sea level; a low ray cannot select a building roof.
            if (!Physics.Raycast(new Vector3(point.x, 2.6f, point.z), Vector3.down, out var ground, 4f, ~0, QueryTriggerInteraction.Ignore))
                continue;
            if (!ground.collider.transform.IsChildOf(roadsRoot) || ground.normal.y < .8f) continue;
            Vector3 feet = ground.point + Vector3.up * .12f;
            if (Physics.CheckCapsule(feet + Vector3.up * .31f, feet + Vector3.up * 1.49f,
                .31f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (camera != null && Vector3.Dot(camera.transform.forward, (feet - camera.transform.position).normalized) >= .2f)
                continue;
            spawn = feet;
            return true;
        }
        return false;
    }

    void UpdatePopulation()
    {
        if (Player == null) return;
        civilians.RemoveAll(x => x == null);
        police.RemoveAll(x => x == null);
        traffic.RemoveAll(x => x == null);
        foreach (var actor in police)
            if (actor != null && actor.name.Contains("Response") && Vector3.Distance(actor.transform.position, Player.transform.position) > 210f)
                Destroy(actor.gameObject);
        foreach (var agent in traffic)
            if (agent != null && agent.name.Contains("(Spawned)") && Vector3.Distance(agent.transform.position, Player.transform.position) > 220f)
                Destroy(agent.gameObject);
        if (PedestriansEnabled && civilianTemplate != null && civilians.Count < 28)
        {
            if (TryHiddenSpawn(35f, 85f, out Vector3 civilianSpawn))
            {
                var actor = Instantiate(civilianTemplate.gameObject, civilianSpawn, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)).GetComponent<SandboxPedestrian>();
                actor.name = "Citizen " + civilians.Count.ToString("00");
                civilians.Add(actor);
            }
        }
        foreach (var actor in civilians) if (actor != null) actor.gameObject.SetActive(PedestriansEnabled || actor == civilianTemplate);
        if (TrafficEnabled && Time.time > nextTrafficSpawn && traffic.Count < 14 && route != null && route.Length > 2)
        {
            nextTrafficSpawn = Time.time + 5f;
            SandboxVehicle model = null;
            foreach (var vehicle in vehicleCatalog) if (vehicle != null && !vehicle.IsPolice && vehicle.Kind == SandboxVehicle.VehicleKind.Car) { model = vehicle; break; }
            if (model != null)
            {
                int segment = UnityEngine.Random.Range(0, route.Length);
                int following = (segment + 1) % route.Length;
                Vector3 position = Vector3.Lerp(route[segment].position, route[following].position, UnityEngine.Random.Range(.18f, .72f));
                position.y += .55f;
                if (Vector3.Distance(position, Player.transform.position) < 48f) return;
                var created = SpawnVehicle(model, position);
                created.transform.rotation = Quaternion.LookRotation((route[following].position - position).normalized);
                var ai = created.GetComponent<SandboxTrafficAgent>() ?? created.gameObject.AddComponent<SandboxTrafficAgent>();
                ai.Waypoints = route;
                ai.NextWaypoint = following;
                ai.enabled = true;
                ai.CruiseSpeedKph = UnityEngine.Random.Range(29f, 44f);
                traffic.Add(ai);
            }
        }
    }

    public SandboxVehicle SpawnVehicle(SandboxVehicle template, Vector3 position)
    {
        if (template == null) return null;
        var vehicle = Instantiate(template.gameObject, position, template.transform.rotation).GetComponent<SandboxVehicle>();
        vehicle.name = template.name + " (Spawned)";
        if (vehicle.Body != null) { vehicle.Body.linearVelocity = Vector3.zero; vehicle.Body.angularVelocity = Vector3.zero; }
        WireVehicle(vehicle);
        var ai = vehicle.GetComponent<SandboxTrafficAgent>();
        if (ai != null) ai.enabled = false;
        return vehicle;
    }

    void WireVehicle(SandboxVehicle vehicle)
    {
        vehicle.Stolen += stolen => ReportCrime(stolen.IsPolice ? 42f : 17f, stolen.transform.position, stolen.IsPolice);
        vehicle.Destroyed += wreck =>
        {
            SandboxFX.Explosion(wreck.transform.position + Vector3.up, 5f, 42f);
            ShowToast("VEHICLE DESTROYED", 2f);
        };
    }

    public SandboxVehicle SpawnVehicle(int index)
    {
        if (vehicleCatalog.Count == 0 || Player == null) return null;
        index = Mathf.Clamp(index, 0, vehicleCatalog.Count - 1);
        Vector3 position = Player.transform.position + Player.transform.forward * 9f + Vector3.up * .5f;
        var spawned = SpawnVehicle(vehicleCatalog[index], position);
        ShowToast("SPAWNED: " + vehicleCatalog[index].name, 2f);
        return spawned;
    }

    public void TeleportTo(string label, Vector3 position)
    {
        if (Player == null) return;
        if (Player.IsInVehicle) Player.ExitVehicle();
        Player.Teleport(position + Vector3.up * 1.5f);
        ShowToast(label.ToUpperInvariant(), 3f);
    }

    void TryInteract()
    {
        SandboxLocation nearest = null;
        float best = 100f;
        foreach (var location in FindObjectsByType<SandboxLocation>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(location.transform.position, Player.transform.position);
            if (d < location.InteractionRadius && d < best) { best = d; nearest = location; }
        }
        if (nearest == null) return;
        if (Player.IsInVehicle)
        {
            SandboxVehicle vehicle = Player.CurrentVehicle;
            bool service = nearest.Kind == SandboxLocation.LocationKind.Garage || nearest.Kind == SandboxLocation.LocationKind.GasStation;
            bool landVehicle = vehicle.Kind == SandboxVehicle.VehicleKind.Car || vehicle.Kind == SandboxVehicle.VehicleKind.Motorcycle;
            if (!service || !landVehicle || vehicle.SpeedKph > 8f) return;
        }
        ActiveLocation = nearest;
        if (nearest.Kind == SandboxLocation.LocationKind.Safehouse) { SaveGame(); ShowToast("SAFEHOUSE SAVED"); }
        else if (nearest.Kind == SandboxLocation.LocationKind.Hospital) { Player.Health = 100f; ShowToast("HEALTH RESTORED"); }
        else if (nearest.Kind == SandboxLocation.LocationKind.GasStation && Player.CurrentVehicle != null) { Player.CurrentVehicle.Repair(); ShowToast("VEHICLE SERVICED"); }
        else ShowToast(nearest.DisplayName.ToUpperInvariant());
    }

    public void CloseLocation() { ActiveLocation = null; }

    public bool Purchase(int cost)
    {
        if (Player == null || Player.Cash < cost) { ShowToast("INSUFFICIENT CASH"); return false; }
        Player.Cash -= cost;
        return true;
    }

    public void SetWeather(WeatherState state)
    {
        Weather = state;
        RenderSettings.fog = state == WeatherState.Fog || state == WeatherState.Rain || state == WeatherState.Storm;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = state == WeatherState.Fog ? .013f : state == WeatherState.Storm ? .005f : .0028f;
        if (rain != null) rain.gameObject.SetActive(state == WeatherState.Rain || state == WeatherState.Storm);
        UpdateSky();
    }

    void UpdateSky()
    {
        float daylight = Mathf.Clamp01(Mathf.Sin((Hour - 6f) / 24f * Mathf.PI * 2f) * .85f + .28f);
        Color night = new Color(.035f, .055f, .13f);
        Color day = Weather == WeatherState.Clear ? new Color(.48f, .76f, .86f) : new Color(.38f, .48f, .55f);
        Color sky = Color.Lerp(night, day, daylight);
        RenderSettings.ambientLight = Color.Lerp(new Color(.13f, .16f, .24f), new Color(.65f, .72f, .77f), daylight);
        RenderSettings.fogColor = sky;
        if (Camera.main != null) Camera.main.backgroundColor = sky;
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler((Hour / 24f) * 360f - 90f, -35f, 0f);
            sun.intensity = (.15f + daylight * 1.25f) * (Weather == WeatherState.Storm ? .55f : 1f);
            sun.color = Color.Lerp(new Color(.46f, .57f, .82f), new Color(1f, .91f, .75f), daylight);
        }
        bool lightsOn = Hour < 6.4f || Hour > 18.4f || Weather == WeatherState.Storm;
        foreach (var light in nightLights) if (light != null && light.enabled != lightsOn) light.enabled = lightsOn;
    }

    void SetupRain()
    {
        var go = new GameObject("Local Rain");
        go.transform.SetParent(transform);
        rain = go.AddComponent<ParticleSystem>();
        var main = rain.main;
        main.loop = true;
        main.startLifetime = 1.15f;
        main.startSpeed = 21f;
        main.startSize = .035f;
        main.maxParticles = 1800;
        main.startColor = new Color(.69f, .85f, 1f, .42f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(34f, 1f, 34f);
        var emission = rain.emission;
        emission.rateOverTime = 520f;
        var velocity = rain.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = -23f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        if (shader != null) renderer.sharedMaterial = new Material(shader);
        go.SetActive(false);
    }

    void LateUpdate()
    {
        if (rain != null && Player != null) rain.transform.position = Player.transform.position + Vector3.up * 14f;
    }

    public void SaveGame()
    {
        if (smokeCaptureRun && !smokeSaveTest || Player == null) return;
        var arsenal = Player.GetComponent<SandboxArsenal>();
        Vector3 savePosition = Player.transform.position;
        if (Player.IsInVehicle)
        {
            SandboxVehicle vehicle = Player.CurrentVehicle;
            bool groundedLandVehicle = (vehicle.Kind == SandboxVehicle.VehicleKind.Car || vehicle.Kind == SandboxVehicle.VehicleKind.Motorcycle)
                && vehicle.IsGrounded;
            savePosition = groundedLandVehicle ? vehicle.GetSafeExitPosition() : lastGroundedPosition;
        }
        var data = new SaveData
        {
            position = savePosition,
            health = Player.Health, armor = Player.Armor, cash = Player.Cash, hour = Hour, heat = heat,
            weather = (int)Weather, selectedWeapon = arsenal != null ? arsenal.SelectedIndex : 0,
            owned = arsenal != null ? (bool[])arsenal.Owned.Clone() : null,
            loaded = arsenal != null ? (int[])arsenal.Loaded.Clone() : null,
            reserve = arsenal != null ? (int[])arsenal.Reserve.Clone() : null,
            skills = (float[])Skills.Clone()
        };
        try { Directory.CreateDirectory(Path.GetDirectoryName(SavePath)); File.WriteAllText(SavePath, JsonUtility.ToJson(data, true)); }
        catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
    }

    public void LoadGame(bool notify = true)
    {
        if (smokeCaptureRun && !smokeSaveTest || Player == null || !File.Exists(SavePath)) return;
        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null) return;
            Vector3 loadPosition = FindSafeLoadPosition(data.position);
            Player.Teleport(loadPosition);
            lastGroundedPosition = loadPosition;
            Player.Health = Mathf.Clamp(data.health, 1f, 100f);
            Player.Armor = Mathf.Clamp(data.armor, 0f, 100f);
            Player.Cash = Mathf.Max(0, data.cash);
            Hour = data.hour;
            SetWeather((WeatherState)Mathf.Clamp(data.weather, 0, 4));
            Skills = data.skills != null && data.skills.Length == 7 ? data.skills : new float[7];
            var arsenal = Player.GetComponent<SandboxArsenal>();
            if (arsenal != null && data.owned != null && data.owned.Length == arsenal.Owned.Length)
            {
                arsenal.Owned = data.owned;
                arsenal.Loaded = data.loaded;
                arsenal.Reserve = data.reserve;
                arsenal.Select(data.selectedWeapon);
            }
            // Loading always begins safely outside of a pursuit.
            SetWantedLevel(0);
            if (notify) ShowToast("GAME LOADED");
        }
        catch (Exception e) { Debug.LogWarning("Load failed: " + e.Message); }
    }

    Vector3 FindSafeLoadPosition(Vector3 savedPosition)
    {
        if (TryFindClearGround(savedPosition, out Vector3 position)) return position;
        if (TryFindClearGround(lastGroundedPosition, out position)) return position;
        if (TryFindClearGround(respawnPoint, out position)) return position;
        return Player.IsInVehicle ? Player.CurrentVehicle.GetSafeExitPosition() : Player.transform.position;
    }

    bool TryFindClearGround(Vector3 origin, out Vector3 position)
    {
        position = default;
        float radius = playerController != null ? playerController.radius : .37f;
        float height = playerController != null ? playerController.height : 2.1f;
        for (int ring = 0; ring <= 4; ring++)
        {
            int samples = ring == 0 ? 1 : 12;
            for (int i = 0; i < samples; i++)
            {
                float angle = i * Mathf.PI * 2f / samples;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (ring * 3f);
                Vector3 sample = origin + offset;
                RaycastHit[] hits = Physics.RaycastAll(sample + Vector3.up * 2.6f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit hit in hits)
                {
                    if (hit.normal.y < .7f || hit.collider.GetComponentInParent<SandboxVehicle>() != null ||
                        hit.collider.GetComponentInParent<SandboxPedestrian>() != null ||
                        hit.collider.GetComponentInParent<SandboxPlayer>() != null) continue;
                    Vector3 feet = hit.point + Vector3.up * .12f;
                    Collider[] overlaps = Physics.OverlapCapsule(feet + Vector3.up * radius,
                        feet + Vector3.up * (height - radius), radius, ~0, QueryTriggerInteraction.Ignore);
                    bool clear = true;
                    foreach (Collider overlap in overlaps)
                        if (!overlap.transform.IsChildOf(Player.transform)) { clear = false; break; }
                    if (!clear) continue;
                    position = feet;
                    return true;
                }
            }
        }
        return false;
    }

    public void PlayerRespawned()
    {
        SetWantedLevel(0);
        if (Player != null) { Player.Teleport(respawnPoint); Player.Health = 100f; Player.Armor = 0f; }
        ShowToast("RESPAWNED AT HARBORLINE GENERAL", 4f);
    }
}
