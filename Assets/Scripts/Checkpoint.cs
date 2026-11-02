using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Defines a checkpoint location in the level where the player will respawn.
/// Works with trigger colliders, solid floor colliders, or distance proximity.
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Optional spawn transform. If null, this checkpoint's transform is used.")]
    public Transform spawnPoint;
    [Tooltip("Vertical spawn offset added to the spawn point position")]
    public Vector3 spawnOffset = new Vector3(0f, 1.0f, 0f);

    [Tooltip("Snap respawn position precisely to the floor surface collider so feet stand on ground like game start")]
    public bool snapToFloorSurface = true;
    [Tooltip("If true, this checkpoint represents the level starting location and will match the player's initial spawn transform")]
    public bool isStartingCheckpoint = false;

    [Header("Detection Settings")]
    [Tooltip("Automatically activate if player is within this radius (useful if colliders are solid or complex)")]
    public bool enableProximityDetection = true;
    public float proximityRadius = 4.0f;

    [Header("Feedback")]
    [Tooltip("Whether this checkpoint is currently the active respawn point")]
    [SerializeField] private bool isActiveCheckpoint = false;
    public UnityEvent onCheckpointActivated;

    public bool IsActive => isActiveCheckpoint;

    public Vector3 RespawnPosition
    {
        get
        {
            if (isStartingCheckpoint && CheckpointManager.Instance != null && CheckpointManager.Instance.InitialSpawnPosition != Vector3.zero)
            {
                return CheckpointManager.Instance.InitialSpawnPosition;
            }

            Vector3 basePos = spawnPoint != null 
                ? spawnPoint.position + spawnOffset 
                : transform.position + spawnOffset;

            if (snapToFloorSurface)
            {
                Vector3 rayOrigin = new Vector3(basePos.x, basePos.y + 2.5f, basePos.z);
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 15f, ~0, QueryTriggerInteraction.Ignore);

                float highestFloorY = float.MinValue;
                bool foundFloor = false;

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerMovement>() != null)
                        continue;

                    if (hit.collider.isTrigger)
                        continue;

                    if (hit.point.y > highestFloorY)
                    {
                        highestFloorY = hit.point.y;
                        foundFloor = true;
                    }
                }

                if (foundFloor)
                {
                    // Player capsule height = 2, center = 0, half-height = 1.0f.
                    // Placing pivot at floorY + 1.015f places feet directly on floor surface with exact eye level.
                    return new Vector3(basePos.x, highestFloorY + 1.015f, basePos.z);
                }
            }

            return basePos;
        }
    }

    public Quaternion RespawnRotation
    {
        get
        {
            if (isStartingCheckpoint && CheckpointManager.Instance != null && CheckpointManager.Instance.InitialSpawnPosition != Vector3.zero)
            {
                return CheckpointManager.Instance.InitialSpawnRotation;
            }

            Quaternion rawRot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
            // Always sanitize rotation: only keep horizontal yaw (Y-axis).
            // Prevents X/Z model tilts (e.g. 270 deg Blender pitch) from flipping the player horizontally.
            return Quaternion.Euler(0f, rawRot.eulerAngles.y, 0f);
        }
    }

    private void Start()
    {
        // Auto register with manager if it exists
        if (CheckpointManager.Instance != null && isActiveCheckpoint)
        {
            CheckpointManager.Instance.SetActiveCheckpoint(this);
        }
    }

    private void Update()
    {
        if (isActiveCheckpoint || !enableProximityDetection) return;

        if (CheckpointManager.Instance != null && CheckpointManager.Instance.PlayerTransform != null)
        {
            float distSq = (CheckpointManager.Instance.PlayerTransform.position - transform.position).sqrMagnitude;
            if (distSq <= proximityRadius * proximityRadius)
            {
                ActivateCheckpoint();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActiveCheckpoint) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null)
        {
            ActivateCheckpoint();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isActiveCheckpoint) return;
        if (collision.collider.CompareTag("Player") || collision.collider.GetComponentInParent<PlayerMovement>() != null)
        {
            ActivateCheckpoint();
        }
    }

    public void ActivateCheckpoint()
    {
        if (isActiveCheckpoint) return;

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SetActiveCheckpoint(this);
        }

        isActiveCheckpoint = true;
        onCheckpointActivated?.Invoke();
        Debug.Log($"[Checkpoint] Activated: {gameObject.name} at {RespawnPosition}");
    }

    public void SetInactive()
    {
        isActiveCheckpoint = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActiveCheckpoint ? Color.green : new Color(0.2f, 0.8f, 1f, 0.6f);
        Vector3 spawnPos = RespawnPosition;
        Gizmos.DrawWireSphere(spawnPos, 0.5f);
        Gizmos.DrawRay(spawnPos, RespawnRotation * Vector3.forward * 1.5f);

        if (enableProximityDetection)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, proximityRadius);
        }
    }
}
