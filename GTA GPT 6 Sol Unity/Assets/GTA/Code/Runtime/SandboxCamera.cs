using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Orbit, shoulder aim and chase camera with obstacle avoidance.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class SandboxCamera : MonoBehaviour
{
    [SerializeField] private SandboxPlayer targetPlayer;
    [SerializeField] private float mouseSensitivity = 0.14f;
    [SerializeField] private float onFootDistance = 5.2f;
    [SerializeField] private float aimDistance = 2.8f;
    [SerializeField] private float characterPivotHeight = 1.55f;
    [SerializeField] private float collisionRadius = 0.28f;
    [SerializeField] private float followSharpness = 12f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private readonly RaycastHit[] collisionHits = new RaycastHit[24];
    private Camera viewCamera;
    private float yaw;
    private float pitch = 16f;
    private float lastMouseMovementTime;
    private float nextTargetSearchTime;
    private float shakeAmplitude;
    private float shakeEndTime;
    private bool firstPerson;
    private bool initialized;

    public SandboxPlayer TargetPlayer { get => targetPlayer; set { targetPlayer = value; initialized = false; } }
    public bool InputEnabled { get; set; } = true;
    public bool IsAiming { get; private set; }
    public bool FirstPerson => firstPerson;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        yaw = transform.eulerAngles.y;
        float initialPitch = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
        if (Mathf.Abs(initialPitch) > 0.1f) pitch = initialPitch;
        if (targetPlayer == null) targetPlayer = FindAnyObjectByType<SandboxPlayer>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (targetPlayer == null)
        {
            if (Time.unscaledTime >= nextTargetSearchTime)
            {
                targetPlayer = FindAnyObjectByType<SandboxPlayer>();
                nextTargetSearchTime = Time.unscaledTime + 1f;
            }
            if (targetPlayer == null) return;
        }

        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        Vector2 mouseDelta = InputEnabled && Cursor.lockState == CursorLockMode.Locked && mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
        if (mouseDelta.sqrMagnitude > 0.01f)
        {
            yaw += mouseDelta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, -35f, 74f);
            lastMouseMovementTime = Time.unscaledTime;
        }

        SandboxVehicle vehicle = targetPlayer.CurrentVehicle;
        if (vehicle != null && Time.unscaledTime - lastMouseMovementTime > 1.2f)
            yaw = Mathf.LerpAngle(yaw, vehicle.transform.eulerAngles.y, 1f - Mathf.Exp(-1.2f * dt));

        if (InputEnabled && keyboard != null && keyboard.vKey.wasPressedThisFrame) firstPerson = !firstPerson;
        IsAiming = InputEnabled && mouse != null && mouse.rightButton.isPressed;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = GetPivot(rotation);
        float distance = GetDistance(vehicle);
        Vector3 desired = firstPerson ? pivot : ResolveCollision(pivot, pivot - rotation * Vector3.forward * distance);

        if (!initialized)
        {
            transform.position = desired;
            initialized = true;
        }
        else transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * dt));

        Vector3 shake = Vector3.zero;
        if (Time.unscaledTime < shakeEndTime)
        {
            float remaining = Mathf.Clamp01((shakeEndTime - Time.unscaledTime) * 4f);
            shake = Random.insideUnitSphere * shakeAmplitude * remaining;
        }
        else shakeAmplitude = 0f;
        transform.position += shake;
        transform.rotation = rotation;
        float wantedFov = firstPerson ? 70f : IsAiming ? 50f : vehicle != null && vehicle.Kind == SandboxVehicle.VehicleKind.Plane ? 74f : 66f;
        viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, wantedFov, 1f - Mathf.Exp(-8f * dt));
    }

    private Vector3 GetPivot(Quaternion rotation)
    {
        SandboxVehicle vehicle = targetPlayer.CurrentVehicle;
        if (vehicle != null)
        {
            float height = vehicle.Kind == SandboxVehicle.VehicleKind.Helicopter ? 2.8f : vehicle.Kind == SandboxVehicle.VehicleKind.Plane ? 2.4f : 1.7f;
            if (firstPerson && vehicle.DriverSeat != null) return vehicle.DriverSeat.position + Vector3.up * 0.35f;
            return vehicle.transform.position + Vector3.up * height;
        }

        Vector3 pivot = targetPlayer.transform.position + Vector3.up * characterPivotHeight;
        if (IsAiming && !firstPerson) pivot += rotation * Vector3.right * 0.55f;
        return pivot;
    }

    private float GetDistance(SandboxVehicle vehicle)
    {
        if (firstPerson) return 0f;
        if (vehicle == null) return IsAiming ? aimDistance : onFootDistance;
        if (vehicle.Kind == SandboxVehicle.VehicleKind.Helicopter) return 13f;
        if (vehicle.Kind == SandboxVehicle.VehicleKind.Plane) return 17f;
        if (vehicle.Kind == SandboxVehicle.VehicleKind.Boat) return 9f;
        if (vehicle.Kind == SandboxVehicle.VehicleKind.Motorcycle) return 6.5f;
        return IsAiming ? 5.2f : 8f;
    }

    private Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
    {
        Vector3 vector = desired - pivot;
        float distance = vector.magnitude;
        if (distance < 0.01f) return desired;
        int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, vector / distance, collisionHits, distance, collisionMask, QueryTriggerInteraction.Ignore);
        float nearest = distance;
        Transform playerTransform = targetPlayer.transform;
        Transform vehicleTransform = targetPlayer.CurrentVehicle != null ? targetPlayer.CurrentVehicle.transform : null;
        for (int i = 0; i < count; i++)
        {
            Collider collider = collisionHits[i].collider;
            if (collider == null) continue;
            Transform hitTransform = collider.transform;
            if (hitTransform.IsChildOf(playerTransform) || vehicleTransform != null && hitTransform.IsChildOf(vehicleTransform)) continue;
            nearest = Mathf.Min(nearest, collisionHits[i].distance);
        }
        return pivot + vector.normalized * Mathf.Max(0.12f, nearest - 0.18f);
    }

    public void SnapToTarget()
    {
        if (targetPlayer == null) return;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = GetPivot(rotation);
        transform.position = firstPerson ? pivot : ResolveCollision(pivot, pivot - rotation * Vector3.forward * GetDistance(targetPlayer.CurrentVehicle));
        transform.rotation = rotation;
        initialized = true;
    }

    public void AddShake(float amplitude, float duration)
    {
        shakeAmplitude = Mathf.Max(shakeAmplitude, amplitude);
        shakeEndTime = Mathf.Max(shakeEndTime, Time.unscaledTime + duration);
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        InputEnabled = locked;
    }
}
