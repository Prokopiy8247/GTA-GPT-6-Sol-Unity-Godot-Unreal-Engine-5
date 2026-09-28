using UnityEngine;

/// <summary>Simple lane-loop driving using the shared drivable vehicle physics.</summary>
[RequireComponent(typeof(SandboxVehicle))]
public class SandboxTrafficAgent : MonoBehaviour
{
    public Transform[] Waypoints;
    public float CruiseSpeedKph = 34f;
    public bool Emergency;
    SandboxVehicle vehicle;
    int next;
    public int NextWaypoint { get => next; set => next = Waypoints != null && Waypoints.Length > 0 ? (value % Waypoints.Length + Waypoints.Length) % Waypoints.Length : 0; }

    void Awake() { vehicle = GetComponent<SandboxVehicle>(); }

    void FixedUpdate()
    {
        if (vehicle == null || Waypoints == null || Waypoints.Length < 2) return;
        var director = SandboxDirector.Instance;
        if (director != null && !director.TrafficEnabled && !Emergency) { vehicle.SetAIInput(0f, 0f, 1f); return; }
        if (director != null && director.Player != null && director.Player.CurrentVehicle == vehicle) { enabled = false; return; }
        if (director != null && director.Player != null && Vector3.Distance(transform.position, director.Player.transform.position) > 210f)
        { vehicle.SetAIInput(0f, 0f, 1f); return; }

        Vector3 target = Waypoints[next].position;
        Vector3 local = transform.InverseTransformPoint(target);
        if (local.magnitude < 9f || (local.z < -3f && local.magnitude < 20f)) next = (next + 1) % Waypoints.Length;
        float steering = Mathf.Clamp(local.x / Mathf.Max(4f, Mathf.Abs(local.z)), -1f, 1f);
        float targetSpeed = CruiseSpeedKph;
        if (Emergency && director != null && director.WantedLevel > 0)
        {
            Vector3 pursuit = director.InPursuit && director.Player != null ? director.Player.transform.position : director.LastKnownPosition;
            if (Vector3.Distance(transform.position, pursuit) < 95f)
            {
                local = transform.InverseTransformPoint(pursuit);
                steering = Mathf.Clamp(local.x / Mathf.Max(5f, Mathf.Abs(local.z)), -1f, 1f);
                targetSpeed = 70f;
            }
        }
        bool blocked = Physics.Raycast(transform.position + transform.forward * 2f + Vector3.up * .8f, transform.forward, out var hit, 9f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider.transform.root != transform.root && hit.collider.GetComponentInParent<SandboxVehicle>() != null;
        float brake = blocked ? 1f : vehicle.SpeedKph > targetSpeed + 5f ? .45f : 0f;
        vehicle.SetAIInput(blocked ? 0f : vehicle.SpeedKph < targetSpeed ? .68f : .04f, steering, brake);
    }
}
