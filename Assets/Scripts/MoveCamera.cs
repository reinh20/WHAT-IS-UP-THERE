using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [Header("Target to Follow")]
    [Tooltip("The Transform that the camera will follow (e.g. CameraPos child on Player)")]
    public Transform cameraPosition;

    [Header("Momentum / Speed Zoom")]
    [Tooltip("Enable dynamic FOV zoom when high momentum/speed is achieved (sliding, grappling, bhop)")]
    public bool enableMomentumZoom = true;

    [Tooltip("Camera to zoom (auto-detected if empty)")]
    public Camera targetCamera;

    [Tooltip("Player Rigidbody to measure momentum from (auto-detected if empty)")]
    public Rigidbody playerRb;

    [Tooltip("Base FOV when standing or moving at normal speeds")]
    public float normalFOV = 60f;

    [Tooltip("Target FOV when maximum momentum/speed is achieved (e.g. 85 for speed rush)")]
    public float maxFOV = 85f;

    [Tooltip("Speed/momentum threshold where camera zoom begins (should be above normal walking maxSpeed)")]
    public float momentumThreshold = 17f;

    [Tooltip("Speed/momentum where maximum FOV is reached")]
    public float maxSpeedForZoom = 32f;

    [Header("Smoothing & Anti-Jitter Tuning")]
    [Tooltip("How fast FOV zooms out when accelerating into high speed (m/s per second)")]
    public float speedAttackRate = 35f;

    [Tooltip("How gently FOV releases back to normal when slowing down (m/s per second). Slower release prevents micro-dip jitter.")]
    public float speedReleaseRate = 14f;

    [Tooltip("Smooth time for camera FOV transitions in seconds (smooth damper)")]
    public float zoomSmoothTime = 0.28f;

    [Header("Runtime Debug")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float currentFOV;

    private float _smoothedSpeed;
    private float _fovVelocity;
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponentInChildren<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        if (targetCamera != null && normalFOV <= 0f)
        {
            normalFOV = targetCamera.fieldOfView;
        }

        if (playerRb == null && cameraPosition != null)
        {
            playerRb = cameraPosition.GetComponentInParent<Rigidbody>();
        }

        if (playerRb != null)
        {
            _playerMovement = playerRb.GetComponent<PlayerMovement>();
        }

        _smoothedSpeed = 0f;
    }

    private void Start()
    {
        if (targetCamera != null && targetCamera.fieldOfView > 0f)
        {
            normalFOV = targetCamera.fieldOfView;
        }
    }

    // Camera following and FOV updates MUST happen in LateUpdate
    // to ensure physics interpolation and player rotation (Look) are fully resolved for this frame.
    private void LateUpdate()
    {
        // Follow target position
        if (cameraPosition != null)
        {
            transform.position = cameraPosition.position;
        }

        // Handle dynamic momentum zoom
        if (enableMomentumZoom)
        {
            HandleMomentumZoom();
        }
    }

    private void HandleMomentumZoom()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponentInChildren<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        if (playerRb == null && cameraPosition != null)
        {
            playerRb = cameraPosition.GetComponentInParent<Rigidbody>();
            if (playerRb != null) _playerMovement = playerRb.GetComponent<PlayerMovement>();
        }

        if (playerRb == null) return;

#if UNITY_6000_0_OR_NEWER
        Vector3 vel = playerRb.linearVelocity;
#else
        Vector3 vel = playerRb.velocity;
#endif

        // Calculate horizontal (planar) speed
        float hSpeed = new Vector2(vel.x, vel.z).magnitude;

        // Calculate effective momentum speed:
        // Include upward swings (grapple, jump pads) and slope slide speed,
        // while ignoring downward free-fall so drops do not falsely trigger zoom.
        float effectiveSpeed = hSpeed;
        if (vel.y > 0f)
        {
            // Upward momentum (grapple slingshot, walljump) contributes to speed feel
            effectiveSpeed = Mathf.Sqrt(hSpeed * hSpeed + (vel.y * 0.7f) * (vel.y * 0.7f));
        }
        else if (_playerMovement != null && _playerMovement.IsCrouching)
        {
            // Sliding down ramps
            effectiveSpeed = Mathf.Sqrt(hSpeed * hSpeed + (vel.y * 0.5f) * (vel.y * 0.5f));
        }

        // Asymmetric Attack / Release Filter:
        // When speed increases, climb at attackRate.
        // When speed momentarily dips (micro-collisions, ramp transitions), gently release at releaseRate.
        // This completely eliminates frame-to-frame FOV jitter and stutter.
        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            if (effectiveSpeed > _smoothedSpeed)
            {
                _smoothedSpeed = Mathf.MoveTowards(_smoothedSpeed, effectiveSpeed, dt * speedAttackRate);
            }
            else
            {
                _smoothedSpeed = Mathf.MoveTowards(_smoothedSpeed, effectiveSpeed, dt * speedReleaseRate);
            }
        }
        currentSpeed = _smoothedSpeed;

        // Dynamic threshold baseline
        float effectiveThreshold = momentumThreshold;
        if (_playerMovement != null)
        {
            effectiveThreshold = Mathf.Max(momentumThreshold, _playerMovement.maxSpeed + 2.0f);
        }

        // Calculate target FOV using smooth S-curve (SmoothStep)
        float targetFOV = normalFOV;
        if (_smoothedSpeed > effectiveThreshold)
        {
            float rawT = Mathf.Clamp01(Mathf.InverseLerp(effectiveThreshold, maxSpeedForZoom, _smoothedSpeed));
            float smoothT = Mathf.SmoothStep(0f, 1f, rawT);
            targetFOV = Mathf.Lerp(normalFOV, maxFOV, smoothT);
        }

        // Smoothly and cleanly interpolate camera FOV using SmoothDamp
        targetCamera.fieldOfView = Mathf.SmoothDamp(targetCamera.fieldOfView, targetFOV, ref _fovVelocity, zoomSmoothTime);
        currentFOV = targetCamera.fieldOfView;
    }
}
