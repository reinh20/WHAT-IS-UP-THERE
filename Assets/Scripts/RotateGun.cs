using UnityEngine;

public class RotateGun : MonoBehaviour
{
    [Header("Grappling Reference")]
    public GrapplingGun grappling;

    [Header("Rotation Settings")]
    [Tooltip("Speed of slerp towards grapple point and back")]
    public float rotationSpeed = 8f;

    private Quaternion desiredRotation;

    private void Awake()
    {
        if (grappling == null)
        {
            grappling = GetComponent<GrapplingGun>();
            if (grappling == null) grappling = GetComponentInParent<GrapplingGun>();
        }
    }

    private void Update()
    {
        if (grappling == null) return;

        if (!grappling.IsGrappling())
        {
            desiredRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        }
        else
        {
            Vector3 direction = grappling.GetGrapplePoint() - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                desiredRotation = Quaternion.LookRotation(direction);
            }
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSpeed);
    }
}
