﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;

namespace TantaresUM
{
    public class ModuleTantaresCamera : PartModule
    {
        [KSPField(isPersistant = true)]
        public string cameraTransformName = "cameraTransform";

        [KSPField(isPersistant = true)]
        public float cameraFieldOfView = 60;

        [KSPField(isPersistant = true)]
        public int cameraHorizontalResolution = 256;

        [KSPField(isPersistant = true)]
        public int cameraVerticalResolution = 256;

        private GameObject _cameraGameObject = null;
        private GameObject _nearGameObject = null;
        private GameObject _farGameObject = null;
        private GameObject _scaledGameObject = null;
        private GameObject _galaxyGameObject = null;

        private Camera _nearCamera = null;
        private Camera _farCamera = null;
        private Camera _scaledCamera = null;
        private Camera _galaxyCamera = null;

        const string CAMERA_PREFIX = "Tantares_";
        const string GALAXY_CAMERA_NAME = "GalaxyCamera";
        const string SCALED_CAMERA_NAME = "Camera ScaledSpace";
        const string FAR_CAMERA_NAME = "UIMainCamera";//"Camera 01";
        const string NEAR_CAMERA_NAME = "Camera 00";

        const string DEBUG_LOG_PREFIX = "ModuleTantaresCamera";

        RenderTexture _renderTextureColor;
        RenderTexture _renderTextureDepth;

        public void Start()
        {
            _cameraGameObject = base.gameObject.GetChild(cameraTransformName);

            if (_cameraGameObject == null)
            {
                Debug.LogFormat("[{0}] Camera game object is missing.", DEBUG_LOG_PREFIX);
                return;
            }

            // Create the render texture.

            _renderTextureColor = new RenderTexture(cameraHorizontalResolution, cameraVerticalResolution, 0);
            _renderTextureDepth = new RenderTexture(cameraHorizontalResolution, cameraVerticalResolution, 24);
            _renderTextureColor.Create();
            _renderTextureDepth.Create();

            // Setup all the cameras.

            _nearGameObject = new GameObject();
            _farGameObject = new GameObject();
            _scaledGameObject = new GameObject();
            _galaxyGameObject = new GameObject();

            // Add the near camera.

            _nearCamera = _nearGameObject.AddComponent<Camera>();
            var nearCameraReference = UnityEngine.Camera.allCameras.FirstOrDefault(cam => cam.name == NEAR_CAMERA_NAME);
            if (nearCameraReference != null)
            {
                _nearCamera.CopyFrom(nearCameraReference);
                _nearCamera.name = CAMERA_PREFIX + NEAR_CAMERA_NAME;
                _nearCamera.enabled = false;

                // The camera is attached to our object transform and does not move from there.

                _nearCamera.transform.parent = _cameraGameObject.transform;
                _nearCamera.transform.localPosition = Vector3.zero;
                _nearCamera.transform.localRotation = Quaternion.identity;
            }

            // Add the far camera.

            _farCamera = _farGameObject.AddComponent<Camera>();
            var farCameraReference = UnityEngine.Camera.allCameras.FirstOrDefault(cam => cam.name == FAR_CAMERA_NAME);
            if (farCameraReference != null)
            {
                _farCamera.CopyFrom(farCameraReference);
                _farCamera.name = CAMERA_PREFIX + FAR_CAMERA_NAME;
                _farCamera.enabled = false;

                // The camera is attached to our object transform and does not move from there.

                _farCamera.transform.parent = _cameraGameObject.transform;
                _farCamera.transform.localPosition = Vector3.zero;
                _farCamera.transform.localRotation = Quaternion.identity;
            }

            // Add the scaled camera.

            _scaledCamera = _scaledGameObject.AddComponent<Camera>();
            var scaledCameraReference = UnityEngine.Camera.allCameras.FirstOrDefault(cam => cam.name == SCALED_CAMERA_NAME);
            if (scaledCameraReference != null)
            {
                _scaledCamera.CopyFrom(scaledCameraReference);
                _scaledCamera.name = CAMERA_PREFIX + SCALED_CAMERA_NAME;
                _scaledCamera.enabled = false;

                // Scaled cam has no parent.
            }

            // Add the galaxy camera.

            _galaxyCamera = _galaxyGameObject.AddComponent<Camera>();
            var galaxyCameraReference = UnityEngine.Camera.allCameras.FirstOrDefault(cam => cam.name == GALAXY_CAMERA_NAME);
            if (galaxyCameraReference != null)
            {
                _galaxyCamera.CopyFrom(galaxyCameraReference);
                _galaxyCamera.name = CAMERA_PREFIX + GALAXY_CAMERA_NAME;
                _galaxyCamera.enabled = false;

                // Galaxy camera renders the galaxy skybox and is not 
                // actually moving, but only rotating to look at the galaxy cube.

                Transform galaxyRoot = GalaxyCubeControl.Instance.transform.parent;
                _galaxyCamera.transform.parent = galaxyRoot;
                _galaxyCamera.transform.localPosition = Vector3.zero;
                _galaxyCamera.transform.localRotation = Quaternion.identity;
            }
        }

