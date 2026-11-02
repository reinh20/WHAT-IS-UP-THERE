using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the chasing red pumpkin hazard (Pumpkin Chaser (Merah)).
/// Spawns 10 seconds after the player respawns, illuminates with ominous red light,
/// persistently pursues the player across the level, and always rotates to face them.
/// If it catches the player, it triggers the jumpscare sequence.
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
public class PumpkinChaser : MonoBehaviour
{
    public enum ChaserState
    {
        WaitingToSpawn,
        Chasing,
        CatchingPlayer
    }

    public enum SpawnMode
    {
        InitialPosition,
        BehindPlayer,
        CustomPoint
    }

    [Header("Spawn & Respawn Settings")]
    [Tooltip("Delay in seconds after player respawns before this pumpkin spawns and begins chasing (10 seconds)")]
    public float respawnDelay = 10f;
    [Tooltip("Whether to wait for the respawn delay when the game starts before spawning")]
    public bool waitOnGameStart = true;
    [SerializeField] private ChaserState currentState = ChaserState.WaitingToSpawn;
    [SerializeField] private float currentTimer = 0f;

    [Header("Spawn Position")]
    public SpawnMode spawnMode = SpawnMode.BehindPlayer;
    public Transform customSpawnPoint;
    [Tooltip("Distance behind the player to spawn when using BehindPlayer mode")]
    public float spawnDistanceBehind = 16f;
    [Tooltip("Height offset above player/ground when spawning behind player")]
    public float spawnHeightOffset = 1.8f;

    [Header("Chase Settings")]
    [Tooltip("Movement speed while pursuing the player")]
    public float chaseSpeed = 10f;
    [Tooltip("Smooth rotation speed to face towards player")]
    public float rotationSpeed = 8f;
    [Tooltip("Hover height above ground while flying")]
    public float hoverHeight = 1.2f;
    [Tooltip("Vertical floating bob amplitude")]
    public float bobAmplitude = 0.35f;
    [Tooltip("Vertical floating bob frequency")]
    public float bobFrequency = 2.5f;
    [Tooltip("Distance threshold to catch the player and trigger jumpscare")]
    public float catchDistance = 1.8f;

    [Header("Facing / Model Alignment")]
    [Tooltip("Model rotation offset for imported meshes (default 270, 0, 0 for Blender import)")]
    public Vector3 modelRotationOffset = new Vector3(270f, 0f, 0f);
    [Tooltip("Whether to track vertical pitch towards player or stay level")]
    public bool trackPitch = true;

    [Header("Red Lights & Visuals")]
    public Light spotLight;
    public Light pointLight;
    [ColorUsage(false, true)]
    public Color chaserRedColor = new Color(1f, 0.04f, 0.04f, 1f);
    public float spotIntensity = 6000f;
    public float pointIntensity = 800f;

    [Header("Jumpscare Audio")]
    [Tooltip("Jumpscare sound played when this chaser catches the player. Overrides the manager's default clip.")]
    public AudioClip jumpscareAudioClip;
    [Range(0f, 1f)] public float jumpscareAudioVolume = 1.0f;

    [Header("Horror Proximity Audio")]
    [Tooltip("Horror sound (horor_audio) played on loop while pursuing the player. Volume increases as it gets closer.")]
    public AudioClip proximityAudioClip;
    [Range(0f, 1f)] public float maxProximityVolume = 1.0f;
    [Tooltip("Maximum distance (in meters) where the horror sound begins playing faintly")]
    public float proximityMaxDistance = 50f;
    [Tooltip("Distance (in meters) where the horror sound reaches maximum volume")]
    public float proximityMinDistance = 4f;
    [Tooltip("How fast the volume transitions smoothly")]
    public float audioFadeSpeed = 4f;
    [Range(0f, 1f)]
    [Tooltip("3D spatial blend (0 = 2D stereo, 1 = 3D directional)")]
    public float proximitySpatialBlend = 0.8f;

    // Internal references
    private AudioSource _proximityAudioSource;
    private float _targetVolume = 0f;
    private GameObject _player;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Renderer _renderer;
    private Collider _collider;

    public ChaserState CurrentState => currentState;
    public float CurrentTimer => currentTimer;

    private void Awake()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();

