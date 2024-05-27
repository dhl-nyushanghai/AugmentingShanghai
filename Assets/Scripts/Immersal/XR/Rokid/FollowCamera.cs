using UnityEngine;
namespace Rokid.UXR.Utility
{
    [ExecuteAlways]
    public class FollowCamera : MonoBehaviour
    {
        public enum FollowType
        {
            RotationAndPosition, //Follows the position and rotation of the camera.
            PositionOnly, // Follows only the position of the camera
            RotationOnly // Follows only the rotation of the camera
        }

        [SerializeField, Tooltip("Follow Camera Pose Type")]
        private FollowType followType = FollowType.RotationAndPosition;
        [SerializeField, Tooltip("Deviation from Camera Position")]
        public Vector3 offsetPosition = new Vector3(0, 0, 0);

        [SerializeField, Tooltip("Deviation from Camera Rotation")]
        private Quaternion offsetRotation = Quaternion.identity;
        [SerializeField, Tooltip("Lock X-axis while following camera rotation")]
        private bool lockRotX = false;
        [SerializeField, Tooltip("Lock Y-axis while following camera rotation")]
        private bool lockRotY = false;
        [SerializeField, Tooltip("Lock Z-axis while following camera rotation")]
        private bool lockRotZ = false;
        [SerializeField, Tooltip("adjust camera center by fov")]
        private bool adjustCenterByFov = true;
        private Vector3 oriOffsetPosition = Vector3.zero;

        private void Start()
        {
            oriOffsetPosition = offsetPosition;
            AdjustCenterByCameraByFov(adjustCenterByFov);
        }
        private void LateUpdate()
        {
            switch (followType)
            {
                case FollowType.RotationAndPosition:
                    this.transform.position = MainCameraCache.mainCamera.transform.TransformPoint(offsetPosition);
                    Vector3 cameraEuler = (offsetRotation * MainCameraCache.mainCamera.transform.rotation).eulerAngles;
                    this.transform.rotation = Quaternion.Euler(lockRotX ? 0 : cameraEuler.x, lockRotY ? 0 : cameraEuler.y, lockRotZ ? 0 : cameraEuler.z);
                    break;
                case FollowType.PositionOnly:
                    this.transform.position = MainCameraCache.mainCamera.transform.position + offsetPosition;
                    break;
                case FollowType.RotationOnly:
                    Vector3 cameraEuler1 = (offsetRotation * MainCameraCache.mainCamera.transform.rotation).eulerAngles;
                    this.transform.rotation = Quaternion.Euler(lockRotX ? 0 : cameraEuler1.x, lockRotY ? 0 : cameraEuler1.y, lockRotZ ? 0 : cameraEuler1.z);
                    break;
            }
        }

        public void AdjustCenterByCameraByFov(bool adjustCenterByFov, bool useLeftEyeFov = true)
        {
            this.adjustCenterByFov = adjustCenterByFov;
            if (adjustCenterByFov)
            {
                Vector3 center = Utils.GetCameraCenter(oriOffsetPosition.z);
                offsetPosition = center + new Vector3(oriOffsetPosition.x, oriOffsetPosition.y, 0);
                RKLog.KeyInfo($"====FollowCamera==== {this.gameObject.name}, offsetPosition:{offsetPosition},oriOffsetPosition:{oriOffsetPosition},center:{center}");
            }
            else
            {
                offsetPosition = oriOffsetPosition;
                RKLog.KeyInfo($"====FollowCamera==== {this.gameObject.name} offsetPosition:{offsetPosition}");
            }
        }
    }
}

