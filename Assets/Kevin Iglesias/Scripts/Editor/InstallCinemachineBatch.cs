using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProjectBootstrap
{
    public static class InstallCinemachineBatch
    {
        private const double TimeoutSeconds = 600d;
        private static AddRequest request;
        private static double deadline;

        public static void Install()
        {
            Debug.Log("[PackageInstaller] Adding com.unity.cinemachine");
            request = Client.Add("com.unity.cinemachine");
            deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (request == null)
                return;
            if (!request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("[PackageInstaller] Timed out installing Cinemachine.");
                    EditorApplication.Exit(2);
                }
                return;
            }

            EditorApplication.update -= Poll;
            if (request.Status == StatusCode.Success)
            {
                Debug.Log($"[PackageInstaller] Installed {request.Result.name}@{request.Result.version}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[PackageInstaller] Failed: " + request.Error?.message);
                EditorApplication.Exit(1);
            }
        }
    }
}
