using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

[InitializeOnLoad]
internal static class InstallCinemachineForLevel2
{
    private const string PackageId = "com.unity.cinemachine";
    private const string SessionKey = "Level2.CinemachineInstallStarted";
    private static AddRequest request;

    static InstallCinemachineForLevel2()
    {
        EditorApplication.delayCall += EnsureInstalled;
    }

    private static void EnsureInstalled()
    {
        if (Application.isBatchMode)
            return;

        if (SessionState.GetBool(SessionKey, false))
            return;

        if (TypeExists("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine") ||
            TypeExists("Cinemachine.CinemachineVirtualCamera, Cinemachine"))
        {
            SessionState.SetBool(SessionKey, true);
            return;
        }

        SessionState.SetBool(SessionKey, true);
        Debug.Log("Installing com.unity.cinemachine for the Level 2 player camera...");
        request = Client.Add(PackageId);
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (request == null || !request.IsCompleted)
            return;

        EditorApplication.update -= Poll;
        if (request.Status == StatusCode.Success)
            Debug.Log($"Installed {request.Result.name}@{request.Result.version}.");
        else
            Debug.LogError("Cinemachine installation failed: " + request.Error?.message);
        request = null;
    }

    private static bool TypeExists(string qualifiedName)
    {
        return System.Type.GetType(qualifiedName) != null;
    }
}
