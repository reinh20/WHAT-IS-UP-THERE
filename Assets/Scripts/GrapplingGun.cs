using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GrapplingGun : MonoBehaviour
{
    [Header("Components & Transforms")]
    [Tooltip("Origin point of the grappling rope (tip of the wand)")]
    public Transform gunTip;
    [Tooltip("Main player camera used for raycasting")]
    public Transform playerCamera;
    [Tooltip("The Player transform (Rigidbody to attach SpringJoint to)")]
    public Transform player;

    [Header("Grapple Settings")]
    [Tooltip("Layers that can be grappled onto")]
    public LayerMask whatIsGrappleable = ~0;
    [Tooltip("Maximum grappling hook reach distance")]
    public float maxDistance = 100f;
    [Tooltip("Aim assist radius (SphereCast) to make hitting edges and platforms much easier")]
    public float aimAssistRadius = 0.8f;

    [Header("Spring Joint Physics (Feel / Tension)")]
    [Tooltip("Spring strength pull towards grapple point")]
    public float spring = 4.5f;
    [Tooltip("Damping to reduce oscillation")]
    public float damper = 7f;
    [Tooltip("Mass scale for physics calculation")]
    public float massScale = 4.5f;
    [Tooltip("Ratio of initial distance to keep as max distance")]
    public float maxDistanceRatio = 0.8f;
    [Tooltip("Ratio of initial distance to keep as min distance")]
    public float minDistanceRatio = 0.25f;

    [Header("Visuals (Rope)")]
    [Tooltip("Speed of rope shooting out animation")]
    public float ropeSpeed = 12f;

    private LineRenderer lr;
    private Vector3 grapplePoint;
    private SpringJoint joint;
    private Vector3 currentGrapplePosition;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (gunTip == null)
        {
            Transform tip = transform.Find("Gun Tip");
            if (tip != null) gunTip = tip;
            else gunTip = transform;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f || PauseManager.IsPaused) return;
        MyInput();
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void MyInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartGrapple();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                StopGrapple();
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightTrigger.wasPressedThisFrame)
            {
                StartGrapple();
            }
            else if (Gamepad.current.rightTrigger.wasReleasedThisFrame)
            {
                StopGrapple();
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartGrapple();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                StopGrapple();
            }
        }
        catch { }
#endif
    }

    /// <summary>
    /// Checks if a valid grapple target is currently in line of sight (using Raycast and Aim-Assist SphereCast).
    /// </summary>
    public bool TryGetGrappleTarget(out RaycastHit hit)
    {
        hit = default;
        if (playerCamera == null || player == null) return false;

        // 1. Direct precise raycast first
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, maxDistance, whatIsGrappleable))
        {
            if (hit.transform != player && !hit.transform.IsChildOf(player))
            {
                return true;
            }
        }

        // 2. Forgiving spherecast aim-assist for fast-paced movement near edges/corners
        if (aimAssistRadius > 0f && Physics.SphereCast(playerCamera.position, aimAssistRadius, playerCamera.forward, out hit, maxDistance, whatIsGrappleable))
        {
            if (hit.transform != player && !hit.transform.IsChildOf(player))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns true if a grapple target is currently in range and ready to latch onto.
    /// </summary>
    public bool CanGrapple()
    {
        return TryGetGrappleTarget(out _);
    }

    public void StartGrapple()
    {
        if (playerCamera == null || player == null) return;

        if (TryGetGrappleTarget(out RaycastHit hit))
        {
            grapplePoint = hit.point;

            // Remove any existing joint before adding a new one
            if (joint != null) Destroy(joint);

            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);

            joint.maxDistance = distanceFromPoint * maxDistanceRatio;
            joint.minDistance = distanceFromPoint * minDistanceRatio;

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            if (lr != null)
            {
                lr.positionCount = 2;
            }
            currentGrapplePosition = gunTip != null ? gunTip.position : transform.position;
        }
    }

    public void StopGrapple()
    {
        if (lr != null)
        {
            lr.positionCount = 0;
        }

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }
    }

    private void DrawRope()
    {
        if (!joint || lr == null || lr.positionCount < 2) return;

        Vector3 origin = gunTip != null ? gunTip.position : transform.position;
        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, grapplePoint, Time.deltaTime * ropeSpeed);

        lr.SetPosition(0, origin);
        lr.SetPosition(1, currentGrapplePosition);
    }

    public bool IsGrappling()
    {
        return joint != null;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }

    private void OnDisable()
    {
        StopGrapple();
    }
}
