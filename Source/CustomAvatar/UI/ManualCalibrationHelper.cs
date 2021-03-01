using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    internal class ManualCalibrationHelper : MonoBehaviour
    {
        private static readonly int kColor = Shader.PropertyToID("_Color");

        private bool loaded;

        private ILogger<ManualCalibrationHelper> _logger;
        private ShaderLoader _shaderLoader;
        private VRPlayerInputInternal _playerInput;
        private PlayerAvatarManager _avatarManager;

        private Material _sphereMaterial;
        private Material _rodMaterial;

        private GameObject _waistSphere;
        private GameObject _leftFootSphere;
        private GameObject _rightFootSphere;

        private GameObject _waistRod;
        private GameObject _leftFootRod;
        private GameObject _rightFootRod;

        internal void Awake()
        {
            enabled = false;
        }

        [Inject]
        internal void Construct(ILogger<ManualCalibrationHelper> logger, ShaderLoader shaderLoader, VRPlayerInputInternal playerInput, PlayerAvatarManager avatarManager)
        {
            _logger = logger;
            _shaderLoader = shaderLoader;
            _playerInput = playerInput;
            _avatarManager = avatarManager;
        }

        internal void Start()
        {
            if (_shaderLoader.unlitShader)
            {
                _sphereMaterial = new Material(_shaderLoader.unlitShader);
                _rodMaterial = new Material(_shaderLoader.unlitShader);

                _rodMaterial.SetColor(kColor, new Color(0, 1f, 0, 1));

                _waistSphere = CreateCalibrationSphere();
                _leftFootSphere = CreateCalibrationSphere();
                _rightFootSphere = CreateCalibrationSphere();

                _waistRod = CreateRod();
                _leftFootRod = CreateRod();
                _rightFootRod = CreateRod();

                loaded = true;
            }
            else
            {
                _logger.Error("Unlit shader not loaded; manual calibration points may not be visible");
            }
        }

        internal void Update()
        {
            UpdateTrackingMarker(_waistSphere, _waistRod, _avatarManager.currentlySpawnedAvatar.pelvis, DeviceUse.Waist);
            UpdateTrackingMarker(_leftFootSphere, _leftFootRod, _avatarManager.currentlySpawnedAvatar.leftLeg, DeviceUse.LeftFoot);
            UpdateTrackingMarker(_rightFootSphere, _rightFootRod, _avatarManager.currentlySpawnedAvatar.rightLeg, DeviceUse.RightFoot);
        }

        internal void OnDisable()
        {
            if (!loaded) return;

            _waistSphere.SetActive(false);
            _leftFootSphere.SetActive(false);
            _rightFootSphere.SetActive(false);

            _waistRod.SetActive(false);
            _leftFootRod.SetActive(false);
            _rightFootRod.SetActive(false);
        }

        private GameObject CreateCalibrationSphere()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.layer = AvatarLayers.kAlwaysVisible;
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.GetComponent<Renderer>().material = _sphereMaterial;

            return sphere;
        }

        private GameObject CreateRod()
        {
            GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            rod.transform.localScale = new Vector3(0.01f, 0.5f, 0.01f);
            rod.GetComponent<Renderer>().material = _rodMaterial;

            return rod;
        }

        private void UpdateTrackingMarker(GameObject sphere, GameObject rod, Transform avatarTarget, DeviceUse deviceUse)
        {
            if (_playerInput.TryGetUncalibratedPoseForAvatar(deviceUse, _avatarManager.currentlySpawnedAvatar, out Pose pose))
            {
                sphere.SetActive(true);
                sphere.transform.position = pose.position;
                sphere.transform.rotation = pose.rotation;

                rod.SetActive(true);
                Vector3 trackerToPoint = pose.position - avatarTarget.position;
                Vector3 pivot = (pose.position + avatarTarget.position) * 0.5f;
                Vector3 localScale = rod.transform.localScale;
                rod.transform.position = pivot;
                rod.transform.rotation = Quaternion.LookRotation(trackerToPoint) * Quaternion.Euler(90, 0, 0);
                rod.transform.localScale = new Vector3(localScale.x, trackerToPoint.magnitude * 0.5f, localScale.z);
            }
            else
            {
                sphere.SetActive(false);
                rod.SetActive(false);
            }
        }
    }
}
