// Converts the player's current health into the width and color of the HUD health bar.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

[DisallowMultipleComponent] // Prevents attaching more than one copy of this component to the same GameObject.
public sealed class PlayerHealthBar : MonoBehaviour // Defines the attachable player-health display component; sealed prevents subclasses.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private TribalPlayerController player; // Saves the player controller whose health the bar reads.
    [SerializeField] private RectTransform fillRect; // Saves the fill rectangle whose right anchor controls the visible bar width.
    [SerializeField] private UnityEngine.UI.Image fillImage; // Saves the fill image whose tint changes from red to green with health.

    private static readonly Color HealthyColor = new Color(0.2f, 0.86f, 0.25f, 1f); // Defines a shared, non-reassignable green color for full health; Color arguments are red, green, blue, and opacity.
    private static readonly Color HurtColor = new Color(0.95f, 0.16f, 0.1f, 1f); // Defines the shared red color used when the health fraction approaches zero.

    // Assign the player and the UI objects whose width and color represent health.
    public void Configure(TribalPlayerController owner, RectTransform fill, UnityEngine.UI.Image image) // Lets setup code supply the player and the bar's size/color components.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        player = owner; // Stores the player reference from which health values will be read.
        fillRect = fill; // Stores the RectTransform used to resize the visible fill.
        fillImage = image; // Stores the Image used to change the fill color.
    } // Ends the block opened by the matching opening brace.

    // Read health after normal updates, convert it to a 0-to-1 fraction, and resize/recolor the fill.
    // The bar moves from red at low health toward green at full health.
    private void LateUpdate() // Unity calls this after normal Update methods so the display can reflect the latest health.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (player == null || fillRect == null || fillImage == null) // Checks whether any required player, rectangle, or image reference is missing.
            return; // Skips this frame's display update when a required reference is missing.

        float normalizedHealth = player.MaxHealth > 0f // Starts a conditional calculation and checks for positive maximum health before dividing.
            ? Mathf.Clamp01(player.Health / player.MaxHealth) // If maximum health is positive, divides current health by maximum health and clamps the result to 0..1.
            : 0f; // Otherwise uses an empty-bar fraction of zero, avoiding division by zero.
        fillRect.anchorMax = new Vector2(normalizedHealth, 1f); // Moves the rectangle's right anchor to the health fraction while keeping the upper vertical anchor at one.
        fillImage.color = Color.Lerp(HurtColor, HealthyColor, normalizedHealth); // Blends from the red hurt color to the green healthy color using the health fraction.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
