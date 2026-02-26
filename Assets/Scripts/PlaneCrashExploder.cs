using UnityEngine;

public class PlaneCrashExploder : MonoBehaviour
{
    [Header("Explosion Prefab")]
    [SerializeField] private ParticleSystem explosionPrefab;
    [SerializeField] private float explosionLifetime = 6f;

    [Header("Crash Detection")]
    [SerializeField] private float minImpactSpeed = 12f;
    [SerializeField] private LayerMask crashLayers = ~0;

    [Header("Disable on crash")]
    [SerializeField] private GameObject visualsRoot;
    [SerializeField] private MonoBehaviour[] disableScripts;
    [SerializeField] private Collider[] disableColliders;
    [SerializeField] private bool disableRigidbody = true;

    [Header("Explosion Sound")]
    [Tooltip("MUST be an AudioClip asset (not an AudioSource, not a prefab).")]
    [SerializeField] private AudioClip explosionSfx;

    [Tooltip("Base loudness of explosion before applying global SFX volume.")]
    [SerializeField, Range(0f, 1f)] private float baseSfxVolume = 1f;

    [Tooltip("Force explosion sound to be 2D (recommended so you always hear it).")]
    [SerializeField] private bool force2DExplosionSfx = true;

    private const string SfxVolumeKey = "sfx_volume";

    private Rigidbody rb;
    private bool exploded;

    // Dedicated one-shot source (doesn't interfere with engine audio)
    private AudioSource oneShotSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();

        if (disableColliders == null || disableColliders.Length == 0)
            disableColliders = GetComponentsInChildren<Collider>(true);

        // Create a dedicated one-shot AudioSource (very reliable)
        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = force2DExplosionSfx ? 0f : 1f;
        oneShotSource.dopplerLevel = 0f;
        oneShotSource.rolloffMode = AudioRolloffMode.Linear;
        oneShotSource.minDistance = 10f;
        oneShotSource.maxDistance = 800f;
        oneShotSource.mute = false;
        oneShotSource.priority = 64;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (exploded) return;

        if (((1 << col.gameObject.layer) & crashLayers.value) == 0)
            return;

        float impact = col.relativeVelocity.magnitude;
        if (impact < minImpactSpeed) return;

        Vector3 hitPoint = (col.contactCount > 0) ? col.GetContact(0).point : transform.position;
        Vector3 hitNormal = (col.contactCount > 0) ? col.GetContact(0).normal : Vector3.up;

        Explode(hitPoint, hitNormal, $"Impact {impact:0.0}");
    }

    private void Explode(Vector3 position, Vector3 normal, string reason)
    {
        exploded = true;

        // Particles
        if (explosionPrefab != null)
        {
            var ps = Instantiate(explosionPrefab, position, Quaternion.LookRotation(normal));
            ps.Play();
            Destroy(ps.gameObject, explosionLifetime);
        }

        // Sound (scaled by global SFX volume)
        PlayExplosionSfx();

        // Disable scripts
        if (disableScripts != null)
        {
            foreach (var mb in disableScripts)
                if (mb != null) mb.enabled = false;
        }

        // Disable colliders
        if (disableColliders != null)
        {
            foreach (var c in disableColliders)
                if (c != null) c.enabled = false;
        }

        // Hide visuals
        if (visualsRoot != null) visualsRoot.SetActive(false);
        else
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
        }

        // Stop physics
        if (disableRigidbody && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Optional: fail mission
        var judge = FindFirstObjectByType<LandingJudge>();
        if (judge != null)
            judge.FailMissionFromCrash(reason);

        Debug.Log($"💥 Plane exploded: {reason}");
    }

    private void PlayExplosionSfx()
    {
        if (explosionSfx == null)
        {
            Debug.LogError("PlaneCrashExploder: ExplosionSfx is NULL. Assign an AudioClip asset.");
            return;
        }

        float sfx01 = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
        float finalVol = Mathf.Clamp01(baseSfxVolume * sfx01);

        if (finalVol <= 0.001f)
        {
            Debug.LogWarning("PlaneCrashExploder: SFX volume is 0 (sfx_volume). Turn up the SFX slider.");
            return;
        }

        // Use OneShot (reliable, doesn't need clip swapping)
        if (oneShotSource != null)
            oneShotSource.PlayOneShot(explosionSfx, finalVol);
        else
            AudioSource.PlayClipAtPoint(explosionSfx, transform.position, finalVol);
    }
}