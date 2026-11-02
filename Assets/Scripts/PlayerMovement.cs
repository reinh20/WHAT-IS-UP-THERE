// RigidBody FPS Movement by Dani (adapted for Unity 6 & Universal Input System)
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerMovement : MonoBehaviour
{
    [Header("Assignables")]
    public Transform playerCam;
    public Transform orientation;

    [Header("Rotation and Look")]
    [SerializeField] private float sensitivity = 50f;
    [SerializeField] private float sensMultiplier = 1f;

    [Header("Movement")]
    public float moveSpeed = 4500f;
    public float maxSpeed = 20f;
    public bool grounded;
    public LayerMask whatIsGround = ~0;

    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    [Header("Crouch & Slide")]
    public Vector3 crouchScale = new Vector3(1f, 0.5f, 1f);
    private Vector3 playerScale;
    public float slideForce = 400f;
    public float slideCounterMovement = 0.2f;

    [Header("Jumping")]
    private bool readyToJump = true;
    public float jumpCooldown = 0.25f;
    public float jumpForce = 550f;

    [Header("Speed / Wind Sound")]
    [Tooltip("Enable wind sound fade-in when moving at high speeds")]
    public bool enableSpeedSound = true;
    [Tooltip("AudioSource that plays the wind sound (added automatically if empty)")]
    public AudioSource windAudioSource;
    [Tooltip("Looping wind audio clip")]
    public AudioClip windClip;
    [Tooltip("Speed/momentum threshold where wind sound begins to fade in")]
    public float soundThreshold = 18f;
    [Tooltip("Speed/momentum where wind sound reaches maximum volume")]
    public float maxSpeedForSound = 30f;
    [Range(0f, 1f)]
    [Tooltip("Maximum volume for wind sound")]
    public float maxVolume = 0.8f;
    [Tooltip("Speed of fade in and fade out (lower = slower/more gradual, higher = faster)")]
    public float soundFadeSpeed = 3f;
    [Tooltip("Whether to scale pitch up slightly as speed increases")]
    public bool scalePitchWithSpeed = true;
    public float minPitch = 0.9f;
    public float maxPitch = 1.3f;

    [Header("Footsteps (Per-Step Audio)")]
    [Tooltip("Enable footstep sounds per step while walking/running")]
    public bool enableFootsteps = true;
    [Tooltip("Dedicated AudioSource for footstep sound effects (auto-created if empty)")]
    public AudioSource footstepAudioSource;
    [Tooltip("Individual footstep clips (sliced per step)")]
    public AudioClip[] footstepClips;
    [Tooltip("Distance traveled on ground between each footstep")]
    public float stepDistance = 2.4f;
    [Tooltip("Distance between steps when moving at high speeds/sprinting")]
    public float sprintStepDistance = 3.0f;
    [Range(0f, 1f)]
    [Tooltip("Footstep sound volume")]
    public float footstepVolume = 0.75f;
    public float footstepMinPitch = 0.92f;
    public float footstepMaxPitch = 1.08f;
    [Tooltip("Play footstep landing sound when hitting ground from a fall")]
    public bool playLandingSound = true;

    [Header("Sliding Sound")]
    [Tooltip("Enable sliding sound while sliding on ground")]
    public bool enableSlideSound = true;
    [Tooltip("Dedicated AudioSource for sliding sound (auto-created if empty)")]
    public AudioSource slideAudioSource;
    [Tooltip("AudioClip for sliding sound (e.g. freesound_community-sliding-sound-103631)")]
    public AudioClip slideClip;
    [Tooltip("Minimum speed required to hear the slide sound")]
    public float minSlideSpeed = 1.0f;
    [Range(0f, 1f)]
    [Tooltip("Maximum volume for sliding sound")]
    public float maxSlideVolume = 0.85f;
    [Tooltip("Fade speed for slide sound entering and exiting")]
    public float slideFadeSpeed = 6f;
    [Tooltip("Scale pitch slightly based on sliding speed")]
    public bool scaleSlidePitch = true;
    public float minSlidePitch = 0.9f;
    public float maxSlidePitch = 1.15f;

    // Footstep tracking
    private float stepCycle;
    private int lastFootstepIndex = -1;
    private bool wasGrounded;

    // Internal Physics / Input
    private Rigidbody rb;
    private float xRotation;
    private float desiredX;
    private float x, y;
    private bool jumping, crouching;
    private bool isCrouching;

    // Sliding / Collision normals
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;
    private bool cancellingGrounded;

    // Public inputs & states for external systems (e.g. WallRun)
    public float HorizontalInput => x;
    public float VerticalInput => y;
    public bool JumpInput => jumping;
    public bool JumpDownInput => jumpPressedThisFrame;
    public bool jumpPressedThisFrame { get; private set; }
    public bool isWallRunning { get; set; }
    public bool IsCrouching => isCrouching;

    [Header("Camera Tilt (Wallrun / Leaning)")]
    public float targetCameraTilt = 0f;
    public float currentCameraTilt = 0f;
    public float cameraTiltSpeed = 10f;

    public void SetTargetCameraTilt(float targetTilt)
    {
        targetCameraTilt = targetTilt;
    }

    /// <summary>
    /// Resets player physics velocity and positions them at a new location/rotation.
    /// Silences all audio and resets movement state to ensure a completely stationary respawn.
    /// Used by Checkpoint and Respawn systems.
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        // Silence all audio immediately
        StopAllAudio();

        // Reset movement inputs and states so player respawns stationary
        x = 0f;
        y = 0f;
        jumping = false;
        crouching = false;
        readyToJump = true;
        isWallRunning = false;

        if (isCrouching)
        {
            isCrouching = false;
            transform.localScale = playerScale;
        }

        // Cancel grappling hook if active
        GrapplingGun gg = GetComponentInChildren<GrapplingGun>();
        if (gg != null)
        {
            gg.StopGrapple();
        }
        SpringJoint[] joints = GetComponents<SpringJoint>();
        foreach (SpringJoint j in joints)
        {
            if (j != null) Destroy(j);
        }

        // Cancel wall running if active
        WallRun wr = GetComponent<WallRun>();
        if (wr != null)
        {
            wr.StopWallRun();
        }

        // Extract ONLY horizontal yaw. The Player body/capsule MUST ALWAYS STAY 100% UPRIGHT.
        float targetYaw = rotation.eulerAngles.y;

        // Player GameObject always stays upright (0, 0, 0) so the capsule collider stands vertically
        transform.position = position;
        transform.rotation = Quaternion.identity;

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = Quaternion.identity;
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.Sleep();
        }

        // Reset camera look and orientation to face target yaw horizontally with zero tilt and zero pitch
        desiredX = targetYaw;
        xRotation = 0f;
        currentCameraTilt = 0f;
        targetCameraTilt = 0f;

        if (playerCam != null)
        {
            playerCam.localPosition = new Vector3(0f, 0.75f, 0f);
            playerCam.localRotation = Quaternion.Euler(0f, desiredX, 0f);

            Camera cam = playerCam.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.transform.localPosition = new Vector3(0f, 0.06f, 0.39f);
                cam.transform.localRotation = Quaternion.identity;
            }

            MoveCamera mc = playerCam.GetComponent<MoveCamera>();
            if (mc != null && mc.cameraPosition != null)
            {
                playerCam.position = mc.cameraPosition.position;
            }
        }

        if (orientation != null)
        {
            orientation.localRotation = Quaternion.Euler(0f, desiredX, 0f);
        }

        Physics.SyncTransforms();
    }

    /// <summary>
    /// Silences all AudioSources on the player GameObject and its children.
    /// </summary>
    public void StopAllAudio()
    {
        if (windAudioSource != null)
        {
            windAudioSource.volume = 0f;
            windAudioSource.Stop();
        }
        if (slideAudioSource != null)
        {
            slideAudioSource.volume = 0f;
            slideAudioSource.Stop();
        }
        if (footstepAudioSource != null)
        {
            footstepAudioSource.Stop();
        }

        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource src in sources)
        {
            if (src != null)
            {
                src.Stop();
            }
        }
    }

    // Helper property to support both Unity 6 (linearVelocity) and older versions (velocity)
    public Vector3 Velocity
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
        set
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = value;
#else
            rb.velocity = value;
