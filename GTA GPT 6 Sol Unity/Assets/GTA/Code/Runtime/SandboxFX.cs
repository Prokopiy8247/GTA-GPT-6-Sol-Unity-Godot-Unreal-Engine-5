using System.Collections.Generic;
using UnityEngine;

/// <summary>Project-owned particles and synthesized one-shot sounds.</summary>
public static class SandboxFX
{
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    public static void Impact(Vector3 point, Color color, float size = .2f)
    {
        Emit(point, color, 8, size, .38f, 2.5f);
        Tone(point, "impact", 105f, .12f, .08f);
    }

    public static void Muzzle(Vector3 point, Vector3 direction)
    {
        Emit(point + direction * .16f, new Color(1f, .7f, .16f), 12, .15f, .09f, 4f);
        Tone(point, "shot", 62f, .19f, .2f);
    }

    public static void Explosion(Vector3 point, float radius = 6f, float damage = 65f)
    {
        Emit(point, new Color(1f, .37f, .09f), 70, .75f, .75f, 9f);
        Emit(point, new Color(.18f, .2f, .23f), 42, 1.1f, 2f, 4f);
        Tone(point, "boom", 38f, .55f, .65f);
        foreach (var col in Physics.OverlapSphere(point, radius))
        {
            float falloff = 1f - Mathf.Clamp01(Vector3.Distance(point, col.bounds.center) / radius);
            if (falloff <= 0f) continue;
            var ped = col.GetComponentInParent<SandboxPedestrian>();
            if (ped != null && !ped.IsDead) ped.TakeDamage(damage * falloff, col.bounds.center);
            var player = col.GetComponentInParent<SandboxPlayer>();
            if (player != null) player.Damage(damage * falloff);
            var vehicle = col.GetComponentInParent<SandboxVehicle>();
            if (vehicle != null) vehicle.Damage(damage * falloff);
            if (col.attachedRigidbody != null) col.attachedRigidbody.AddExplosionForce(950f, point, radius, 1f);
        }
        if (SandboxDirector.Instance != null) SandboxDirector.Instance.ReportCrime(30f, point, false);
    }

    static void Emit(Vector3 point, Color color, int count, float size, float lifetime, float speed)
    {
        var go = new GameObject("Transient VFX");
        go.transform.position = point;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = lifetime;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = .4f;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .2f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        if (shader != null) renderer.sharedMaterial = new Material(shader) { color = color };
        ps.Emit(count);
        Object.Destroy(go, lifetime + .3f);
    }

    public static void Tone(Vector3 point, string key, float frequency, float length, float volume)
    {
        if (!clips.TryGetValue(key, out var clip))
        {
            int samples = Mathf.CeilToInt(16000f * length);
            float[] data = new float[samples];
            uint seed = 17491;
            for (int i = 0; i < samples; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                float noise = ((seed >> 8) / 16777216f) * 2f - 1f;
                float t = i / 16000f;
                float env = Mathf.Pow(1f - i / (float)samples, key == "boom" ? 1.4f : 3f);
                data[i] = (Mathf.Sin(t * frequency * 6.28318f) * .35f + noise * .65f) * env;
            }
            clip = AudioClip.Create("Synth " + key, samples, 1, 16000, false);
            clip.SetData(data, 0);
            clips[key] = clip;
        }
        var go = new GameObject("Transient Sound");
        go.transform.position = point;
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = .8f;
        source.maxDistance = 100f;
        source.volume = volume;
        source.Play();
        Object.Destroy(go, length + .1f);
    }
}
