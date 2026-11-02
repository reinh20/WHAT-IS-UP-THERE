// Rigidbody FPS Wall Run (Dani / Karlson style)
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerMovement))]
public class WallRun : MonoBehaviour
{
    [Header("Wall Detection")]
    [Tooltip("Layers recognized as walls. If set to Default or Nothing, any vertical surface will be automatically detected.")]
    public LayerMask whatIsWall;
    [Tooltip("Layers recognized as ground for height checks")]
    public LayerMask whatIsGround;
    [Tooltip("Distance of raycasts to detect walls on left and right sides")]
    public float wallCheckDistance = 1.6f;
    [Tooltip("Minimum height above ground required to initiate wall run")]
    public float minJumpHeight = 0.5f;
    [Tooltip("Maximum surface normal Y deviation to count as vertical wall (e.g. 0.3)")]
    public float maxWallNormalY = 0.35f;

    [Header("Wallrunning Physics")]
    [Tooltip("Force propelling player forward along the wall surface")]
    public float wallRunForce = 2400f;
    [Tooltip("Max horizontal speed while wall running")]
    public float maxWallRunSpeed = 22f;
    [Tooltip("Force pulling player against the wall to maintain contact")]
    public float wallStickForce = 150f;
    [Tooltip("Gentle downward drift velocity while wall running (e.g. 2.0 m/s)")]
    public float wallRunGravity = 2.0f;
    [Tooltip("Maximum duration the player can stay on the wall (in seconds)")]
    public float maxWallRunTime = 3.0f;

    [Header("Wall Jump")]
    [Tooltip("Upward impulse when jumping off a wall")]
    public float wallJumpUpForce = 14f;
    [Tooltip("Outward impulse away from the wall normal")]
    public float wallJumpSideForce = 14f;
    [Tooltip("Forward impulse in the direction you are facing")]
    public float wallJumpForwardForce = 8f;
    [Tooltip("Cooldown after wall jumping before you can re-attach to a wall")]
    public float exitWallDuration = 0.25f;

    [Header("Camera Tilt")]
    [Tooltip("Target roll angle when running on wall (positive for right wall, negative for left wall)")]
    public float tiltAngle = 12f;
    [Tooltip("Speed of camera tilt interpolation")]
    public float tiltSpeed = 10f;

    [Header("Audio Feedback")]
    [Tooltip("Dedicated audio source for wall running friction/footsteps")]
    public AudioSource wallRunAudioSource;
    public AudioClip wallRunClip;
    [Range(0f, 1f)]
    public float wallRunVolume = 0.75f;

    [Header("Runtime State (Read Only)")]
    public bool isWallRunning;
    public bool wallLeft;
    public bool wallRight;
    public Vector3 wallNormal;

    // References
    private Rigidbody rb;
    private PlayerMovement pm;
    private Transform orientation;
    private CapsuleCollider col;

    // Detection hits
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    // Timers
    private float wallRunTimer;
    private float wallRunTimeOnWall;
    private bool exitingWall;
    private float exitWallTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        col = GetComponent<CapsuleCollider>();

        if (pm != null && pm.orientation != null)
        {
            orientation = pm.orientation;
        }