#endif
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        InitSpeedSound();
        InitFootstepSound();
        InitSlideSound();

        // Apply saved master volume from settings
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 0.75f));
        }
    }

    private void Reset()
    {
        whatIsGround = ~0;
    }

    private void Start()
    {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (whatIsGround.value == 0)
        {
            whatIsGround = ~0;
        }

        // Initialize look angles smoothly from initial camera/orientation setup
        if (playerCam != null)
        {
            Vector3 euler = playerCam.localEulerAngles;
            desiredX = euler.y;
            xRotation = euler.x;
            if (xRotation > 180f) xRotation -= 360f;
        }
        else if (orientation != null)
        {
            desiredX = orientation.localEulerAngles.y;
        }
        else
        {
            desiredX = transform.localEulerAngles.y;
        }

        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            sensMultiplier = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        }
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        MyInput();
        Look();
        HandleSpeedSound();
        HandleFootsteps();
        HandleSlideSound();

        // Convenience: allow releasing cursor with Escape key
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !PauseManager.IsPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked && !PauseManager.IsPaused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif
    }

    /// <summary>
    /// Find user input supporting both New Input System and Legacy Input Manager.
    /// </summary>
    private void MyInput()
    {
        x = 0;
        y = 0;
        jumping = false;
        jumpPressedThisFrame = false;
        crouching = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;

            jumping = Keyboard.current.spaceKey.isPressed;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressedThisFrame = true;
            crouching = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.1f) x += stick.x;
            if (Mathf.Abs(stick.y) > 0.1f) y += stick.y;

            if (Gamepad.current.buttonSouth.isPressed) jumping = true;
            if (Gamepad.current.buttonSouth.wasPressedThisFrame) jumpPressedThisFrame = true;
            if (Gamepad.current.buttonEast.isPressed || Gamepad.current.rightStickButton.isPressed) crouching = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (x == 0 && y == 0 && !jumping && !crouching)
            {
                x = Input.GetAxisRaw("Horizontal");
                y = Input.GetAxisRaw("Vertical");
                jumping = Input.GetButton("Jump");
                if (Input.GetButtonDown("Jump")) jumpPressedThisFrame = true;
                crouching = Input.GetKey(KeyCode.LeftControl);
            }
        }
        catch { }
