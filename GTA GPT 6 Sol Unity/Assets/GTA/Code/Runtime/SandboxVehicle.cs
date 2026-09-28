using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shared arcade physics and entry point for land, water and air vehicles.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class SandboxVehicle : MonoBehaviour
{
    public enum VehicleKind { Car, Motorcycle, Boat, Helicopter, Plane }

    [Header("Identity")]
    [SerializeField] private VehicleKind kind = VehicleKind.Car;
    [SerializeField] private bool isPolice;
    [SerializeField] private float maxHealth = 180f;
    [SerializeField] private float health = 180f;

    [Header("Mounts and visuals")]
    [SerializeField] private Transform driverSeat;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform rotor;
    [SerializeField] private Transform tailRotor;
    [SerializeField] private Transform propeller;
    [SerializeField] private Transform[] wheelVisuals;
    [SerializeField] private Light[] headlights;
    [SerializeField] private Light[] brakeLights;
    [SerializeField] private Light[] policeLights;
    [SerializeField] private Vector3 chassisSize = new Vector3(2f, 1.2f, 4.2f);

    [Header("World")]
    [SerializeField] private float waterLevel;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float suspensionLength = 0.45f;
    [SerializeField] private float wheelRadius = 0.36f;
    [SerializeField] private float grip = 1f;
    [SerializeField, Range(0, 3)] private int engineUpgrade;
    [SerializeField, Range(0, 3)] private int brakeUpgrade;
    [SerializeField, Range(0, 3)] private int armorUpgrade;

    private static readonly HashSet<SandboxVehicle> Registry = new HashSet<SandboxVehicle>();
    private static readonly bool SmokePhysicsLog = Array.Exists(Environment.GetCommandLineArgs(),
        argument => argument == "-smokecapture");
    private readonly RaycastHit[] wheelHits = new RaycastHit[16];
    private Rigidbody body;
    private Collider bodyCollider;
    private SandboxPlayer driver;
    private bool aiOccupied;
    private bool destroyed;
    private bool playerInputEnabled = true;
    private bool headlightsOn;
    private bool manualLights;
    private bool sirenOn;
    private float throttle;
    private float steer;
    private float brake;
    private float verticalControl;
    private float pitchControl;
    private float yawControl;
    private float planeThrottle;
    private float lastAIInputTime;
    private int groundedWheels;
    private float rotorAngle;
    private Quaternion rotorRest = Quaternion.identity;

    public static IReadOnlyCollection<SandboxVehicle> All => Registry;
    public VehicleKind Kind { get => kind; set { kind = value; if (body != null) ConfigurePhysics(); } }
    public bool IsPolice { get => isPolice; set => isPolice = value; }
    public int EngineUpgrade { get => engineUpgrade; set => engineUpgrade = Mathf.Clamp(value, 0, 3); }
    public int BrakeUpgrade { get => brakeUpgrade; set => brakeUpgrade = Mathf.Clamp(value, 0, 3); }
    public int ArmorUpgrade { get => armorUpgrade; set => armorUpgrade = Mathf.Clamp(value, 0, 3); }
    public Vector3 ChassisSize { get => chassisSize; set => chassisSize = value; }
    public float Health { get => health; set { health = Mathf.Clamp(value, 0f, maxHealth); destroyed = health <= 0f; } }
    public float MaxHealth => maxHealth;
    public bool IsDestroyed => destroyed;
    public bool IsOccupied => driver != null || aiOccupied;
    public SandboxPlayer Driver => driver;
    public Rigidbody Body => body;
    public Transform DriverSeat { get => driverSeat; set => driverSeat = value; }
    public Transform ExitPoint { get => exitPoint; set => exitPoint = value; }
    public float WaterLevel { get => waterLevel; set => waterLevel = value; }
    public bool HeadlightsOn { get => headlightsOn || !manualLights && IsOccupied && RenderSettings.ambientLight.grayscale < 0.27f; set { headlightsOn = value; manualLights = true; UpdateLights(); } }
    public bool SirenOn { get => sirenOn; set { sirenOn = value && isPolice; UpdateLights(); } }
    public float SpeedKph => body != null ? body.linearVelocity.magnitude * 3.6f : 0f;
    public bool IsGrounded => groundedWheels > 0;

    public event Action<SandboxVehicle> Destroyed;
    public event Action<SandboxVehicle, float> Damaged;
    public event Action<SandboxVehicle> Stolen;

    private void Awake()
    {
        // Imported scene builders may only name the root and leave the optional enum at its default.
        string identity = name.ToLowerInvariant();
        if (transform.childCount > 0) identity += " " + transform.GetChild(0).name.ToLowerInvariant();
        if (kind == VehicleKind.Car)
        {
            if (identity.Contains("motorcycle") || identity.Contains("bike")) kind = VehicleKind.Motorcycle;
            else if (identity.Contains("boat")) kind = VehicleKind.Boat;
            else if (identity.Contains("helicopter") || identity.Contains("heli")) kind = VehicleKind.Helicopter;
            else if (identity.Contains("airplane") || identity.Contains("plane")) kind = VehicleKind.Plane;
        }
        if (!isPolice && identity.Contains("police")) isPolice = true;
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        if (bodyCollider == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = GetChassisSize();
            box.center = Vector3.up * (box.size.y * 0.68f);
            bodyCollider = box;
        }
        maxHealth = Mathf.Max(1f, maxHealth);
        health = Mathf.Clamp(health, 1f, maxHealth);
        DiscoverAnimatedParts();
        ConfigurePhysics();
        EnsureLights();
        UpdateLights();
    }

    private void OnEnable() => Registry.Add(this);
    private void OnDisable() => Registry.Remove(this);

    private void ConfigurePhysics()
    {
        if (body == null) return;
        body.mass = kind switch
        {
            VehicleKind.Motorcycle => 330f,
            VehicleKind.Boat => 800f,
            VehicleKind.Helicopter => 1400f,
            VehicleKind.Plane => 1100f,
            _ => 1400f
        };
        body.interpolation = RigidbodyInterpolation.Interpolate;
        // Sweep CCD avoids speculative contacts with a road slab below the box
        // while retaining fast-car collision checks at street speed.
        body.collisionDetectionMode = kind == VehicleKind.Car
            ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.ContinuousSpeculative;
        body.linearDamping = kind == VehicleKind.Boat ? 0.55f : kind == VehicleKind.Helicopter ? 0.3f : 0.08f;
        body.angularDamping = kind == VehicleKind.Boat ? 1.8f : kind == VehicleKind.Helicopter ? 1.7f : 1.2f;
        body.centerOfMass = kind == VehicleKind.Motorcycle ? Vector3.up * 0.3f : kind == VehicleKind.Helicopter ? Vector3.up * 0.15f : Vector3.up * 0.25f;
        // Cars use arcade suspension on a flat city grid. Prevent small road seams from
        // rolling the box chassis into the pavement and redirecting the vehicle.
        body.constraints = kind == VehicleKind.Car
            ? RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ
            : RigidbodyConstraints.None;
        body.maxAngularVelocity = 5f;
        body.useGravity = true;
    }

    private void DiscoverAnimatedParts()
    {
        List<Transform> wheels = wheelVisuals == null || wheelVisuals.Length == 0 ? new List<Transform>(4) : null;
        foreach (Transform part in GetComponentsInChildren<Transform>(true))
        {
            if (part == transform) continue;
            if (wheels != null && part.name.StartsWith("Wheel_", StringComparison.OrdinalIgnoreCase)) wheels.Add(part);
            if (rotor == null && part.name.StartsWith("Rotor_Main", StringComparison.OrdinalIgnoreCase)) rotor = part;
            if (tailRotor == null && part.name.StartsWith("Rotor_Tail", StringComparison.OrdinalIgnoreCase)) tailRotor = part;
            if (propeller == null && part.name.StartsWith("Propeller", StringComparison.OrdinalIgnoreCase)) propeller = part;
        }
        if (wheels != null && wheels.Count > 0) wheelVisuals = wheels.ToArray();
        if (rotor != null) rotorRest = rotor.localRotation;
    }

    private void Update()
    {
        if (driver != null) ReadDriverInput();
        else if (aiOccupied && Time.time - lastAIInputTime > 0.8f)
        {
            throttle = 0f;
            brake = 0.4f;
            steer = 0f;
        }
        if (rotor != null && kind == VehicleKind.Helicopter)
        {
            rotorAngle += (driver != null || aiOccupied ? 1250f : 180f) * Time.deltaTime;
            rotor.localRotation = rotorRest * Quaternion.Euler(0f, rotorAngle, 0f);
            if (tailRotor != null) tailRotor.Rotate(Vector3.right, 1500f * Time.deltaTime, Space.Self);
        }
        if (propeller != null && kind == VehicleKind.Plane)
            propeller.Rotate(Vector3.forward, (280f + planeThrottle * 1600f) * Time.deltaTime, Space.Self);
        UpdateLights();
    }

    private void ReadDriverInput()
    {
        Keyboard key = Keyboard.current;
        if (key == null || !playerInputEnabled || destroyed)
        {
            throttle = steer = brake = verticalControl = pitchControl = yawControl = 0f;
            return;
        }

        float forward = (key.wKey.isPressed ? 1f : 0f) - (key.sKey.isPressed ? 1f : 0f);
        float turn = (key.dKey.isPressed ? 1f : 0f) - (key.aKey.isPressed ? 1f : 0f);
        throttle = forward;
        steer = turn;
        brake = key.spaceKey.isPressed ? 1f : 0f;
        verticalControl = 0f;
        pitchControl = 0f;
        yawControl = 0f;

        if (kind == VehicleKind.Helicopter)
        {
            verticalControl = (key.spaceKey.isPressed ? 1f : 0f) - (key.leftCtrlKey.isPressed ? 1f : 0f);
            brake = 0f;
        }
        else if (kind == VehicleKind.Plane)
        {
            planeThrottle = Mathf.Clamp01(planeThrottle + forward * Time.deltaTime * 0.65f);
            pitchControl = (key.upArrowKey.isPressed || key.iKey.isPressed ? 1f : 0f) - (key.downArrowKey.isPressed || key.kKey.isPressed ? 1f : 0f);
            yawControl = (key.eKey.isPressed ? 1f : 0f) - (key.qKey.isPressed ? 1f : 0f);
        }

        if (key.lKey.wasPressedThisFrame) HeadlightsOn = !HeadlightsOn;
        if (isPolice && key.jKey.wasPressedThisFrame) SirenOn = !SirenOn;
    }

    private void FixedUpdate()
    {
        if (body == null || body.isKinematic || destroyed) return;
        groundedWheels = 0;
        switch (kind)
        {
            case VehicleKind.Car:
            case VehicleKind.Motorcycle: SimulateLandVehicle(); break;
            case VehicleKind.Boat: SimulateBoat(); break;
            case VehicleKind.Helicopter: SimulateHelicopter(); break;
            case VehicleKind.Plane: SimulatePlane(); break;
        }
    }

    private void SimulateLandVehicle()
    {
        Vector3 size = GetChassisSize();
        float lateral = size.x * (kind == VehicleKind.Motorcycle ? 0.35f : 0.39f);
        float longitudinal = size.z * 0.34f;
        // Keep each wheel ray within reach while the chassis rests on the road.
        // A fixed fraction of body height leaves vans and pickups with no wheel contact.
        float colliderBottom = bodyCollider is BoxCollider chassisBox
            ? chassisBox.center.y - chassisBox.size.y * .5f : size.y * .06f;
        float mountY = Mathf.Min(size.y * .54f,
            colliderBottom + suspensionLength + wheelRadius - .22f);
        float spring = body.mass * 9.81f / (kind == VehicleKind.Motorcycle ? 0.44f : 0.64f);
        float damper = body.mass * 2.1f;
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * lateral;
            float z = (i < 2 ? 1f : -1f) * longitudinal;
            Vector3 origin = transform.TransformPoint(x, mountY, z);
            float maxDistance = suspensionLength + wheelRadius;
            if (!CastGround(origin, -transform.up, maxDistance, out RaycastHit hit)) continue;
            groundedWheels++;
            float compression = maxDistance - hit.distance;
            float wheelVelocity = Vector3.Dot(body.GetPointVelocity(origin), transform.up);
            float force = Mathf.Clamp(compression * spring - wheelVelocity * damper, 0f, body.mass * 20f);
            body.AddForceAtPosition(transform.up * force, origin, ForceMode.Force);
        }

        Vector3 velocity = body.linearVelocity;
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        float sideSpeed = Vector3.Dot(velocity, transform.right);
        float topSpeed = (kind == VehicleKind.Motorcycle ? 39f : 34f) * (1f + engineUpgrade * 0.07f);
        float reverseSpeed = kind == VehicleKind.Motorcycle ? 10f : 13f;
        if (groundedWheels > 0)
        {
            float lateralGrip = (brake > 0.5f ? 2.4f : 7f) * grip;
            body.AddForce(-transform.right * sideSpeed * body.mass * lateralGrip, ForceMode.Force);
            if (throttle != 0f && (throttle > 0f ? forwardSpeed < topSpeed : forwardSpeed > -reverseSpeed))
                body.AddForce(transform.forward * throttle * body.mass * (kind == VehicleKind.Motorcycle ? 9f : 8f) * (1f + engineUpgrade * 0.14f), ForceMode.Force);
            if (brake > 0f)
                body.AddForce(-transform.forward * forwardSpeed * body.mass * brake * (8f + brakeUpgrade * 1.5f), ForceMode.Force);
            float speedFactor = Mathf.Clamp(forwardSpeed / 8f, -1f, 1f);
            body.AddTorque(Vector3.up * steer * speedFactor * (kind == VehicleKind.Motorcycle ? 1.7f : 1.45f), ForceMode.Acceleration);
            float yawDamping = kind == VehicleKind.Car && Mathf.Abs(steer) < .05f ? 5.5f : 1.7f;
            body.AddTorque(-Vector3.up * body.angularVelocity.y * yawDamping, ForceMode.Acceleration);
        }

        if (kind == VehicleKind.Motorcycle)
        {
            Vector3 correction = Vector3.Cross(transform.up, Vector3.up) * 7f;
            body.AddTorque(correction - body.angularVelocity * 1.1f, ForceMode.Acceleration);
        }
        AnimateWheels(forwardSpeed);
    }

    private bool CastGround(Vector3 origin, Vector3 direction, float distance, out RaycastHit closest)
    {
        closest = default;
        int count = Physics.RaycastNonAlloc(origin, direction, wheelHits, distance, groundMask, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = wheelHits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.normal.y < 0.24f || hit.distance >= best) continue;
            closest = hit;
            best = hit.distance;
        }
        return best < float.MaxValue;
    }

    private void AnimateWheels(float forwardSpeed)
    {
        if (wheelVisuals == null) return;
        for (int i = 0; i < wheelVisuals.Length; i++)
        {
            Transform wheel = wheelVisuals[i];
            if (wheel == null) continue;
            wheel.Rotate(Vector3.right, forwardSpeed / Mathf.Max(0.1f, wheelRadius) * Mathf.Rad2Deg * Time.fixedDeltaTime, Space.Self);
        }
    }

    private void SimulateBoat()
    {
        float immersion = waterLevel + 0.2f - transform.position.y;
        if (immersion > 0f)
        {
            float verticalSpeed = Vector3.Dot(body.linearVelocity, Vector3.up);
            body.AddForce(Vector3.up * body.mass * (9.81f + immersion * 8f - verticalSpeed * 3f), ForceMode.Force);
            body.AddForce(-body.linearVelocity * body.mass * 0.28f, ForceMode.Force);
            body.AddTorque(Vector3.Cross(transform.up, Vector3.up) * 2.5f, ForceMode.Acceleration);
            if (Vector3.Dot(body.linearVelocity, transform.forward) < 23f * (1f + engineUpgrade * 0.07f) || throttle < 0f)
                body.AddForce(transform.forward * throttle * body.mass * 5f * (1f + engineUpgrade * 0.14f), ForceMode.Force);
            body.AddTorque(Vector3.up * steer * (0.45f + SpeedKph * 0.015f), ForceMode.Acceleration);
        }
    }

    private void SimulateHelicopter()
    {
        // A slight negative idle lift holds a landed helicopter down; Space adds collective.
        float lift = 9.81f * 0.82f + verticalControl * 15f;
        body.AddForce(transform.up * body.mass * lift, ForceMode.Force);
        body.AddForce(transform.forward * throttle * body.mass * 5.5f * (1f + engineUpgrade * 0.14f), ForceMode.Force);
        body.AddTorque(Vector3.up * steer * 1.8f, ForceMode.Acceleration);
        Vector3 desiredUp = (Vector3.up + transform.forward * throttle * 0.13f + transform.right * steer * 0.06f).normalized;
        body.AddTorque(Vector3.Cross(transform.up, desiredUp) * 3.4f, ForceMode.Acceleration);
        if (body.linearVelocity.magnitude > 43f) body.linearVelocity = body.linearVelocity.normalized * 43f;
    }

    private void SimulatePlane()
    {
        Vector3 velocity = body.linearVelocity;
        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(velocity, transform.forward));
        body.AddForce(transform.forward * body.mass * planeThrottle * 12f * (1f + engineUpgrade * 0.14f), ForceMode.Force);
        float liftFactor = Mathf.Min(2.1f, forwardSpeed * forwardSpeed / (24f * 24f));
        body.AddForce(transform.up * body.mass * 9.81f * liftFactor, ForceMode.Force);
        body.AddForce(-velocity * body.mass * (0.015f + forwardSpeed * 0.0012f), ForceMode.Force);
        float authority = Mathf.Clamp01(forwardSpeed / 12f);
        body.AddRelativeTorque(new Vector3(-pitchControl * 1.25f, yawControl * 0.6f, -steer * 1.8f) * authority, ForceMode.Acceleration);
        if (brake > 0f && Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 2.2f, groundMask, QueryTriggerInteraction.Ignore))
            body.AddForce(-velocity * body.mass * brake * (2.8f + brakeUpgrade * 0.6f), ForceMode.Force);
        if (body.linearVelocity.magnitude > 75f) body.linearVelocity = body.linearVelocity.normalized * 75f;
    }

    private Vector3 GetChassisSize()
    {
        return kind switch
        {
            VehicleKind.Motorcycle => new Vector3(0.85f, 1.25f, 2.35f),
            VehicleKind.Boat => new Vector3(2.2f, 1.1f, 5.4f),
            VehicleKind.Helicopter => new Vector3(2.5f, 1.5f, 6f),
            VehicleKind.Plane => new Vector3(7f, 1.6f, 7f),
            _ => chassisSize
        };
    }

    private void UpdateLights()
    {
        SetLights(headlights, HeadlightsOn && !destroyed);
        SetLights(brakeLights, brake > 0.2f && !destroyed);
        if (policeLights != null && policeLights.Length >= 2)
        {
            bool first = Mathf.Repeat(Time.time * 7f, 1f) < 0.5f;
            if (policeLights[0] != null) policeLights[0].enabled = sirenOn && !destroyed && first;
            if (policeLights[1] != null) policeLights[1].enabled = sirenOn && !destroyed && !first;
        }
    }

    private static void SetLights(Light[] lights, bool state)
    {
        if (lights == null) return;
        foreach (Light light in lights) if (light != null && light.enabled != state) light.enabled = state;
    }

    private void EnsureLights()
    {
        Vector3 size = GetChassisSize();
        if (headlights == null || headlights.Length == 0)
        {
            int count = kind == VehicleKind.Motorcycle ? 1 : 2;
            headlights = new Light[count];
            for (int i = 0; i < count; i++)
            {
                float x = count == 1 ? 0f : (i == 0 ? -1f : 1f) * size.x * 0.34f;
                headlights[i] = CreateLight("Headlamp " + i, new Vector3(x, size.y * 0.74f, size.z * 0.48f),
                    LightType.Spot, new Color(1f, 0.94f, 0.77f), 3.2f, 26f);
                headlights[i].spotAngle = 49f;
            }
        }
        if ((kind == VehicleKind.Car || kind == VehicleKind.Motorcycle) && (brakeLights == null || brakeLights.Length == 0))
        {
            int count = kind == VehicleKind.Motorcycle ? 1 : 2;
            brakeLights = new Light[count];
            for (int i = 0; i < count; i++)
            {
                float x = count == 1 ? 0f : (i == 0 ? -1f : 1f) * size.x * 0.36f;
                brakeLights[i] = CreateLight("Brake lamp " + i, new Vector3(x, size.y * 0.76f, -size.z * 0.48f),
                    LightType.Point, new Color(1f, 0.12f, 0.06f), 2.4f, 4.5f);
            }
        }
        if (isPolice && (policeLights == null || policeLights.Length == 0))
        {
            policeLights = new Light[2];
            policeLights[0] = CreateLight("Police beacon red", new Vector3(-0.43f, size.y * 1.35f, 0f),
                LightType.Point, new Color(1f, 0.05f, 0.05f), 5f, 12f);
            policeLights[1] = CreateLight("Police beacon blue", new Vector3(0.43f, size.y * 1.35f, 0f),
                LightType.Point, new Color(0.08f, 0.26f, 1f), 5f, 12f);
        }
    }

    private Light CreateLight(string label, Vector3 localPosition, LightType type, Color color, float intensity, float range)
    {
        GameObject child = new GameObject(label);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = localPosition;
        Light light = child.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.enabled = false;
        return light;
    }

    public void SetPlayerInputEnabled(bool enabled) => playerInputEnabled = enabled;

    public void SetAIInput(float inputThrottle, float inputSteer, float inputBrake)
    {
        if (driver != null) return;
        if (body != null && body.isKinematic) body.isKinematic = false;
        aiOccupied = true;
        throttle = Mathf.Clamp(inputThrottle, -1f, 1f);
        steer = Mathf.Clamp(inputSteer, -1f, 1f);
        brake = Mathf.Clamp01(inputBrake);
        lastAIInputTime = Time.time;
        if (kind == VehicleKind.Plane) planeThrottle = Mathf.Clamp01(inputThrottle);
    }

    public void SetAIOccupied(bool occupied)
    {
        if (driver != null) return;
        aiOccupied = occupied;
        if (!occupied) throttle = steer = brake = 0f;
    }

    public bool TryEnter(SandboxPlayer player)
    {
        if (player == null || driver != null || destroyed) return false;
        bool theft = aiOccupied;
        aiOccupied = false;
        driver = player;
        if (body != null && body.isKinematic) body.isKinematic = false;
        throttle = steer = brake = 0f;
        if (theft) Stolen?.Invoke(this);
        return true;
    }

    public void ReleaseDriver(SandboxPlayer player)
    {
        if (driver != player) return;
        driver = null;
        throttle = steer = brake = verticalControl = pitchControl = yawControl = 0f;
        playerInputEnabled = true;
    }

    public Vector3 GetSafeExitPosition()
    {
        if (exitPoint != null) return exitPoint.position;
        Vector3 size = GetChassisSize();
        Vector3[] candidates =
        {
            transform.position - transform.right * (size.x * 0.5f + 1.2f),
            transform.position + transform.right * (size.x * 0.5f + 1.2f),
            transform.position - transform.forward * (size.z * 0.5f + 1.2f)
        };
        foreach (Vector3 candidate in candidates)
        {
            Vector3 position = candidate + Vector3.up * 0.3f;
            if (Physics.Raycast(candidate + Vector3.up * 2.5f, Vector3.down, out RaycastHit hit, 5f, groundMask, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform)) position = hit.point + Vector3.up * 0.05f;
            if (!Physics.CheckCapsule(position + Vector3.up * 0.38f, position + Vector3.up * 1.55f, 0.32f, groundMask, QueryTriggerInteraction.Ignore))
                return position;
        }
        return candidates[0] + Vector3.up * 0.5f;
    }

    public void Damage(float amount)
    {
        if (amount <= 0f || destroyed) return;
        amount /= 1f + armorUpgrade * 0.28f;
        health = Mathf.Max(0f, health - amount);
        Damaged?.Invoke(this, amount);
        if (health <= 0f)
        {
            destroyed = true;
            throttle = steer = brake = 0f;
            sirenOn = false;
            UpdateLights();
            Destroyed?.Invoke(this);
        }
    }

    public void Repair()
    {
        health = maxHealth;
        destroyed = false;
        body.linearVelocity *= 0.8f;
        body.angularVelocity *= 0.8f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (destroyed) return;
        float speed = collision.relativeVelocity.magnitude;
        if (SmokePhysicsLog && driver != null && kind == VehicleKind.Car && speed > 1f)
        {
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Debug.Log("SMOKE car-contact other=" + collision.collider.name + " speed=" + speed.ToString("F2")
                + " point=" + point.ToString("F2") + " car=" + transform.position.ToString("F2")
                + " yaw=" + transform.eulerAngles.y.ToString("F1"));
        }
        if (speed > (kind == VehicleKind.Plane || kind == VehicleKind.Helicopter ? 5f : 8f))
            Damage((speed - 5f) * (kind == VehicleKind.Plane || kind == VehicleKind.Helicopter ? 7f : 3.1f));
    }
}