        // Auto-configure layers if not set
        if (whatIsWall.value == 0)
        {
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer != -1)
            {
                whatIsWall = (1 << wallLayer) | (1 << 0); // Wall + Default
            }
            else
            {
                whatIsWall = ~0; // Everything
            }
        }

        if (whatIsGround.value == 0 && pm != null)
        {
            whatIsGround = pm.whatIsGround;
        }

        InitAudio();
    }

    private void Start()
    {
        if (orientation == null && pm != null)
        {
            orientation = pm.orientation;
        }

        InitAudio();
    }

    private void Update()
    {
        if (orientation == null && pm != null)
        {
            orientation = pm.orientation;
        }

        CheckForWalls();
        StateMachine();
        HandleCameraTilt();
        HandleAudio();
    }

    private void FixedUpdate()
    {
        if (isWallRunning)
        {
            WallRunMovement();
        }
    }

    /// <summary>
    /// Checks for valid vertical wall surfaces on both left and right sides.
    /// Uses a combination of SphereCast and multi-angle Raycasts so detection works
    /// reliably whether the player is running straight, approaching at an angle, or looking slightly towards/away.
    /// </summary>
    private void CheckForWalls()
    {
        if (orientation == null)
        {
            if (pm == null) pm = GetComponent<PlayerMovement>();
            if (pm != null && pm.orientation != null) orientation = pm.orientation;
            else orientation = transform;
        }

        Vector3 origin = transform.position;

        // Check right side
        wallRight = CheckSide(origin, orientation.right, out rightWallHit);

        // Check left side
        wallLeft = CheckSide(origin, -orientation.right, out leftWallHit);
    }

    private bool CheckSide(Vector3 origin, Vector3 sideDir, out RaycastHit bestHit)
    {
        bestHit = default;
        float castDist = Mathf.Max(wallCheckDistance, 1.4f);

        // 1. SphereCast along direct side (forgiving volume detection)
        if (Physics.SphereCast(origin, 0.25f, sideDir, out RaycastHit sphereHit, castDist, whatIsWall))
        {
            if (IsValidWall(sphereHit))
            {
                bestHit = sphereHit;
                return true;
            }
        }

        // 2. Direct lateral raycast
        if (Physics.Raycast(origin, sideDir, out RaycastHit directHit, castDist, whatIsWall))
        {
            if (IsValidWall(directHit))
            {
                bestHit = directHit;
                return true;
            }
        }

        // 3. Forward-angled raycast (~35 degrees forward-lateral) for angled approaches
        Vector3 fwdAngle = (sideDir + orientation.forward * 0.7f).normalized;
        if (Physics.Raycast(origin, fwdAngle, out RaycastHit fwdHit, castDist * 1.1f, whatIsWall))
        {
            if (IsValidWall(fwdHit))
            {
                bestHit = fwdHit;
                return true;
            }
        }

        // 4. Forward-offset direct raycast (player running parallel to wall)
        Vector3 fwdOffsetOrigin = origin + orientation.forward * 0.35f;
        if (Physics.Raycast(fwdOffsetOrigin, sideDir, out RaycastHit fwdOffsetHit, castDist, whatIsWall))
        {
            if (IsValidWall(fwdOffsetHit))
            {
                bestHit = fwdOffsetHit;
                return true;
            }
        }

        // 5. Back-angled raycast (~25 degrees back-lateral)
        Vector3 backAngle = (sideDir - orientation.forward * 0.4f).normalized;
        if (Physics.Raycast(origin, backAngle, out RaycastHit backHit, castDist, whatIsWall))
        {
            if (IsValidWall(backHit))
            {
                bestHit = backHit;
                return true;
            }
        }

        return false;
    }

    private bool IsValidWall(RaycastHit hit)
    {
        if (hit.collider == null) return false;
        if (hit.collider.isTrigger) return false;
        // Surface normal Y must be low (near vertical wall, not floor or ceiling)
        if (Mathf.Abs(hit.normal.y) > maxWallNormalY) return false;
        return true;
    }

    /// <summary>
    /// Checks if player is at least minJumpHeight above the ground.
    /// </summary>
    private bool AboveGround()
    {
        float bottomOffset = 1.0f;
        if (col != null)
        {
            bottomOffset = (col.height * 0.5f) - col.center.y;
        }

        // If raycast from center downward past bottom hits ground within minJumpHeight, player is too close to ground
        return !Physics.Raycast(transform.position, Vector3.down, bottomOffset + minJumpHeight, whatIsGround);
    }

    private void StateMachine()
    {
        // Handle exit wall cooldown timer
        if (exitingWall)
        {
            exitWallTimer -= Time.deltaTime;
            if (exitWallTimer <= 0f)
            {
                exitingWall = false;
            }
        }

        float verticalInput = pm != null ? pm.VerticalInput : 0f;
        bool jumpDownInput = pm != null ? pm.JumpDownInput : false;
#if ENABLE_INPUT_SYSTEM
        if (pm == null && UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) verticalInput = 1f;
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) verticalInput = -1f;
            if (kb.spaceKey.wasPressedThisFrame) jumpDownInput = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (pm == null)
            {
                verticalInput = Input.GetAxisRaw("Vertical");
                jumpDownInput = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
            }
        }
        catch { }
