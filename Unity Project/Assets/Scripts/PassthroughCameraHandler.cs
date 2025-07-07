using System.Collections;
using UnityEngine;

namespace Scripts
{
    public class PassthroughCameraHandler : MonoBehaviour
    {
        public enum FrontCameraSelection
        {
            Left,
            Right,
        }

        public enum FrontCameraResolutionSelection
        {
            _320x240,
            _640x480,
            _800x600,
            _1280x960
        }
    
        [SerializeField] private FrontCameraSelection selectedFrontCamera = FrontCameraSelection.Left;
        [SerializeField] private FrontCameraResolutionSelection selectedFrontCameraResolution = FrontCameraResolutionSelection._1280x960;

        public WebCamTexture WebCamTexture { get; private set; }
        public Vector2Int CurrentCameraResolution { get; private set; }

        public IEnumerator InitializeWebCamTexture()
        {
            while (true)
            {
                var devices = WebCamTexture.devices;
                if (PassthroughCameraUtils.EnsureInitialized() && PassthroughCameraUtils.CameraEyeToCameraIdMap.TryGetValue(selectedFrontCamera, out var cameraData))
                {
                    if (cameraData.index < devices.Length)
                    {
                        var deviceName = devices[cameraData.index].name;
                        WebCamTexture  webCamTexture = new WebCamTexture(deviceName, CurrentCameraResolution.x, CurrentCameraResolution.y);
                        
                        // There is a bug in the current implementation of WebCamTexture: if 'Play()' is called at the same frame the WebCamTexture was created, this error is logged and the WebCamTexture object doesn't work:
                        //     Camera2: SecurityException java.lang.SecurityException: validateClientPermissionsLocked:1325: Callers from device user 0 are not currently allowed to connect to camera "66"
                        //     Camera2: Timeout waiting to open camera.
                        // Waiting for one frame is important and prevents the bug.
                        yield return null;
                        webCamTexture.Play();
                        var currentResolution = new Vector2Int(webCamTexture.width, webCamTexture.height);
                        
                        WebCamTexture = webCamTexture;
                        Debug.Log($"WebCamTexture created, texturePtr: {WebCamTexture.GetNativeTexturePtr()}, size: {WebCamTexture.width}/{WebCamTexture.height}");
                        yield break;
                    }
                }

                Debug.LogError($"Requested camera is not present in WebCamTexture.devices: {string.Join(", ", devices)}.");
                yield return null;
            }
        }

        public static Vector2Int GetCameraDimensions(FrontCameraResolutionSelection option)
        {
            return option switch
            {
                FrontCameraResolutionSelection._320x240 => new Vector2Int(320, 240),
                FrontCameraResolutionSelection._640x480 => new Vector2Int(640, 480),
                FrontCameraResolutionSelection._800x600 => new Vector2Int(800, 600),
                FrontCameraResolutionSelection._1280x960 => new Vector2Int(1280, 960),
                _ => Vector2Int.zero
            };
        }

        private void Awake()
        {
            CurrentCameraResolution = GetCameraDimensions(selectedFrontCameraResolution);
        }
    }
}
