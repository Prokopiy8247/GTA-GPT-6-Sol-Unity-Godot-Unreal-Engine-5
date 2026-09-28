using System;
using UnityEngine;

/// <summary>Lightweight, distance-throttled street life and foot-police behaviour.</summary>
[RequireComponent(typeof(CharacterController))]
public class SandboxPedestrian : MonoBehaviour
{
    public enum ActorRole { Civilian, Police, Shopkeeper, Tactical }
    public ActorRole Role = ActorRole.Civilian;
    public float Health = 100f;
    public float WalkSpeed = 1.65f;
    public float RunSpeed = 4.3f;
    public float RoamRadius = 28f;
    public Transform VisualRoot;
    public bool IsDead => Health <= 0f;

    CharacterController controller;
    Vector3 home, destination;
    float decisionAt, nextShot, panicUntil, deathAt, witnessAt;
    bool reporting;
    Vector3 velocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = .31f;
        controller.center = Vector3.up * .9f;
        home = transform.position;
        destination = home;
        decisionAt = Time.time + UnityEngine.Random.Range(.4f, 2f);
    }

    void Update()
    {
        if (IsDead)
        {
            if (Time.time > deathAt) Destroy(gameObject);
            return;
        }
        SandboxPlayer player = SandboxDirector.Instance != null ? SandboxDirector.Instance.Player : null;
        if (player == null) return;
        float distance = Vector3.Distance(player.transform.position, transform.position);
        if (distance > 190f) return;
        // Far actors retain visual presence, but skip most decisions and movement frames.
        if (distance > 95f && Time.frameCount % 5 != Mathf.Abs(gameObject.name.GetHashCode() % 5)) return;

        if (reporting && Time.time >= witnessAt)
        {
            reporting = false;
            if (distance < 75f && SandboxDirector.Instance != null)
                SandboxDirector.Instance.ReportCrime(16f, transform.position, true);
        }

        Vector3 target = destination;
        bool alarmed = Time.time < panicUntil;
        if (Role == ActorRole.Police || Role == ActorRole.Tactical)
        {
            var director = SandboxDirector.Instance;
            if (director != null && director.WantedLevel > 0)
            {
                target = director.InPursuit ? player.transform.position : director.LastKnownPosition;
                if (distance < 34f && CanSee(player.transform.position + Vector3.up, distance))
                {
                    director.PoliceSighted(player.transform.position);
                    if (director.WantedLevel >= 2 && distance < 29f && Time.time > nextShot)
                    {
                        nextShot = Time.time + (Role == ActorRole.Tactical ? .7f : 1.3f);
                        player.Damage(Role == ActorRole.Tactical ? 9f : 5f);
                        SandboxFX.Muzzle(transform.position + Vector3.up * 1.45f, (player.transform.position - transform.position).normalized);
                    }
                }
            }
        }
        else if (alarmed)
        {
            Vector3 away = transform.position - player.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < .01f) away = transform.forward;
            target = transform.position + away.normalized * 20f;
        }
        else if (Time.time > decisionAt || Vector3.Distance(transform.position, destination) < 1.4f)
        {
            decisionAt = Time.time + UnityEngine.Random.Range(3f, 7f);
            Vector2 r = UnityEngine.Random.insideUnitCircle * RoamRadius;
            destination = home + new Vector3(r.x, 0f, r.y);
            destination.x = Mathf.Clamp(destination.x, -287f, 287f);
            destination.z = Mathf.Clamp(destination.z, -287f, 287f);
            target = destination;
        }

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.magnitude > .65f)
        {
            direction.Normalize();
            if (Physics.Raycast(transform.position + Vector3.up * .7f, direction, out var hit, .9f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.gameObject != gameObject)
                direction = Vector3.Cross(Vector3.up, direction).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 7f);
            velocity = direction * (alarmed || Role == ActorRole.Tactical ? RunSpeed : WalkSpeed);
            controller.Move((velocity + Vector3.down * 4f) * Time.deltaTime);
            if (VisualRoot != null) VisualRoot.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 10f) * 2f, 0f, 0f);
        }
    }

    bool CanSee(Vector3 point, float distance)
    {
        Vector3 eye = transform.position + Vector3.up * 1.55f;
        Vector3 ray = point - eye;
        if (distance > 12f && Vector3.Dot(transform.forward, ray.normalized) < -.12f) return false;
        return !Physics.Raycast(eye, ray.normalized, ray.magnitude, ~0, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(eye, ray.normalized, out var hit, ray.magnitude, ~0, QueryTriggerInteraction.Ignore)
               && hit.collider.GetComponentInParent<SandboxPlayer>() != null;
    }

    public void Panic(float seconds = 8f)
    {
        if (Role == ActorRole.Police || Role == ActorRole.Tactical || IsDead) return;
        panicUntil = Mathf.Max(panicUntil, Time.time + seconds);
    }

    public void WitnessCrime()
    {
        if (Role == ActorRole.Police || Role == ActorRole.Tactical || IsDead) return;
        Panic(12f);
        reporting = true;
        witnessAt = Time.time + UnityEngine.Random.Range(2.4f, 4.5f);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, bool fromPlayer = true)
    {
        if (IsDead) return;
        Health -= amount;
        Panic(16f);
        SandboxFX.Impact(hitPoint, new Color(.9f, .13f, .11f), .22f);
        if (fromPlayer && SandboxDirector.Instance != null)
            SandboxDirector.Instance.ReportCrime(Role == ActorRole.Police || Role == ActorRole.Tactical ? 52f : 27f, transform.position, Role == ActorRole.Police || Role == ActorRole.Tactical);
        if (Health <= 0f)
        {
            deathAt = Time.time + 16f;
            reporting = false;
            if (controller != null) controller.enabled = false;
            var capsule = GetComponent<Collider>();
            if (capsule != null) capsule.enabled = false;
            transform.rotation = Quaternion.Euler(75f, transform.eulerAngles.y, 0f);
            if (Role == ActorRole.Civilian && UnityEngine.Random.value < .35f && SandboxDirector.Instance != null)
                SandboxDirector.Instance.AwardCash(15);
        }
    }
}