#endif

        // Conditions to initiate or sustain wall running:
        // 1. A valid wall is on left or right
        // 2. Player is holding forward (W / Vertical > 0)
        // 3. Player is in the air above ground
        // 4. Player is not in exit cooldown
        // 5. Player is not grounded
        bool canWallRun = (wallLeft || wallRight) && verticalInput > 0.05f && AboveGround() && !exitingWall && (pm == null || !pm.grounded);

        if (canWallRun)
        {
            if (!isWallRunning)
            {
                StartWallRun();
            }

            wallRunTimeOnWall += Time.deltaTime;

            // Wall run timer countdown
            if (wallRunTimer > 0f)
            {
                wallRunTimer -= Time.deltaTime;
            }

            if (wallRunTimer <= 0f && isWallRunning)
            {
                exitingWall = true;
                exitWallTimer = exitWallDuration;
                StopWallRun();
            }

            // Wall Jump check:
            // Requires a fresh jump press (not holding down from floor jump) and brief attachment grace period
            if (jumpDownInput && wallRunTimeOnWall >= 0.05f)
            {
                WallJump();
            }
        }
        else
        {
            if (isWallRunning)
            {
                StopWallRun();
            }
        }
    }

    private void StartWallRun()
    {
        isWallRunning = true;
        if (pm != null) pm.isWallRunning = true;

        wallRunTimer = maxWallRunTime;
        wallRunTimeOnWall = 0f;

        // Disable standard gravity while wall running for smooth Karlson-style glide
        if (rb != null)
        {
            rb.useGravity = false;
        }

        // Catch momentum: neutralize steep falling velocity
        Vector3 vel = pm != null ? pm.Velocity : rb.linearVelocity;
        if (vel.y < 0f)
        {
            vel.y = Mathf.Max(vel.y * 0.15f, 0f);
            if (pm != null) pm.Velocity = vel;
            else rb.linearVelocity = vel;
        }
    }

    private void WallRunMovement()
    {
        if (orientation == null || rb == null) return;

        wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        // Forward direction along the wall surface
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

        // Ensure wallForward points in the direction the player is looking
        if ((orientation.forward - wallForward).sqrMagnitude > (orientation.forward - -wallForward).sqrMagnitude)
        {
            wallForward = -wallForward;
        }

        // Apply forward propulsion along the wall
        rb.AddForce(wallForward * wallRunForce * Time.deltaTime, ForceMode.Force);

        // Firm inward stick force towards the wall to maintain contact against bounce
        rb.AddForce(-wallNormal * wallStickForce, ForceMode.Force);

        // Gentle downward glide velocity (controls vertical fall smoothly)
        Vector3 vel = pm != null ? pm.Velocity : rb.linearVelocity;
        vel.y = Mathf.MoveTowards(vel.y, -wallRunGravity, Time.deltaTime * 5f);

        // Clamp maximum horizontal speed along wall
        Vector3 hVel = new Vector3(vel.x, 0f, vel.z);
        if (hVel.magnitude > maxWallRunSpeed)
        {
            hVel = hVel.normalized * maxWallRunSpeed;
            vel = new Vector3(hVel.x, vel.y, hVel.z);
        }

        if (pm != null) pm.Velocity = vel;
        else rb.linearVelocity = vel;
    }

    public void StopWallRun()
    {
        isWallRunning = false;
        wallRunTimeOnWall = 0f;

        // Restore standard gravity
        if (rb != null)
        {
            rb.useGravity = true;
        }

        if (pm != null)
        {
            pm.isWallRunning = false;
            pm.SetTargetCameraTilt(0f);
        }
    }

    private void WallJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallDuration;

        Vector3 normal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 jumpDir = (Vector3.up * wallJumpUpForce)
                        + (normal * wallJumpSideForce)
                        + (orientation != null ? orientation.forward * wallJumpForwardForce : Vector3.zero);

        // Reset vertical velocity for crisp jump feel
        Vector3 vel = pm != null ? pm.Velocity : rb.linearVelocity;
        vel.y = 0f;
        if (pm != null) pm.Velocity = vel;
        else rb.linearVelocity = vel;

        // Restore gravity before applying impulse
        if (rb != null)
        {
            rb.useGravity = true;
        }

        rb.AddForce(jumpDir, ForceMode.Impulse);
        StopWallRun();
    }

    private void HandleCameraTilt()
    {
        if (pm == null) return;

        if (isWallRunning)
        {
            if (wallRight)
            {
                pm.SetTargetCameraTilt(tiltAngle);
            }
            else if (wallLeft)
            {
                pm.SetTargetCameraTilt(-tiltAngle);
            }
        }
        else
        {
            pm.SetTargetCameraTilt(0f);
        }
    }

    public void InitAudio()
    {
        if (pm == null) pm = GetComponent<PlayerMovement>();

        // Fallback for clip if null
        if (wallRunClip == null && pm != null && pm.slideClip != null)
        {
            wallRunClip = pm.slideClip;
        }

        if (wallRunAudioSource == null)
        {
            // Search existing audio sources for one matching wallRunClip or dedicated
            AudioSource[] sources = GetComponents<AudioSource>();
            foreach (var src in sources)
            {
                if (src != null && src.clip == wallRunClip && src != pm.windAudioSource && src != pm.footstepAudioSource && src != pm.slideAudioSource)
                {
                    wallRunAudioSource = src;
                    break;
                }
            }

            if (wallRunAudioSource == null)
            {
                wallRunAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (wallRunAudioSource != null)
        {
            wallRunAudioSource.loop = true;
            wallRunAudioSource.playOnAwake = false;
            wallRunAudioSource.spatialBlend = 0f;
            if (wallRunClip != null)
            {
                wallRunAudioSource.clip = wallRunClip;
            }
        }
    }

    private void HandleAudio()
    {
        if (wallRunAudioSource == null) return;
        if (wallRunAudioSource.clip == null)
        {
            if (wallRunClip != null) wallRunAudioSource.clip = wallRunClip;
            else if (pm != null && pm.slideClip != null) wallRunAudioSource.clip = pm.slideClip;
        }
        if (wallRunAudioSource.clip == null) return;

        if (isWallRunning)
        {
            if (!wallRunAudioSource.isPlaying)
            {
                wallRunAudioSource.volume = wallRunVolume * 0.5f; // Immediately audible
                wallRunAudioSource.Play();
            }

            wallRunAudioSource.volume = Mathf.MoveTowards(wallRunAudioSource.volume, wallRunVolume, Time.deltaTime * 10f);

            // Dynamic pitch based on player speed
            Vector3 vel = pm != null ? pm.Velocity : (rb != null ? rb.linearVelocity : Vector3.zero);
            float speedRatio = Mathf.Clamp01(vel.magnitude / Mathf.Max(maxWallRunSpeed, 1f));
            wallRunAudioSource.pitch = Mathf.Lerp(1.05f, 1.25f, speedRatio);
        }
        else
        {
            if (wallRunAudioSource.isPlaying)
            {
                wallRunAudioSource.volume = Mathf.MoveTowards(wallRunAudioSource.volume, 0f, Time.deltaTime * 14f);
                if (wallRunAudioSource.volume <= 0.02f)
                {
                    wallRunAudioSource.Stop();
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform t = orientation != null ? orientation : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, t.right * wallCheckDistance);
        Gizmos.DrawRay(transform.position, -t.right * wallCheckDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector3.down * (1.0f + minJumpHeight));
    }
}
