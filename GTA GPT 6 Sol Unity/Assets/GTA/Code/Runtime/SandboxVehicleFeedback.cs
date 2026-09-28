using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Local engine, horn, siren and damage feedback synthesized from project code.</summary>
[RequireComponent(typeof(SandboxVehicle))]
public class SandboxVehicleFeedback : MonoBehaviour
{
    static readonly Dictionary<string, AudioClip> generated = new Dictionary<string, AudioClip>();
    SandboxVehicle vehicle;
    AudioSource engine, siren;
    ParticleSystem smoke;
    float nextHorn;

    void Start()
    {
        vehicle = GetComponent<SandboxVehicle>();
        engine = gameObject.AddComponent<AudioSource>();
        engine.clip = Clip("engine", vehicle.Kind);
        engine.loop = true;
        engine.spatialBlend = 1f;
        engine.maxDistance = 90f;
        engine.volume = 0f;
        engine.Play();
        siren = gameObject.AddComponent<AudioSource>();
        siren.clip = Clip("siren", vehicle.Kind);
        siren.loop = true;
        siren.spatialBlend = 1f;
        siren.maxDistance = 140f;
        siren.volume = 0f;
        siren.Play();
        SetupSmoke();
    }

    void Update()
    {
        if (vehicle == null) return;
        float speed = vehicle.SpeedKph;
        engine.pitch = Mathf.Clamp(.65f + speed / (vehicle.Kind == SandboxVehicle.VehicleKind.Plane ? 170f : 110f), .6f, 2f);
        engine.volume = Mathf.MoveTowards(engine.volume, vehicle.IsOccupied || speed > 5f ? .24f + Mathf.Clamp01(speed / 150f) * .18f : .03f, Time.deltaTime);
        siren.volume = Mathf.MoveTowards(siren.volume, vehicle.SirenOn ? .4f : 0f, Time.deltaTime * 2f);
        if (smoke != null)
        {
            var emission = smoke.emission;
            emission.rateOverTime = vehicle.IsDestroyed ? 58f : vehicle.Health / vehicle.MaxHealth < .34f ? 14f : 0f;
            var main = smoke.main;
            main.startColor = vehicle.IsDestroyed ? new Color(.92f, .36f, .13f, .7f) : new Color(.28f, .28f, .28f, .65f);
        }
        if (vehicle.Driver != null && Keyboard.current != null && Keyboard.current.hKey.isPressed && Time.time > nextHorn)
        { nextHorn = Time.time + .35f; SandboxFX.Tone(transform.position + transform.forward * 2f, "horn", 260f, .3f, .38f); }
    }

    static AudioClip Clip(string key, SandboxVehicle.VehicleKind kind)
    {
        string id = key + kind;
        if (generated.TryGetValue(id, out var clip)) return clip;
        int rate = 16000, count = rate;
        float[] data = new float[count];
        float baseHz = kind == SandboxVehicle.VehicleKind.Helicopter ? 64f : kind == SandboxVehicle.VehicleKind.Plane ? 110f : kind == SandboxVehicle.VehicleKind.Boat ? 78f : 83f;
        uint seed = 6537;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            seed = seed * 1664525u + 1013904223u;
            float noise = ((seed >> 8) / 16777216f) * 2f - 1f;
            data[i] = key == "siren"
                ? Mathf.Sin(2f * Mathf.PI * (470f + Mathf.Sin(t * 2f * Mathf.PI * 1.1f) * 170f) * t) * .42f
                : (Mathf.Sin(2f * Mathf.PI * baseHz * t) * .47f + Mathf.Sin(2f * Mathf.PI * baseHz * 2f * t) * .21f + noise * .08f);
        }
        clip = AudioClip.Create("Harborline " + id, count, 1, rate, false);
        clip.SetData(data, 0);
        generated[id] = clip;
        return clip;
    }

    void SetupSmoke()
    {
        var go = new GameObject("Damage smoke and fire");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.2f, 1f);
        smoke = go.AddComponent<ParticleSystem>();
        var main = smoke.main;
        main.loop = true;
        main.startLifetime = 1.5f;
        main.startSpeed = 1.6f;
        main.startSize = .32f;
        main.maxParticles = 150;
        main.startColor = new Color(.27f, .27f, .27f, .7f);
        var emission = smoke.emission;
        emission.rateOverTime = 0f;
        var shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        if (shader != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = new Material(shader);
    }
}
