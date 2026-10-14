// Binds the existing Cinemachine player camera to the hero and ensures mouse orbit is attached.
// Study guide and script map: CODE_GUIDE.md in the project root.

using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CinemachineCamera))]
public sealed class CinemachinePlayerFollow : MonoBehaviour
{
    [SerializeField] private Transform player;

    private CinemachineCamera cinemachineCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Unity runs this after a scene loads. Find the player/main camera and repair their Cinemachine binding if possible.
    // Add a missing Brain or follow binder; this does not create a new Cinemachine camera when none exists.
    private static void EnsurePlayerCameraAtRuntime()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            playerObject = GameObject.Find("TribalPlayer");
        if (playerObject == null)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;
        if (mainCamera.GetComponent<CinemachineBrain>() == null)
            mainCamera.gameObject.AddComponent<CinemachineBrain>();

        CinemachinePlayerFollow existingBinder = FindFirstObjectByType<CinemachinePlayerFollow>();
        if (existingBinder != null)
        {
            existingBinder.Configure(playerObject.transform);
            return;
        }

        GameObject cameraObject = GameObject.Find("CinemachinePlayerCamera");
        CinemachineCamera existingCamera = cameraObject != null
            ? cameraObject.GetComponent<CinemachineCamera>()
            : null;
        if (existingCamera == null)
            return;

        CinemachinePlayerFollow binder = existingCamera.GetComponent<CinemachinePlayerFollow>();
        if (binder == null)
            binder = existingCamera.gameObject.AddComponent<CinemachinePlayerFollow>();
        binder.Configure(playerObject.transform);
    }

    // Store the requested player transform and immediately assign the camera's Follow and LookAt targets.
    public void Configure(Transform target)
    {
        player = target;
        ApplyTarget();
    }

    // Cache camera components, find the player if needed, and bind the target when this component loads.
    private void Awake()
    {
        CacheComponents();
        FindPlayerIfNeeded();
        ApplyTarget();
    }

    // Refresh the camera components and player binding whenever this component becomes enabled.
    private void OnEnable()
    {
        CacheComponents();
        FindPlayerIfNeeded();
        ApplyTarget();
    }

    // After normal updates, recover a missing player reference and correct Follow/LookAt if they no longer match.
    private void LateUpdate()
    {
        FindPlayerIfNeeded();
        if (player != null && (cinemachineCamera.Follow != player || cinemachineCamera.LookAt != player))
            ApplyTarget();
    }

    // Cache the CinemachineCamera and add mouse-orbit support if it is missing.
    private void CacheComponents()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = GetComponent<CinemachineCamera>();
        if (GetComponent<CinemachineMouseOrbit>() == null)
            gameObject.AddComponent<CinemachineMouseOrbit>();
    }

    // If no player is assigned, search by the Player tag, then fall back to the object name TribalPlayer.
    private void FindPlayerIfNeeded()
    {
        if (player != null)
            return;

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
            player = taggedPlayer.transform;
        else
        {
            GameObject namedPlayer = GameObject.Find("TribalPlayer");
            if (namedPlayer != null)
                player = namedPlayer.transform;
        }
    }

    // Assign the same player as both Follow and LookAt so the camera tracks and aims at the hero.
    private void ApplyTarget()
    {
        CacheComponents();
        if (player == null)
            return;

        cinemachineCamera.Follow = player;
        cinemachineCamera.LookAt = player;
    }
}
