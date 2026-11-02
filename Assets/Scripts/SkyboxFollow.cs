using UnityEngine;

namespace Astronomy
{
    /// <summary>
    /// Keeps the 3D Skybox centered on the active camera to ensure celestial bodies
    /// and stars appear at infinity with zero parallax error.
    /// </summary>
    [ExecuteAlways]
    public class SkyboxFollow : MonoBehaviour
    {
        [Tooltip("The camera transform to follow. If unassigned, Camera.main will be automatically used.")]
        public Transform targetCamera;

        private void Start()
        {
            EnsureTarget();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                EnsureTarget();
            }

            if (targetCamera != null)
            {
                transform.position = targetCamera.position;
            }
        }

        private void EnsureTarget()
        {
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
        }
    }
}
