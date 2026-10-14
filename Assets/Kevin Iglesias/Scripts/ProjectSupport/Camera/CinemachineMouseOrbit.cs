// Turns mouse movement into horizontal and vertical camera orbit around the player's look-at point.
// Study guide and script map: CODE_GUIDE.md in the project root.

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public sealed class CinemachineMouseOrbit : CinemachineExtension
{
    [Header("Mouse Look")]
    [SerializeField, Min(0.01f)] private float sensitivity = 0.12f;
    [SerializeField] private float minimumPitch = -25f;
    [SerializeField] private float maximumPitch = 55f;
    [SerializeField] private bool invertVertical;
    [SerializeField] private bool lockCursorOnPlay = true;

    private float yaw;
    private float pitch;
    private bool pitchInitialized;
    private bool ignoreNextMouseDelta;

    // Register the Cinemachine extension, reset orbit initialization, and optionally lock the cursor when playing.
    protected override void OnEnable()
    {
        base.OnEnable();
        yaw = 0f;
        pitchInitialized = false;
        if (Application.isPlaying && lockCursorOnPlay)
            SetCursorLocked(true);
    }

    // Read mouse movement while the cursor is locked and gameplay is active; accumulate yaw and clamp vertical pitch.
    // Handle cursor release/recapture and ignore the first mouse delta after locking to avoid a sudden jump.
    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (IslandGameSession.Instance != null && IslandGameSession.Instance.BlocksGameplay)
        {
            if (Cursor.lockState != CursorLockMode.None)
                SetCursorLocked(false);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetCursorLocked(false);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            SetCursorLocked(true);

        if (mouse == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 mouseDelta = mouse.delta.ReadValue();
        if (ignoreNextMouseDelta)
        {
            ignoreNextMouseDelta = false;
            return;
        }
        yaw = Mathf.Repeat(yaw + mouseDelta.x * sensitivity, 360f);
        float verticalDirection = invertVertical ? -1f : 1f;
        if (pitchInitialized)
            pitch = Mathf.Clamp(pitch - mouseDelta.y * sensitivity * verticalDirection, minimumPitch, maximumPitch);
    }

    // Cinemachine calls this during camera processing. At Finalize, rotate its offset around the look-at pivot.
    // Keep the original camera distance, then apply position/orientation corrections for the requested orbit.
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || !state.HasLookAt())
            return;

        Vector3 pivot = state.ReferenceLookAt;
        Vector3 originalPosition = state.GetFinalPosition();
        Vector3 originalOffset = originalPosition - pivot;
        if (originalOffset.sqrMagnitude < 0.001f)
            return;

        Vector3 horizontalOffset = Vector3.ProjectOnPlane(originalOffset, Vector3.up);
        if (horizontalOffset.sqrMagnitude < 0.001f)
            return;
        if (!pitchInitialized)
        {
            pitch = Mathf.Clamp(Mathf.Atan2(originalOffset.y, horizontalOffset.magnitude) * Mathf.Rad2Deg,
                minimumPitch, maximumPitch);
            pitchInitialized = true;
        }

        Vector3 horizontalDirection = Quaternion.AngleAxis(yaw, Vector3.up) * horizontalOffset.normalized;
        float distance = originalOffset.magnitude;
        float pitchRadians = pitch * Mathf.Deg2Rad;
        Vector3 desiredOffset = distance *
            (horizontalDirection * Mathf.Cos(pitchRadians) + Vector3.up * Mathf.Sin(pitchRadians));
        Vector3 desiredPosition = pivot + desiredOffset;
        Quaternion desiredOrientation = Quaternion.LookRotation(-desiredOffset, Vector3.up);

        state.PositionCorrection += desiredPosition - originalPosition;
        state.OrientationCorrection = Quaternion.Inverse(state.RawOrientation) * desiredOrientation;
    }

    // Lock and hide the cursor for mouse look, or release and show it for menus; ignore the next delta when locking.
    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        ignoreNextMouseDelta = locked;
    }
}
