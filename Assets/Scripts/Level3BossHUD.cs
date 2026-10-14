// Shows the final boss's name and remaining health while the boss encounter is active.
// Study guide and script map: CODE_GUIDE.md in the project root.

using TMPro; // Imports TextMeshPro types such as TMP_Text for displaying text in the interface.
using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

public sealed class Level3BossHUD : MonoBehaviour // Defines an attachable component that updates the boss health display; sealed prevents subclasses.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private IslandGameSession session; // Saves the game session used to find the boss and check encounter progress.
    [SerializeField] private GameObject panel; // Saves the panel object that is shown or hidden for the boss encounter.
    [SerializeField] private UnityEngine.UI.Image healthFill; // Saves the UI Image whose filled fraction represents the boss's remaining health.
    [SerializeField] private TMP_Text label; // Saves the TextMeshPro label used for the boss name and health numbers.

    // Connect the session, boss-panel root, health-fill image, and title text.
    public void Configure(IslandGameSession game, GameObject root, // Begins a setup method that accepts the session and panel object.
        UnityEngine.UI.Image fill, TMP_Text title) // Finishes the parameter list with the health-fill image and title label.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        session = game; // Stores the supplied session reference.
        panel = root; // Stores the panel GameObject that will be toggled on and off.
        healthFill = fill; // Stores the image whose fill amount will change with boss health.
        label = title; // Stores the text component that will show the boss name and health.
    } // Ends the block opened by the matching opening brace.

    // After normal updates, show the panel only for an active boss encounter and refresh its health fraction and label.
    private void LateUpdate() // Unity calls this after ordinary Update methods, so the HUD reads the frame's latest game state.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (session == null || panel == null) // Checks whether either the session or the panel reference is missing; || means OR.
            return; // Skips this frame's HUD update when required references are unavailable.

        IslandEnemyAI boss = session.FinalBoss; // Reads the final boss's enemy-AI component from the session.
        bool show = session.BossActive && !session.IsFinished && boss != null; // Shows the panel only if the encounter is active, the game has not finished, and a boss exists.
        if (panel.activeSelf != show) // Checks whether the panel's current active setting differs from the desired setting.
            panel.SetActive(show); // Changes the panel's active setting to match whether the boss display should be visible.
        if (!show) // Checks whether the panel should be hidden.
            return; // Skips updating invisible health details.

        healthFill.fillAmount = boss.MaxHealth > 0f ? Mathf.Clamp01(boss.Health / boss.MaxHealth) : 0f; // Converts boss health to a fraction clamped to 0..1; uses zero if maximum health is not positive.
        label.text = "NALLEGAM   " + Mathf.CeilToInt(boss.Health) + " / " + Mathf.CeilToInt(boss.MaxHealth); // Displays NALLEGAM and current/maximum health, rounding each number up to a whole number.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
