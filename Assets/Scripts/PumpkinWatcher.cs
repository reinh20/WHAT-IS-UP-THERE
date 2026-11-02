using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the light cycle, warning flicker, red danger phase, player detection,
/// and jumpscare triggering for a pumpkin watcher hazard.
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
public class PumpkinWatcher : MonoBehaviour
{
    public enum WatcherState
    {
        SafeOrange,
        TransitionFlicker,
        DangerRed,
        JumpscareExecuting
    }

    [Header("Cycle Timers (Seconds)")]
    [Tooltip("Duration of the safe orange light phase before warning flicker")]
    public float safeDuration = 5.0f;
    [Tooltip("Duration of the warning flicker phase before turning red")]
    public float transitionDuration = 1.5f;
    [Tooltip("Duration of the dangerous red light phase where detection is active")]
    public float dangerDuration = 3.0f;

    [Header("Synchronization")]
    [Tooltip("Synchronize cycle with all other pumpkins using a shared clock")]
    public bool syncWithAllPumpkins = true;

    [Header("Light References")]
    [Tooltip("The main spotlight shining from the pumpkin. Auto-detected from children if empty.")]
    public Light spotLight;
    [Tooltip("The inner point light inside the pumpkin. Auto-detected from children if empty.")]
    public Light pointLight;

    [Header("Safe Light Settings")]
    [ColorUsage(false, true)]
    public Color safeLightColor = new Color(1.0f, 0.58f, 0.16f, 1.0f); // Warm flame orange
    public float safeSpotIntensity = 7500f;
    public float safePointIntensity = 500f;

    [Header("Danger Light Settings")]
    [ColorUsage(false, true)]
    public Color dangerLightColor = new Color(1.0f, 0.05f, 0.05f, 1.0f); // Pure sinister red
    public float dangerSpotIntensity = 10000f;
    public float dangerPointIntensity = 800f;

    [Header("Pumpkin Material Customization")]
    [Tooltip("The renderer on the pumpkin. Auto-detected if empty.")]
    public Renderer pumpkinRenderer;
    [Tooltip("Material slot index for the pumpkin face/eyes (default is index 1 for Material.002)")]
    public int faceMaterialIndex = 1;
    public Color safeFaceBaseColor = new Color(1.0f, 0.882f, 0.0f, 1.0f); // Warm yellow
    public Color safeFaceEmission = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    public Color dangerFaceBaseColor = new Color(1.0f, 0.05f, 0.05f, 1.0f); // Intense red
    [ColorUsage(true, true)]
    public Color dangerFaceEmission = new Color(4.0f, 0.2f, 0.2f, 1.0f); // High HDR red glow

    [Header("Player Detection")]
    [Tooltip("Layer mask used for line-of-sight raycasting (exclude triggers)")]
    public LayerMask detectionLayers = ~0;
    [Tooltip("Vertical offset from player origin to test line-of-sight (aims at chest/head)")]
    public float playerTargetHeightOffset = 1.0f;

    [Header("State (Read Only)")]
    [SerializeField] private WatcherState currentState = WatcherState.SafeOrange;
    [SerializeField] private float stateTimer = 0.0f;
    [SerializeField] private bool isPlayerDetected = false;

    // Material instances so we don't modify the source FBX asset
    private Material[] _instantiatedMaterials;
    private Material _faceMaterial;

    // Player cache
    private Transform _playerTransform;
    private Collider _playerCollider;

    // Shared global clock static state
    private static float _sharedTimer = 0.0f;
    private static WatcherState _sharedState = WatcherState.SafeOrange;
    private static int _syncMasterFrame = -1;

    public WatcherState CurrentState => currentState;
    public bool IsPlayerDetected => isPlayerDetected;

    private void Awake()
    {
        AutoFindReferences();
        InitMaterials();
    }

    private void Reset()
    {
        AutoFindReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoFindReferences();
        }
    }
