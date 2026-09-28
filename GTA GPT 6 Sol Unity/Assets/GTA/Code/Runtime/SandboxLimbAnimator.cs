using System;
using UnityEngine;

/// <summary>Animates segmented Blender character meshes without an armature.</summary>
[DisallowMultipleComponent]
public sealed class SandboxLimbAnimator : MonoBehaviour
{
    [SerializeField] private float walkSwingDegrees = 23f;
    [SerializeField] private float runSwingDegrees = 34f;
    [SerializeField] private float bobHeight = 0.045f;

    private Transform visualRoot;
    private CharacterController controller;
    private SandboxPlayer player;
    private Transform leftArm;
    private Transform rightArm;
    private Transform leftLeg;
    private Transform rightLeg;
    private Quaternion leftArmRest;
    private Quaternion rightArmRest;
    private Quaternion leftLegRest;
    private Quaternion rightLegRest;
    private Vector3 visualRestPosition;
    private Vector3 previousPosition;
    private float cycle;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddToSegmentedCharacters()
    {
        foreach (CharacterController character in FindObjectsByType<CharacterController>())
        {
            if (character.GetComponent<SandboxLimbAnimator>() != null) continue;
            bool hasLimb = false;
            foreach (Transform child in character.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith("Arm | sleeve", StringComparison.OrdinalIgnoreCase)
                    || child.name.StartsWith("Leg | trousers", StringComparison.OrdinalIgnoreCase)
                    || child.name.StartsWith("Leg | calf", StringComparison.OrdinalIgnoreCase)) { hasLimb = true; break; }
            }
            if (hasLimb) character.gameObject.AddComponent<SandboxLimbAnimator>();
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        controller = GetComponent<CharacterController>();
        player = GetComponent<SandboxPlayer>();
        visualRoot = player != null && player.VisualRoot != null ? player.VisualRoot : transform.childCount > 0 ? transform.GetChild(0) : null;
        if (visualRoot == null) return;

        Transform[] pieces = visualRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform piece in pieces)
        {
            if (piece.name.StartsWith("Arm | sleeve", StringComparison.OrdinalIgnoreCase))
            {
                Transform pivot = MakePivot(piece, true);
                if (Side(piece) < 0f) leftArm = pivot;
                else rightArm = pivot;
            }
            else if (piece.name.StartsWith("Leg | trousers", StringComparison.OrdinalIgnoreCase)
                     || piece.name.StartsWith("Leg | calf", StringComparison.OrdinalIgnoreCase))
            {
                Transform pivot = MakePivot(piece, false);
                if (Side(piece) < 0f) leftLeg = pivot;
                else rightLeg = pivot;
            }
        }

        // Hands and shoes follow the corresponding upper segment rather than floating in place.
        foreach (Transform piece in pieces)
        {
            if (piece.name.StartsWith("Arm | hand", StringComparison.OrdinalIgnoreCase))
            {
                Transform destination = Side(piece) < 0f ? leftArm : rightArm;
                if (destination != null) piece.SetParent(destination, true);
            }
            else if (piece.name.StartsWith("Foot |", StringComparison.OrdinalIgnoreCase))
            {
                Transform destination = Side(piece) < 0f ? leftLeg : rightLeg;
                if (destination != null) piece.SetParent(destination, true);
            }
        }

        leftArmRest = leftArm != null ? leftArm.localRotation : Quaternion.identity;
        rightArmRest = rightArm != null ? rightArm.localRotation : Quaternion.identity;
        leftLegRest = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
        rightLegRest = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
        visualRestPosition = visualRoot.localPosition;
        previousPosition = transform.position;
        initialized = leftArm != null || rightArm != null || leftLeg != null || rightLeg != null;
    }

    private Transform MakePivot(Transform piece, bool arm)
    {
        if (piece.parent != null && piece.parent.name.StartsWith(arm ? "Shoulder pivot | " : "Hip pivot | ", StringComparison.OrdinalIgnoreCase))
            return piece.parent;
        Renderer renderer = piece.GetComponent<Renderer>();
        if (renderer == null) return null;
        Bounds bounds = renderer.bounds;
        Vector3 pivotPosition = new Vector3(bounds.center.x, bounds.max.y - (arm ? 0.08f : 0.04f), bounds.center.z);
        Transform parent = piece.parent;
        GameObject helper = new GameObject((arm ? "Shoulder pivot | " : "Hip pivot | ") + (Side(piece) < 0f ? "L" : "R"));
        Transform pivot = helper.transform;
        pivot.SetParent(parent, false);
        pivot.position = pivotPosition;
        pivot.rotation = parent.rotation;
        piece.SetParent(pivot, true);
        return pivot;
    }

    private float Side(Transform piece)
    {
        Renderer renderer = piece.GetComponent<Renderer>();
        Vector3 point = renderer != null ? renderer.bounds.center : piece.position;
        return transform.InverseTransformPoint(point).x;
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        if (dt <= 0f) return;
        bool riding = player != null && player.IsInVehicle;
        float speed = controller != null && controller.enabled ? controller.velocity.magnitude : (transform.position - previousPosition).magnitude / dt;
        previousPosition = transform.position;
        if (riding) speed = 0f;
        bool swimming = player != null && player.IsSwimming;
        float intensity = Mathf.Clamp01(speed / 4f);
        float maxSwing = speed > 5.3f ? runSwingDegrees : walkSwingDegrees;
        if (player != null && (player.IsStealth || player.IsCrouched || player.IsInCover)) maxSwing *= 0.56f;
        if (swimming) maxSwing = 28f;
        cycle += dt * (3.1f + speed * 1.55f);
        float swing = Mathf.Sin(cycle) * maxSwing * intensity;
        float idleArm = Mathf.Sin(Time.time * 1.7f) * 1.6f;
        float blend = 1f - Mathf.Exp(-10f * dt);
        SetRotation(leftLeg, leftLegRest, swing, blend);
        SetRotation(rightLeg, rightLegRest, -swing, blend);
        SetRotation(leftArm, leftArmRest, -swing * 0.68f + idleArm, blend);
        SetRotation(rightArm, rightArmRest, swing * 0.68f - idleArm, blend);
        Vector3 targetPosition = visualRestPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(cycle)) * bobHeight * intensity);
        visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPosition, blend);
    }

    private static void SetRotation(Transform limb, Quaternion rest, float xDegrees, float blend)
    {
        if (limb != null) limb.localRotation = Quaternion.Slerp(limb.localRotation, rest * Quaternion.Euler(xDegrees, 0f, 0f), blend);
    }
}
