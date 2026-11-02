using UnityEngine;

/// <summary>
/// Controls the pumpkin lantern lighting, including the primary Spot Light,
/// an ambient inner glow Point Light, and a realistic candle/flame flicker effect.
/// </summary>
[SelectionBase]
[ExecuteAlways]
[DisallowMultipleComponent]
public class PumpkinLantern : MonoBehaviour
{
    public const string SpotLightName = "Pumpkin_SpotLight";
    public const string InnerGlowName = "Pumpkin_InnerGlow";

    [Header("Primary Spot Light")]
    [Tooltip("The main directional spotlight shining outward from the pumpkin")]
    public Light spotLight;
    [ColorUsage(false, true)]
    public Color lightColor = new Color(1.0f, 0.58f, 0.16f, 1.0f); // Warm flame amber
    public float spotIntensity = 5.0f;
    public float spotRange = 14.0f;
    [Range(10f, 120f)] public float spotAngle = 70.0f;
    [Range(0f, 120f)] public float innerSpotAngle = 35.0f;
    [Tooltip("Downward tilt (degrees) of the spot light when auto-placed, so it lights the floor in front of the pumpkin")]
    [Range(0f, 80f)] public float spotDownTilt = 20.0f;
    public LightShadows shadows = LightShadows.Soft;

    [Header("Inner Glow (Point Light)")]
    [Tooltip("Secondary subtle point light inside the pumpkin so its body glows warmly")]
    public Light pointLight;
    public bool enableInnerGlow = true;
    public float innerGlowIntensity = 1.6f;
    public float innerGlowRange = 3.5f;

    [Header("Atmospheric Flicker")]
    [Tooltip("Simulates a realistic flickering candle/torch flame inside the jack-o'-lantern")]
    public bool enableFlicker = true;
    [Range(0.5f, 25f)] public float flickerSpeed = 7.0f;
    [Range(0.02f, 0.5f)] public float flickerAmount = 0.22f;

    private float _noiseOffset;

    private void OnEnable()
    {
        _noiseOffset = Random.Range(0f, 500f);
        ApplySettings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Modifying other components directly inside OnValidate causes
        // "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate" warnings
        // (URP adds its additional light data). Defer the update to the next editor tick instead.
        UnityEditor.EditorApplication.delayCall -= DeferredApplySettings;
        UnityEditor.EditorApplication.delayCall += DeferredApplySettings;
    }

    private void DeferredApplySettings()
    {
        if (this == null) return; // Component was destroyed before the delayed call ran
        ApplySettings();
    }
#endif

    private void Update()
    {
        if (!enableFlicker || !Application.isPlaying) return;

        // Smooth natural candle flame flicker using Perlin noise
        float noise = Mathf.PerlinNoise((Time.time * flickerSpeed) + _noiseOffset, 0.0f);
        float multiplier = 1.0f + ((noise - 0.5f) * 2.0f * flickerAmount);

        if (spotLight != null)
        {
            spotLight.intensity = spotIntensity * multiplier;
        }

        if (pointLight != null && enableInnerGlow)
        {
            pointLight.intensity = innerGlowIntensity * multiplier;
        }
    }

    /// <summary>
    /// Applies the inspector lighting configuration to the attached Light components.
    /// </summary>
    public void ApplySettings()
    {
        if (spotLight != null)
        {
            spotLight.type = LightType.Spot;
            spotLight.color = lightColor;
            spotLight.intensity = spotIntensity;
            spotLight.range = spotRange;
            spotLight.spotAngle = spotAngle;
            spotLight.innerSpotAngle = Mathf.Min(innerSpotAngle, spotAngle);
            spotLight.shadows = shadows;
        }

        if (pointLight != null)
        {
            pointLight.type = LightType.Point;
            pointLight.color = lightColor;
            pointLight.intensity = innerGlowIntensity;
            pointLight.range = innerGlowRange;
            pointLight.shadows = LightShadows.None;
            pointLight.enabled = enableInnerGlow;
        }
    }

    /// <summary>
    /// Automatically discovers or instantiates the Spot Light and Point Light child objects
    /// and places them relative to the pumpkin mesh bounds (works with scaled/rotated FBX imports).
    /// </summary>
    [ContextMenu("Auto Setup Lights")]
    public void AutoSetupLights()
    {
        Bounds bounds = GetWorldBounds();
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        if (radius < 0.01f) radius = 0.5f;

        Vector3 face = GetFaceDirection();

        // 1. Spot Light setup - just outside the pumpkin face so the pumpkin mesh doesn't shadow it
        bool spotCreated;
        spotLight = FindOrCreateLight(SpotLightName, out spotCreated);
        if (spotCreated)
        {
            Vector3 dir = (face + Vector3.down * Mathf.Tan(spotDownTilt * Mathf.Deg2Rad)).normalized;
            spotLight.transform.position = center + face * (radius * 1.05f) + Vector3.up * (bounds.extents.y * 0.1f);
            spotLight.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        // 2. Point Light (Inner Glow) setup - centered inside the pumpkin
        bool glowCreated;
        pointLight = FindOrCreateLight(InnerGlowName, out glowCreated);
        if (glowCreated)
        {
            pointLight.transform.position = center;
        }

        ApplySettings();
    }

    private Light FindOrCreateLight(string childName, out bool created)
    {
        created = false;
        Transform t = transform.Find(childName);
        if (t == null)
        {
            GameObject go = new GameObject(childName);
            // worldPositionStays = true keeps a clean world transform even when the FBX root is scaled x100
            go.transform.SetParent(transform, true);
            t = go.transform;
            created = true;
        }

        Light l = t.GetComponent<Light>();
        if (l == null) l = t.gameObject.AddComponent<Light>();
        return l;
    }

    /// <summary>
    /// Combined world-space bounds of all renderers in the pumpkin hierarchy.
    /// </summary>
    public Bounds GetWorldBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(transform.position + Vector3.up * 0.5f, Vector3.one);
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    /// <summary>
    /// Horizontal direction the carved face is looking at.
    /// Handles Blender/Sketchfab FBX imports that come in with a -90 X rotation
    /// (in that case local forward points up/down and the face is along local -Y).
    /// </summary>
    public Vector3 GetFaceDirection()
    {
        Vector3 f = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (f.sqrMagnitude < 0.01f)
        {
            f = Vector3.ProjectOnPlane(-transform.up, Vector3.up);
        }
        if (f.sqrMagnitude < 0.01f)
        {
            f = Vector3.forward;
        }
        return f.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (spotLight != null)
        {
            Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.4f);
            Gizmos.DrawRay(spotLight.transform.position, spotLight.transform.forward * spotRange);
        }

        if (pointLight != null && enableInnerGlow)
        {
            Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.25f);
            Gizmos.DrawWireSphere(pointLight.transform.position, innerGlowRange);
        }
    }
}