        FindPlayer();
        EnsureLights();
        ApplyRedVisuals();
        EnsureProximityAudioSource();
    }

    private void Start()
    {
        if (waitOnGameStart)
        {
            HideChaser();
            currentState = ChaserState.WaitingToSpawn;
            currentTimer = respawnDelay;
        }
        else
        {
            SpawnChaser();
        }
    }

    private void OnEnable()
    {
        CheckpointManager.OnPlayerRespawned += HandlePlayerRespawned;
    }

    private void OnDisable()
    {
        CheckpointManager.OnPlayerRespawned -= HandlePlayerRespawned;
        StopProximityAudioInstant();
    }

    private void FindPlayer()
    {
        if (_player == null)
        {
            _player = GameObject.FindWithTag("Player");
            if (_player == null)
            {
                var pm = FindAnyObjectByType<PlayerMovement>();
                if (pm != null) _player = pm.gameObject;
            }
        }
    }

    private void EnsureLights()
    {
        if (spotLight == null)
        {
            Transform st = transform.Find("Pumpkin Spot Light");
            if (st != null) spotLight = st.GetComponent<Light>();
        }

        if (pointLight == null)
        {
            Transform pt = transform.Find("Pumpkin Point Light");
            if (pt != null) pointLight = pt.GetComponent<Light>();
        }
    }

    private void ApplyRedVisuals()
    {
        if (spotLight != null)
        {
            spotLight.color = chaserRedColor;
            spotLight.intensity = spotIntensity;
        }

        if (pointLight != null)
        {
            pointLight.color = chaserRedColor;
            pointLight.intensity = pointIntensity;
        }

        // Set eye and mouth emission to sinister red
        if (_renderer != null)
        {
            Material[] mats = _renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (i == 1 || mats[i].name.Contains("002"))
                {
                    mats[i].EnableKeyword("_EMISSION");
                    if (mats[i].HasProperty("_BaseColor"))
                        mats[i].SetColor("_BaseColor", chaserRedColor);
                    if (mats[i].HasProperty("_EmissionColor"))
                        mats[i].SetColor("_EmissionColor", chaserRedColor * 3.5f);
                }
            }
        }
    }

    /// <summary>
    /// Called when the player respawns (after dying or falling off the map).
    /// Resets the chaser and begins the 10-second countdown before re-spawning.
    /// </summary>
    public void HandlePlayerRespawned()
    {
        StopProximityAudioInstant();
        HideChaser();
        currentState = ChaserState.WaitingToSpawn;
        currentTimer = respawnDelay;
        Debug.Log($"[PumpkinChaser] Player respawned. Chaser will reappear in {respawnDelay} seconds.");
    }

    private void Update()
    {
        FindPlayer();
        UpdateProximityAudio();

        switch (currentState)
        {
            case ChaserState.WaitingToSpawn:
                currentTimer -= Time.deltaTime;
                if (currentTimer <= 0f)
                {
                    SpawnChaser();
                }
                break;

            case ChaserState.Chasing:
                UpdateChase();
                break;

            case ChaserState.CatchingPlayer:
                // Jumpscare is executing; chaser is stationary or hidden
                break;
        }
    }

    private void UpdateChase()
    {
        if (_player == null) return;

        Vector3 playerPos = _player.transform.position;

        // Hover target position with smooth floating bob
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        Vector3 targetPos = new Vector3(playerPos.x, playerPos.y + hoverHeight + bob, playerPos.z);

        // Move towards target position
        transform.position = Vector3.MoveTowards(transform.position, targetPos, chaseSpeed * Time.deltaTime);

        // Smoothly rotate to face directly towards the player
        Vector3 lookDir = (playerPos + Vector3.up * 1.0f) - transform.position;
        if (!trackPitch)
        {
            lookDir.y = 0f;
        }

        if (lookDir.sqrMagnitude > 0.001f)
        {
            // Apply model rotation offset (270 deg X) so the face and spotlight point forward
            Quaternion desiredRotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up) * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSpeed);
        }

        // Distance check for catching player
        float distToPlayer = Vector3.Distance(transform.position, playerPos);
        if (distToPlayer <= catchDistance)
        {
            CatchPlayer();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState != ChaserState.Chasing) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null)
        {
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        if (currentState == ChaserState.CatchingPlayer) return;
        currentState = ChaserState.CatchingPlayer;

        Debug.Log("[PumpkinChaser] Caught the player! Triggering jumpscare...");

        // Stop proximity audio immediately before triggering jumpscare
        StopProximityAudioInstant();

        // Trigger jumpscare sequence
        if (PumpkinJumpscareManager.Instance != null)
        {
            PumpkinJumpscareManager.Instance.TriggerJumpscare(gameObject, _player);
        }
        else if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer();
        }

        HideChaser();
    }

    private void SpawnChaser()
    {
        FindPlayer();

        Vector3 spawnPos = _initialPosition;

        if (spawnMode == SpawnMode.BehindPlayer && _player != null)
        {
            Vector3 playerFwd = _player.transform.forward;
            playerFwd.y = 0f;
            if (playerFwd.sqrMagnitude < 0.001f) playerFwd = Vector3.forward;
            playerFwd.Normalize();

            spawnPos = _player.transform.position - (playerFwd * spawnDistanceBehind) + Vector3.up * spawnHeightOffset;
        }
        else if (spawnMode == SpawnMode.CustomPoint && customSpawnPoint != null)
        {
            spawnPos = customSpawnPoint.position;
        }

        transform.position = spawnPos;

        // Face player immediately
        if (_player != null)
        {
            Vector3 lookDir = (_player.transform.position + Vector3.up * 1f) - transform.position;
            if (!trackPitch) lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up) * Quaternion.Euler(modelRotationOffset);
            }
        }

        ShowChaser();
        currentState = ChaserState.Chasing;
        Debug.Log($"[PumpkinChaser] Spawned at {spawnPos}! Now chasing player.");
    }

    private void ShowChaser()
    {
        if (_renderer != null) _renderer.enabled = true;
        if (_collider != null) _collider.enabled = true;
        if (spotLight != null) spotLight.enabled = true;
        if (pointLight != null) pointLight.enabled = true;
    }

    private void HideChaser()
    {
        StopProximityAudioInstant();
        if (_renderer != null) _renderer.enabled = false;
        if (_collider != null) _collider.enabled = false;
        if (spotLight != null) spotLight.enabled = false;
        if (pointLight != null) pointLight.enabled = false;
    }

    private void EnsureProximityAudioSource()
    {
        if (_proximityAudioSource == null)
        {
            Transform existingTr = transform.Find("ChaserProximityAudio");
            if (existingTr != null)
            {
                _proximityAudioSource = existingTr.GetComponent<AudioSource>();
            }

            if (_proximityAudioSource == null)
            {
                GameObject audioGo = new GameObject("ChaserProximityAudio");
                audioGo.transform.SetParent(transform, false);
                _proximityAudioSource = audioGo.AddComponent<AudioSource>();
            }

            _proximityAudioSource.playOnAwake = false;
            _proximityAudioSource.loop = true;
            _proximityAudioSource.spatialBlend = proximitySpatialBlend;
            _proximityAudioSource.rolloffMode = AudioRolloffMode.Linear;
            _proximityAudioSource.minDistance = proximityMinDistance;
            _proximityAudioSource.maxDistance = proximityMaxDistance;
            _proximityAudioSource.volume = 0f;
        }

        if (proximityAudioClip == null)
        {
#if UNITY_EDITOR
            proximityAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/asset/sound/horor_audio.mp3");
#endif
        }

        if (_proximityAudioSource != null && proximityAudioClip != null)
        {
            if (_proximityAudioSource.clip != proximityAudioClip)
            {
                _proximityAudioSource.clip = proximityAudioClip;
            }
        }
    }

    private void UpdateProximityAudio()
    {
        if (_proximityAudioSource == null) return;

        if (currentState == ChaserState.Chasing && _player != null)
        {
            float dist = Vector3.Distance(transform.position, _player.transform.position);

            // InverseLerp: 0 at proximityMaxDistance, 1 at proximityMinDistance
            float factor = Mathf.InverseLerp(proximityMaxDistance, proximityMinDistance, dist);

            // Exponential curve for dramatic horror tension as it gets closer
            float curve = Mathf.Pow(factor, 1.4f);
            _targetVolume = curve * maxProximityVolume;

            // Silence if jumpscare or finish is active
            if (PumpkinJumpscareManager.Instance != null && PumpkinJumpscareManager.Instance.IsJumpscareActive)
            {
                _targetVolume = 0f;
            }
            if (FinishLine.IsGameFinished)
            {
                _targetVolume = 0f;
            }
        }
        else
        {
            _targetVolume = 0f;
        }

        // Start playing loop if volume is required
        if (_targetVolume > 0.001f && !_proximityAudioSource.isPlaying && _proximityAudioSource.clip != null)
        {
            _proximityAudioSource.Play();
        }

        // Smooth volume interpolation
        _proximityAudioSource.volume = Mathf.MoveTowards(_proximityAudioSource.volume, _targetVolume, audioFadeSpeed * Time.deltaTime);

        // Stop playing if completely silent
        if (_proximityAudioSource.volume <= 0.0001f && _targetVolume <= 0.0001f && _proximityAudioSource.isPlaying)
        {
            _proximityAudioSource.Stop();
        }
    }

    private void StopProximityAudioInstant()
    {
        _targetVolume = 0f;
        if (_proximityAudioSource != null)
        {
            _proximityAudioSource.volume = 0f;
            _proximityAudioSource.Stop();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Max distance where sound starts (faint)
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, proximityMaxDistance);

        // Min distance where sound is maximum volume
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, proximityMinDistance);

        // Catch radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}