        public void Update()
        {
            _scaledCamera.transform.position = ScaledSpace.LocalToScaledSpace(_cameraGameObject.transform.position);
            _scaledCamera.transform.rotation = _cameraGameObject.transform.rotation;

            _galaxyGameObject.transform.rotation = _cameraGameObject.transform.rotation;
        }

        public void LateUpdate()
        {
            _nearCamera.enabled = false;
            _farCamera.enabled = false;
            _scaledCamera.enabled = false;
            _galaxyCamera.enabled = false;
        }

        [KSPAction(guiName = "Capture Image", activeEditor = true)]
        public void ActionCaptureImage(KSPActionParam param)
        {
            CaptureImage();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Capture Image", active = true)]
        public void EventCaptureImage()
        {
            CaptureImage();
        }

        public void CaptureImage()
        {
            try
            {
                // Switch the camera on.

                Debug.LogFormat("[{0}] Switching camera on.", DEBUG_LOG_PREFIX);

                _nearCamera.enabled = true;
                if (GameSettings.GraphicsVersion != GameSettings.GraphicsType.D3D11)
                    _farCamera.enabled = true;
                _scaledCamera.enabled = true;
                _galaxyCamera.enabled = true;

                // Render camera to texture.

                Debug.LogFormat("[{0}] Rendering cameras to texture.", DEBUG_LOG_PREFIX);

                var imageTexture = new Texture2D(cameraHorizontalResolution, cameraVerticalResolution, TextureFormat.RGB24, false);

                RenderTexture.active = _renderTextureColor;

                _nearCamera.SetTargetBuffers(_renderTextureColor.colorBuffer, _renderTextureDepth.depthBuffer);
                _farCamera.SetTargetBuffers(_renderTextureColor.colorBuffer, _renderTextureDepth.depthBuffer);
                _scaledCamera.SetTargetBuffers(_renderTextureColor.colorBuffer, _renderTextureDepth.depthBuffer);
                _galaxyCamera.SetTargetBuffers(_renderTextureColor.colorBuffer, _renderTextureDepth.depthBuffer);

                _galaxyCamera.Render();
                _scaledCamera.Render();
                if (GameSettings.GraphicsVersion != GameSettings.GraphicsType.D3D11)
                    _farCamera.Render();
                _nearCamera.Render();

                imageTexture.ReadPixels(new Rect(0, 0, cameraHorizontalResolution, cameraVerticalResolution), 0, 0);
                imageTexture.Apply();

                Debug.LogFormat("[{0}] Encoding render texture to bytes.", DEBUG_LOG_PREFIX);

                byte[] bytes = imageTexture.EncodeToPNG();
                Destroy(imageTexture);

                // Disable the render texture.

                Debug.LogFormat("[{0}] Cleaning up render textures.", DEBUG_LOG_PREFIX);

                RenderTexture.active = null;
                _nearCamera.targetTexture = null;
                _farCamera.targetTexture = null;
                _scaledCamera.targetTexture = null;
                _galaxyCamera.targetTexture = null;

                // Switch the camera off.

                Debug.LogFormat("[{0}] Switching camera off.", DEBUG_LOG_PREFIX);

                _nearCamera.enabled = false;
                _farCamera.enabled = false;
                _scaledCamera.enabled = false;
                _galaxyCamera.enabled = false;

                // Write the file.

                string fileName = string.Format(@"C:\output\image_{0}.png", Guid.NewGuid().ToString());

                File.WriteAllBytes(fileName, bytes);
            }
            catch (Exception ex)
            {
                Debug.LogFormat("[{0}] Error capturing image.", DEBUG_LOG_PREFIX);
                Debug.LogFormat("[{0}] {1}.", DEBUG_LOG_PREFIX, ex.Message);
            }
        }
    }
}