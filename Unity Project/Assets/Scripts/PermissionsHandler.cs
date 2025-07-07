using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;

namespace Scripts
{
    public class PermissionsHandler : MonoBehaviour
    {
        private static bool _hasAskedOnce = false;
        private bool _permissionRequestComplete = false;
        private bool _allPermissionsGranted = false;

#if UNITY_ANDROID

        private static readonly string[] Permissions = {
            OVRPermissionsRequester.ScenePermission,
            "android.permission.CAMERA",
            "horizonos.permission.HEADSET_CAMERA"
        };

        /// <summary>
        /// Requests all required permissions if needed, then waits until the user responds.
        /// Calls onComplete(true) only if every permission was granted.
        /// </summary>
        public IEnumerator AskForPermissions(Action<bool> onComplete = null)
        {
            if (ArePermissionsGranted())
            {
                onComplete?.Invoke(true);
                yield break;
            }

            if (_hasAskedOnce)
            {
                yield return new WaitUntil(() => _permissionRequestComplete);
                onComplete?.Invoke(_allPermissionsGranted);
                yield break;
            }

            _hasAskedOnce = true;
            _permissionRequestComplete = false;

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += OnPermissionGranted;
            callbacks.PermissionDenied += OnPermissionDenied;
            callbacks.PermissionDeniedAndDontAskAgain += OnPermissionDenied;

            Permission.RequestUserPermissions(Permissions, callbacks);

            yield return new WaitUntil(() => _permissionRequestComplete);
            onComplete?.Invoke(_allPermissionsGranted);
        }

        public bool ArePermissionsGranted()
        {
            foreach (var permission in Permissions)
            {
                if (!Permission.HasUserAuthorizedPermission(permission))
                    return false;
            }
            return true;
        }

        private void OnPermissionGranted(string permission)
        {
            Debug.Log($"PermissionsHandler: {permission} granted.");
            if (ArePermissionsGranted())
                FinishPermissionRequest(true);
        }

        private void OnPermissionDenied(string permission)
        {
            Debug.LogError($"PermissionsHandler: {permission} denied. Required permissions not granted.");
            FinishPermissionRequest(false);
        }

        private void FinishPermissionRequest(bool success)
        {
            _allPermissionsGranted = success;
            _permissionRequestComplete = true;
            _hasAskedOnce = false;
        }

#endif
    }
}