#endif

    private void Start()
    {
        FindPlayer();
        ApplyStateVisuals(currentState, 1.0f);
    }

    private void AutoFindReferences()
    {
        if (pumpkinRenderer == null)
        {
            pumpkinRenderer = GetComponentInChildren<Renderer>();
        }

        if (spotLight == null)
        {
            Transform s = transform.Find("Pumpkin Spot Light");
            if (s == null) s = transform.Find("Pumpkin_SpotLight");
            if (s != null) spotLight = s.GetComponent<Light>();
        }

        if (pointLight == null)
        {
            Transform p = transform.Find("Pumpkin Point Light");
            if (p == null) p = transform.Find("Pumpkin_InnerGlow");
            if (p != null) pointLight = p.GetComponent<Light>();
        }

        // Capture initial intensities if not set
        if (spotLight != null && spotLight.intensity > 0)
        {
            safeSpotIntensity = spotLight.intensity;
            dangerSpotIntensity = safeSpotIntensity * 1.3f;
        }
        if (pointLight != null && pointLight.intensity > 0)
        {
            safePointIntensity = pointLight.intensity;
            dangerPointIntensity = safePointIntensity * 1.4f;
        }
    }

    private void InitMaterials()
    {
        if (pumpkinRenderer == null) return;

        // Accessing .materials instantiates unique copies for this renderer
        _instantiatedMaterials = pumpkinRenderer.materials;
        if (faceMaterialIndex >= 0 && faceMaterialIndex < _instantiatedMaterials.Length)
        {
            _faceMaterial = _instantiatedMaterials[faceMaterialIndex];
            if (_faceMaterial != null)
            {
                _faceMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    private void FindPlayer()
    {
        if (_playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
            {
                _playerTransform = pm.transform;
                _playerCollider = pm.GetComponent<Collider>();
            }
            else
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null)
                {
                    _playerTransform = p.transform;
                    _playerCollider = p.GetComponent<Collider>();
                }
            }
        }
    }

    private void Update()
    {
        if (currentState == WatcherState.JumpscareExecuting) return;

        if (syncWithAllPumpkins)
        {
            UpdateSharedCycle();
            currentState = _sharedState;
            stateTimer = _sharedTimer;
        }
        else
        {
            UpdateIndependentCycle();
        }

        UpdateVisuals();

        // During DangerRed state, evaluate player detection
        if (currentState == WatcherState.DangerRed)
        {
            CheckPlayerDetection();
        }
        else
        {
            isPlayerDetected = false;
        }
    }

    private void UpdateSharedCycle()
    {
        // Only one instance advances the shared timer per frame
        if (_syncMasterFrame != Time.frameCount)
        {
            _syncMasterFrame = Time.frameCount;
            _sharedTimer += Time.deltaTime;

            switch (_sharedState)
            {
                case WatcherState.SafeOrange:
                    if (_sharedTimer >= safeDuration)
                    {
                        _sharedTimer = 0.0f;
                        _sharedState = WatcherState.TransitionFlicker;
                    }
                    break;

                case WatcherState.TransitionFlicker:
                    if (_sharedTimer >= transitionDuration)
                    {
                        _sharedTimer = 0.0f;
                        _sharedState = WatcherState.DangerRed;
                    }
                    break;

                case WatcherState.DangerRed:
                    if (_sharedTimer >= dangerDuration)
                    {
                        _sharedTimer = 0.0f;
                        _sharedState = WatcherState.SafeOrange;
                    }
                    break;
            }
        }
    }

    private void UpdateIndependentCycle()
    {
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case WatcherState.SafeOrange:
                if (stateTimer >= safeDuration)
                {
                    stateTimer = 0.0f;
                    currentState = WatcherState.TransitionFlicker;
                }
                break;

            case WatcherState.TransitionFlicker:
                if (stateTimer >= transitionDuration)
                {
                    stateTimer = 0.0f;
                    currentState = WatcherState.DangerRed;
                }
                break;

            case WatcherState.DangerRed:
                if (stateTimer >= dangerDuration)
                {
                    stateTimer = 0.0f;
                    currentState = WatcherState.SafeOrange;
                }
                break;
        }
    }

    private void UpdateVisuals()
    {
        switch (currentState)
        {
            case WatcherState.SafeOrange:
                ApplyStateVisuals(WatcherState.SafeOrange, 1.0f);
                break;

            case WatcherState.TransitionFlicker:
                // Rapid erratic flickering on/off with noise
                float flicker = Mathf.PerlinNoise(Time.time * 28f, transform.position.x);
                bool lampOn = flicker > 0.35f && (Random.value > 0.15f);
                float intensityMult = lampOn ? Random.Range(0.4f, 1.2f) : 0.05f;

                // Color alternates with warning red flashes
                Color flickerColor = (flicker > 0.7f) 
                    ? Color.Lerp(safeLightColor, dangerLightColor, Random.Range(0.5f, 1f))
                    : safeLightColor;

                ApplyFlickerVisuals(flickerColor, intensityMult);
                break;

            case WatcherState.DangerRed:
                ApplyStateVisuals(WatcherState.DangerRed, 1.0f);
                break;
        }
    }

    private void ApplyStateVisuals(WatcherState state, float intensityMultiplier)
    {
        bool isDanger = (state == WatcherState.DangerRed);
        Color lightCol = isDanger ? dangerLightColor : safeLightColor;
        float spotInt = (isDanger ? dangerSpotIntensity : safeSpotIntensity) * intensityMultiplier;
        float pointInt = (isDanger ? dangerPointIntensity : safePointIntensity) * intensityMultiplier;

        if (spotLight != null)
        {
            spotLight.enabled = true;
            spotLight.color = lightCol;
            spotLight.intensity = spotInt;
        }

        if (pointLight != null)
        {
            pointLight.enabled = true;
            pointLight.color = lightCol;
            pointLight.intensity = pointInt;
        }

        if (_faceMaterial != null)
        {
            Color baseCol = isDanger ? dangerFaceBaseColor : safeFaceBaseColor;
            Color emissCol = isDanger ? dangerFaceEmission : safeFaceEmission;

            if (_faceMaterial.HasProperty("_BaseColor"))
                _faceMaterial.SetColor("_BaseColor", baseCol);
            if (_faceMaterial.HasProperty("_EmissionColor"))
                _faceMaterial.SetColor("_EmissionColor", emissCol * intensityMultiplier);
        }
    }

    private void ApplyFlickerVisuals(Color lightCol, float intensityMultiplier)
    {
        if (spotLight != null)
        {
            spotLight.enabled = intensityMultiplier > 0.1f;
            spotLight.color = lightCol;
            spotLight.intensity = safeSpotIntensity * intensityMultiplier;
        }

        if (pointLight != null)
        {
            pointLight.enabled = intensityMultiplier > 0.1f;
            pointLight.color = lightCol;
            pointLight.intensity = safePointIntensity * intensityMultiplier;
        }

        if (_faceMaterial != null)
        {
            Color baseCol = Color.Lerp(safeFaceBaseColor, dangerFaceBaseColor, intensityMultiplier > 0.7f ? 0.8f : 0.1f);
            Color emissCol = safeFaceEmission * intensityMultiplier * 2.0f;

            if (_faceMaterial.HasProperty("_BaseColor"))
                _faceMaterial.SetColor("_BaseColor", baseCol);
            if (_faceMaterial.HasProperty("_EmissionColor"))
                _faceMaterial.SetColor("_EmissionColor", emissCol);
        }
    }

    /// <summary>
    /// Checks whether the player is currently inside the spotlight's detection cone
    /// and has an unobstructed line of sight.
    /// </summary>
    private void CheckPlayerDetection()
    {
        FindPlayer();
        if (_playerTransform == null) return;

        Vector3 spotOrigin = (spotLight != null) ? spotLight.transform.position : transform.position;
        Vector3 spotForward = (spotLight != null) ? spotLight.transform.forward : -transform.up;
        float spotRange = (spotLight != null) ? spotLight.range : 35f;
        float halfAngle = (spotLight != null) ? (spotLight.spotAngle * 0.5f) : 38f;

        Vector3 playerTarget = _playerCollider != null 
            ? _playerCollider.bounds.center 
            : _playerTransform.position + Vector3.up * playerTargetHeightOffset;

        Vector3 toPlayer = playerTarget - spotOrigin;
        float distance = toPlayer.magnitude;

        // 1. Distance check
        if (distance > spotRange)
        {
            isPlayerDetected = false;
            return;
        }

        // 2. Cone Angle check
        float angle = Vector3.Angle(spotForward, toPlayer);
        if (angle > halfAngle)
        {
            isPlayerDetected = false;
            return;
        }

        // 3. Line of sight Raycast check (walls/obstacles block vision)
        if (Physics.Raycast(spotOrigin, toPlayer.normalized, out RaycastHit hit, distance, detectionLayers, QueryTriggerInteraction.Ignore))
        {
            bool hitPlayer = hit.transform.CompareTag("Player") 
                || hit.transform.root.CompareTag("Player") 
                || hit.transform.GetComponentInParent<PlayerMovement>() != null;

            if (hitPlayer)
            {
                isPlayerDetected = true;
                TriggerJumpscareSequence();
                return;
            }
        }

        isPlayerDetected = false;
    }

    private void TriggerJumpscareSequence()
    {
        currentState = WatcherState.JumpscareExecuting;

        // Ensure materials and lights reflect maximum horror state
        ApplyStateVisuals(WatcherState.DangerRed, 1.8f);

        Debug.Log($"[PumpkinWatcher] Player DETECTED by {gameObject.name}! Triggering Jumpscare & Game Over!");

        if (PumpkinJumpscareManager.Instance != null)
        {
            PumpkinJumpscareManager.Instance.TriggerJumpscare(this, _playerTransform != null ? _playerTransform.gameObject : null);
        }
        else
        {
            Debug.LogWarning("[PumpkinWatcher] PumpkinJumpscareManager instance not found! Responding with direct respawn.");
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RespawnPlayer();
            }
            ResetToInitialState();
        }
    }

    /// <summary>
    /// Resets the pumpkin cycle and lights back to the initial safe orange state.
    /// Called by the jumpscare manager after player respawns.
    /// </summary>
    public void ResetToInitialState()
    {
        currentState = WatcherState.SafeOrange;
        stateTimer = 0.0f;
        isPlayerDetected = false;

        _sharedState = WatcherState.SafeOrange;
        _sharedTimer = 0.0f;

        ApplyStateVisuals(WatcherState.SafeOrange, 1.0f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = (spotLight != null) ? spotLight.transform.position : transform.position;
        Vector3 forward = (spotLight != null) ? spotLight.transform.forward : -transform.up;
        float range = (spotLight != null) ? spotLight.range : 35f;
        float angle = (spotLight != null) ? spotLight.spotAngle : 75f;

        Gizmos.color = (currentState == WatcherState.DangerRed) ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawRay(origin, forward * range);

        // Draw cone circles
        float radiusAtEnd = Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * range;
        Vector3 endPoint = origin + forward * range;
        Gizmos.DrawWireSphere(endPoint, radiusAtEnd);

        if (isPlayerDetected && _playerTransform != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(origin, _playerTransform.position + Vector3.up * playerTargetHeightOffset);
        }
    }
}
