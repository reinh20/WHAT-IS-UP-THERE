using UnityEngine;

/// <summary>
/// Manages player checkpoints and coordinates player respawning.
/// Automatically records the initial spawn point if no checkpoint has been reached yet.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    private static CheckpointManager _instance;
    public static CheckpointManager Instance => _instance;

    [Header("Player Reference")]
    [Tooltip("The player GameObject to respawn. Auto-detected by PlayerMovement or 'Player' tag if null.")]
    [SerializeField] private GameObject player;

    [Header("Current Active Checkpoint")]
    [SerializeField] private Checkpoint activeCheckpoint;

    /// <summary>
    /// Event triggered whenever the player respawns at a checkpoint or initial spawn point.
    /// </summary>
    public static event System.Action OnPlayerRespawned;

    // Fallback initial start transform
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private PlayerMovement playerMovement;

    public Vector3 InitialSpawnPosition => initialSpawnPosition;
    public Quaternion InitialSpawnRotation => initialSpawnRotation;
    public Transform PlayerTransform => player != null ? player.transform : null;
    public Checkpoint ActiveCheckpoint => activeCheckpoint;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        FindPlayer();

        if (player != null)
        {
            initialSpawnPosition = player.transform.position;
            initialSpawnRotation = player.transform.rotation;
        }
    }

    private void Start()
    {
        if (initialSpawnPosition == Vector3.zero)
        {
            FindPlayer();
            if (player != null)
            {
                initialSpawnPosition = player.transform.position;
                initialSpawnRotation = player.transform.rotation;
            }
        }
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                player = playerMovement.gameObject;
            }
            else
            {
                player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerMovement = player.GetComponent<PlayerMovement>();
                }
            }
        }
        else if (playerMovement == null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    public void SetActiveCheckpoint(Checkpoint cp)
    {
        if (activeCheckpoint != null && activeCheckpoint != cp)
        {
            activeCheckpoint.SetInactive();
        }

        activeCheckpoint = cp;
    }

    public Vector3 GetRespawnPosition()
    {
        if (activeCheckpoint != null)
        {
            if (activeCheckpoint.isStartingCheckpoint && initialSpawnPosition != Vector3.zero)
            {
                return initialSpawnPosition;
            }
            return activeCheckpoint.RespawnPosition;
        }
        return initialSpawnPosition;
    }

    public Quaternion GetRespawnRotation()
    {
        if (activeCheckpoint != null)
        {
            if (activeCheckpoint.isStartingCheckpoint && initialSpawnPosition != Vector3.zero)
            {
                return initialSpawnRotation;
            }
            return activeCheckpoint.RespawnRotation;
        }
        return initialSpawnRotation;
    }

    [Header("Fall Off Map (Void Death)")]
    [Tooltip("Enable automatic respawn when player falls off the level into the void")]
    public bool enableFallDeath = true;
    [Tooltip("Y coordinate threshold below which the player is considered fallen off the map")]
    public float fallDeathYThreshold = -15.0f;
    [Tooltip("Delay in seconds after falling off the map before respawning")]
    public float fallDeathDelay = 3.0f;
    [Tooltip("Fade duration when player falls and respawns")]
    public float fallFadeDuration = 0.35f;

    private bool _isFallRespawning = false;

    private void Update()
    {
        if (enableFallDeath && !_isFallRespawning)
        {
            FindPlayer();
            if (player != null && player.transform.position.y < fallDeathYThreshold)
            {
                TriggerFallDeath();
            }
        }
    }

    /// <summary>
    /// Triggers death and respawn sequence when player falls off the map into the void.
    /// </summary>
    public void TriggerFallDeath()
    {
        if (_isFallRespawning) return;
        StartCoroutine(FallDeathRoutine());
    }

    private System.Collections.IEnumerator FallDeathRoutine()
    {
        _isFallRespawning = true;
        Debug.Log($"[CheckpointManager] Player fell off the map! (Y={player.transform.position.y:F1} < {fallDeathYThreshold}). Respawning...");

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Quick dark fade overlay if it exists
        GameObject overlayGo = GameObject.Find("RedScreenOverlay");
        UnityEngine.UI.Image overlay = overlayGo != null ? overlayGo.GetComponent<UnityEngine.UI.Image>() : null;

        if (overlay != null)
        {
            float elapsed = 0f;
            while (elapsed < fallFadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Clamp01(elapsed / fallFadeDuration);
                overlay.color = new Color(0.08f, 0.02f, 0.02f, a);
                yield return null;
            }
            overlay.color = new Color(0.08f, 0.02f, 0.02f, 1f);
        }

        // Hold on dark screen for the requested delay (3 seconds) before respawning
        yield return new WaitForSeconds(fallDeathDelay);

        // Teleport player to active checkpoint
        RespawnPlayer();

        // Reset all pumpkins in the level
        PumpkinWatcher[] allPumpkins = FindObjectsByType<PumpkinWatcher>(FindObjectsInactive.Include);
        foreach (PumpkinWatcher pw in allPumpkins)
        {
            pw.ResetToInitialState();
        }

        // Fade back in smoothly
        if (overlay != null)
        {
            float elapsed = 0f;
            while (elapsed < fallFadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = 1.0f - Mathf.Clamp01(elapsed / fallFadeDuration);
                overlay.color = new Color(0.08f, 0.02f, 0.02f, a);
                yield return null;
            }
            overlay.color = new Color(0.08f, 0.02f, 0.02f, 0f);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        _isFallRespawning = false;
    }

    /// <summary>
    /// Respawns the player at the current active checkpoint or initial spawn location.
    /// Resets physics velocity and restores camera orientation.
    /// </summary>
    public void RespawnPlayer()
    {
        FindPlayer();
        if (player == null)
        {
            Debug.LogError("[CheckpointManager] Cannot respawn: Player GameObject not found!");
            return;
        }

        if (initialSpawnPosition == Vector3.zero && player != null)
        {
            initialSpawnPosition = player.transform.position;
            initialSpawnRotation = player.transform.rotation;
        }

        Vector3 targetPos = GetRespawnPosition();
        Quaternion targetRot = GetRespawnRotation();

        if (playerMovement != null)
        {
            playerMovement.Teleport(targetPos, targetRot);
        }
        else
        {
            player.transform.position = targetPos;
            player.transform.rotation = Quaternion.identity;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
                rb.position = targetPos;
                rb.rotation = Quaternion.identity;
                rb.freezeRotation = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                rb.Sleep();
            }
            Physics.SyncTransforms();
        }

        Debug.Log($"[CheckpointManager] Player respawned at {targetPos}");
        OnPlayerRespawned?.Invoke();
    }
}