#endif

        // Crouching state transition
        if (crouching && !isCrouching)
        {
            StartCrouch();
        }
        else if (!crouching && isCrouching)
        {
            StopCrouch();
        }
    }

    private void StartCrouch()
    {
        isCrouching = true;
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);

        if (Velocity.magnitude > 0.5f)
        {
            if (grounded && orientation != null)
            {
                rb.AddForce(orientation.forward * slideForce);
                TriggerSlideStart();
            }
        }
    }

    private void StopCrouch()
    {
        isCrouching = false;
        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    private void Movement()
    {
        if (rb == null) return;
        if (isWallRunning) return; // WallRun handles its own physics and forces

        // Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10f);

        // Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        // Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        // If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();

        // Set max speed
        float currentMaxSpeed = this.maxSpeed;

        // If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000f);
            return;
        }

        // If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > currentMaxSpeed) x = 0;
        if (x < 0 && xMag < -currentMaxSpeed) x = 0;
        if (y > 0 && yMag > currentMaxSpeed) y = 0;
        if (y < 0 && yMag < -currentMaxSpeed) y = 0;

        // Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        // Apply forces to move player
        if (orientation != null)
        {
            rb.AddForce(orientation.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
            rb.AddForce(orientation.right * x * moveSpeed * Time.deltaTime * multiplier);
        }
    }

    private void Jump()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;

            // Add jump forces
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            // If jumping while falling, reset y velocity.
            Vector3 vel = Velocity;
            if (vel.y < 0.5f)
                Velocity = new Vector3(vel.x, 0, vel.z);
            else if (vel.y > 0)
                Velocity = new Vector3(vel.x, vel.y / 2f, vel.z);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Look()
    {
        // Don't process mouse look if game is paused or cursor is not locked
        if (PauseManager.IsPaused || Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = 0f;
        float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            // WebGL & Browser pointer-lock protection:
            // 1. Clamp delta magnitude to prevent extreme snapping from browser pointer-wrap or frame hiccups
            delta = Vector2.ClampMagnitude(delta, 50f);

            // 2. Browser Pointer Lock reports raw window/CSS pixels which are 2x higher on high-DPI screens
            float webglSensFactor = (Application.platform == RuntimePlatform.WebGLPlayer) ? 0.45f : 1.0f;

            mouseX += delta.x * (sensitivity * 0.002f) * sensMultiplier * webglSensFactor;
            mouseY += delta.y * (sensitivity * 0.002f) * sensMultiplier * webglSensFactor;
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            mouseX += stick.x * sensitivity * Time.deltaTime * 3f * sensMultiplier;
            mouseY += stick.y * sensitivity * Time.deltaTime * 3f * sensMultiplier;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (mouseX == 0f && mouseY == 0f)
            {
                float axisX = Mathf.Clamp(Input.GetAxis("Mouse X"), -10f, 10f);
                float axisY = Mathf.Clamp(Input.GetAxis("Mouse Y"), -10f, 10f);
                mouseX = axisX * sensitivity * Time.fixedDeltaTime * sensMultiplier;
                mouseY = axisY * sensitivity * Time.fixedDeltaTime * sensMultiplier;
            }
        }
        catch { }
#endif

        // CRITICAL BUG FIX FOR 180-DEGREE FLIP:
        // NEVER read rot.y from playerCam.transform.localRotation.eulerAngles every frame!
        // In Unity, when pitch (xRotation) or camera tilt (roll) is non-zero, Quaternion.eulerAngles.y
        // flips by 180 degrees due to Euler angle branch ambiguity, causing the camera to violently snap backwards!
        // By continuously accumulating desiredX += mouseX, yaw is 100% continuous and never flips.
        desiredX += mouseX;

        // Rotate pitch, clamped so player cannot flip upside down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCam != null)
        {
            // Perform the rotations with smooth camera tilt
            currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetCameraTilt, Time.deltaTime * cameraTiltSpeed);
            playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, currentCameraTilt);
        }

        if (orientation != null)
        {
            orientation.transform.localRotation = Quaternion.Euler(0f, desiredX, 0f);
        }
    }

    private void CounterMovement(float xInput, float yInput, Vector2 mag)
    {
        if (!grounded || jumping || rb == null) return;

        // Slow down sliding
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -Velocity.normalized * slideCounterMovement);
            return;
        }

        // Counter movement
        if ((Mathf.Abs(mag.x) > threshold && Mathf.Abs(xInput) < 0.05f) || (mag.x < -threshold && xInput > 0) || (mag.x > threshold && xInput < 0))
        {
            if (orientation != null)
                rb.AddForce(moveSpeed * orientation.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if ((Mathf.Abs(mag.y) > threshold && Mathf.Abs(yInput) < 0.05f) || (mag.y < -threshold && yInput > 0) || (mag.y > threshold && yInput < 0))
        {
            if (orientation != null)
                rb.AddForce(moveSpeed * orientation.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        // Limit diagonal running
        if (Mathf.Sqrt(Mathf.Pow(Velocity.x, 2) + Mathf.Pow(Velocity.z, 2)) > maxSpeed)
        {
            float fallspeed = Velocity.y;
            Vector3 n = Velocity.normalized * maxSpeed;
            Velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vector calculations regarding movement and limiting movement
    /// </summary>
    public Vector2 FindVelRelativeToLook()
    {
        if (orientation == null || rb == null) return Vector2.zero;

        float lookAngle = orientation.eulerAngles.y;
        float moveAngle = Mathf.Atan2(Velocity.x, Velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90f - u;

        float magnitude = Velocity.magnitude;
        float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        // Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            // FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        // Invoke ground cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void StopGrounded()
    {
        grounded = false;
    }

    private void InitSpeedSound()
    {
        if (!enableSpeedSound) return;

        if (windAudioSource == null)
        {
            windAudioSource = GetComponent<AudioSource>();
            if (windAudioSource == null)
            {
                windAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (windAudioSource != null)
        {
            windAudioSource.loop = true;
            windAudioSource.playOnAwake = false;
            windAudioSource.spatialBlend = 0f; // 2D stereo for head wind presence
            if (windClip != null && windAudioSource.clip == null)
            {
                windAudioSource.clip = windClip;
            }
            windAudioSource.volume = 0f;
            if (windAudioSource.clip != null && !windAudioSource.isPlaying)
            {
                windAudioSource.Play();
            }
        }
    }

    private void HandleSpeedSound()
    {
        if (!enableSpeedSound)
        {
            if (windAudioSource != null && windAudioSource.volume > 0f)
            {
                windAudioSource.volume = Mathf.MoveTowards(windAudioSource.volume, 0f, Time.deltaTime * soundFadeSpeed);
            }
            return;
        }

        if (windAudioSource == null)
        {
            InitSpeedSound();
            if (windAudioSource == null) return;
        }

        if (windAudioSource.clip == null && windClip != null)
        {
            windAudioSource.clip = windClip;
            windAudioSource.volume = 0f;
            windAudioSource.Play();
        }

        if (windAudioSource.clip == null) return;

        if (!windAudioSource.isPlaying)
        {
            windAudioSource.Play();
        }

        float speed = new Vector2(Velocity.x, Velocity.z).magnitude;
        float targetVolume = 0f;
        float targetPitch = 1f;

        float effectiveThreshold = Mathf.Max(soundThreshold, maxSpeed + 2.5f);

        if (speed > effectiveThreshold)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(effectiveThreshold, maxSpeedForSound, speed));
            targetVolume = Mathf.Lerp(0f, maxVolume, t);
            if (scalePitchWithSpeed)
            {
                targetPitch = Mathf.Lerp(minPitch, maxPitch, t);
            }
        }

        // Smooth fade-in when fast, smooth fade-out when slow
        windAudioSource.volume = Mathf.Lerp(windAudioSource.volume, targetVolume, Time.deltaTime * soundFadeSpeed);
        if (scalePitchWithSpeed)
        {
            windAudioSource.pitch = Mathf.Lerp(windAudioSource.pitch, targetPitch, Time.deltaTime * soundFadeSpeed);
        }
    }

    private void InitFootstepSound()
    {
        if (!enableFootsteps) return;

        if (footstepAudioSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            foreach (var src in sources)
            {
                if (src != windAudioSource)
                {
                    footstepAudioSource = src;
                    break;
                }
            }

            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.loop = false;
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.spatialBlend = 0f; // 2D stereo for direct player presence
        }
    }

    private void HandleFootsteps()
    {
        if (!enableFootsteps) return;
        if (footstepClips == null || footstepClips.Length == 0) return;

        // Check landing sound when transitioning from air to ground
        if (playLandingSound && !wasGrounded && grounded && Velocity.y < -1.5f)
        {
            PlayFootstep(footstepVolume * 1.15f);
            stepCycle = 0f;
        }
        wasGrounded = grounded;

        // Must be on the ground to take footsteps
        if (!grounded) return;

        // If crouching/sliding, footsteps don't play
        if (isCrouching) return;

        // Horizontal velocity only
        Vector3 horizVel = new Vector3(Velocity.x, 0f, Velocity.z);
        float horizSpeed = horizVel.magnitude;

        // Stationary or very slow -> no steps
        if (horizSpeed < 0.4f) return;

        // Step distance depends on walking vs running
        float currentStepDist = (horizSpeed > maxSpeed * 0.6f) ? sprintStepDistance : stepDistance;
        stepCycle += horizSpeed * Time.deltaTime;

        if (stepCycle >= currentStepDist)
        {
            stepCycle = 0f;
            PlayFootstep(footstepVolume);
        }
    }

    public void PlayFootstep(float volume)
    {
        if (footstepAudioSource == null) InitFootstepSound();
        if (footstepAudioSource == null || footstepClips == null || footstepClips.Length == 0) return;

        // Pick random index avoiding immediate repeat
        int index = UnityEngine.Random.Range(0, footstepClips.Length);
        if (footstepClips.Length > 1 && index == lastFootstepIndex)
        {
            index = (index + 1) % footstepClips.Length;
        }
        lastFootstepIndex = index;

        AudioClip clip = footstepClips[index];
        if (clip != null)
        {
            footstepAudioSource.pitch = UnityEngine.Random.Range(footstepMinPitch, footstepMaxPitch);
            footstepAudioSource.PlayOneShot(clip, volume);
        }
    }

    private void InitSlideSound()
    {
        if (!enableSlideSound) return;

        if (slideAudioSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            foreach (var s in sources)
            {
                if (s != windAudioSource && s != footstepAudioSource)
                {
                    slideAudioSource = s;
                    break;
                }
            }

            if (slideAudioSource == null)
            {
                slideAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (slideAudioSource != null)
        {
            slideAudioSource.loop = true;
            slideAudioSource.playOnAwake = false;
            slideAudioSource.spatialBlend = 0f;
            if (slideClip != null && slideAudioSource.clip == null)
            {
                slideAudioSource.clip = slideClip;
            }
            slideAudioSource.volume = 0f;
        }
    }

    private void TriggerSlideStart()
    {
        if (!enableSlideSound || slideAudioSource == null) return;
        if (slideClip == null && slideAudioSource.clip == null) return;

        if (slideAudioSource.clip == null) slideAudioSource.clip = slideClip;

        slideAudioSource.time = 0f;
        slideAudioSource.volume = maxSlideVolume * 0.5f;
        if (!slideAudioSource.isPlaying)
        {
            slideAudioSource.Play();
        }
    }

    private void HandleSlideSound()
    {
        if (!enableSlideSound || slideAudioSource == null) return;
        if (slideClip == null && slideAudioSource.clip == null) return;

        if (slideAudioSource.clip == null) slideAudioSource.clip = slideClip;

        // Active slide condition: crouching + grounded + moving above minimum slide speed
        bool isSliding = isCrouching && grounded && (Velocity.magnitude > minSlideSpeed);

        float targetVolume = 0f;
        float targetPitch = 1f;

        if (isSliding)
        {
            if (!slideAudioSource.isPlaying)
            {
                slideAudioSource.Play();
            }

            float t = Mathf.Clamp01(Velocity.magnitude / maxSpeed);
            targetVolume = Mathf.Lerp(maxSlideVolume * 0.35f, maxSlideVolume, t);

            if (scaleSlidePitch)
            {
                targetPitch = Mathf.Lerp(minSlidePitch, maxSlidePitch, t);
            }
        }

        // Smooth volume transition
        slideAudioSource.volume = Mathf.MoveTowards(slideAudioSource.volume, targetVolume, Time.deltaTime * slideFadeSpeed);
        if (scaleSlidePitch)
        {
            slideAudioSource.pitch = Mathf.Lerp(slideAudioSource.pitch, targetPitch, Time.deltaTime * slideFadeSpeed);
        }

        if (slideAudioSource.volume <= 0.001f && !isSliding && slideAudioSource.isPlaying)
        {
            slideAudioSource.Stop();
        }
    }
}
