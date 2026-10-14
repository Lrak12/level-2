// Handles the intro menu buttons: New Game, Options, How to Play, and Quit.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.
using UnityEngine.SceneManagement; // Imports SceneManager and related types used to load and manage game scenes.

public class MainMenu : MonoBehaviour // Defines the attachable component that provides the main menu's button actions.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private GameObject mainMenuPanel; // Saves the main menu panel assigned in the Inspector.
    [SerializeField] private GameObject optionsPanel; // Saves the settings/options panel that replaces the main panel when opened.
    [SerializeField] private GameObject howToPlayPanel; // Saves the instructions panel that displays how to play.

    [SerializeField, Tooltip("First playable scene. Set this to level 1 once it is imported.")] // Makes the following private scene field editable in the Inspector and provides its hover-help text.
    private string firstGameplayScene = "Assets/Scenes/Stage2_Battlefield.unity"; // Defaults New Game to the Stage2 scene path; a saved Inspector value can choose a different first scene.

    public string FirstGameplayScene => firstGameplayScene; // Provides a public read-only property returning the configured first playable scene.
    private bool startingGame; // Tracks whether a scene load has already been requested, guarding against repeated button clicks.

    // Restore normal time and a visible unlocked cursor so the intro menu stays usable after returning from gameplay.
    private void Awake() // Unity calls this during initialization to restore menu-friendly time and cursor settings.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        Time.timeScale = 1f; // Resumes normal game-time speed, including when the previous scene left the game paused.
        Cursor.lockState = CursorLockMode.None; // Releases any cursor lock so the pointer can move freely over the menu.
        Cursor.visible = true; // Makes the mouse cursor visible for selecting menu controls.
    } // Ends the block opened by the matching opening brace.

    // Called by New Game. Confirm the configured first scene can load, prevent double-click loads, and enter that scene.
    public void NewGame() // Defines the public method connected to the New Game button.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (startingGame) return; // Immediately ignores the click if a game-start request is already underway.
        if (!Application.CanStreamedLevelBeLoaded(firstGameplayScene)) // Checks whether Unity can load the configured scene from the available build scenes.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            Debug.LogError("The starting scene is missing from the build: " + firstGameplayScene, this); // Reports the missing scene path as an error and associates the message with this component.
            return; // Stops the start request instead of attempting to load an unavailable scene.
        } // Ends the block opened by the matching opening brace.

        startingGame = true; // Marks the start request active so subsequent clicks are ignored.
        Time.timeScale = 1f; // Ensures the new gameplay scene begins with normal time progression.
        SceneManager.LoadScene(firstGameplayScene); // Requests the configured scene using the default Single mode, replacing the current scene.
    } // Ends the block opened by the matching opening brace.

    // Called by Options. Hide the main panel and show the settings panel.
    public void OpenOptions() // Defines the action connected to the Options button.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        mainMenuPanel.SetActive(false); // Hides the main menu panel and its child controls.
        optionsPanel.SetActive(true); // Shows the options panel and its child controls.
    } // Ends the block opened by the matching opening brace.

    // Called by the settings Back button. Hide settings and restore the main panel.
    public void CloseOptions() // Defines the action connected to the options panel's Back button.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        optionsPanel.SetActive(false); // Hides the options panel.
        mainMenuPanel.SetActive(true); // Shows the main menu panel again.
    } // Ends the block opened by the matching opening brace.

    // Called by How to Play. Hide the main panel and show the controls/help panel.
    public void OpenHowToPlay() // Defines the action connected to the How to Play button.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        mainMenuPanel.SetActive(false); // Hides the main menu before displaying instructions.
        howToPlayPanel.SetActive(true); // Shows the how-to-play panel.
    } // Ends the block opened by the matching opening brace.

    // Called by the help Back button. Hide help and restore the main panel.
    public void CloseHowToPlay() // Defines the action connected to the instructions panel's Back button.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        howToPlayPanel.SetActive(false); // Hides the how-to-play panel.
        mainMenuPanel.SetActive(true); // Returns the user to the main menu panel.
    } // Ends the block opened by the matching opening brace.

    // Called by Quit. Quit the built application, or stop Play mode when running inside the Unity Editor.
    public void QuitGame() // Defines the action connected to the Quit button.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        Application.Quit(); // Requests application shutdown in a built game; Unity ignores this call in the Editor.

#if UNITY_EDITOR // Compiles the following code only for the Unity Editor, keeping the Editor API out of game builds.
        UnityEditor.EditorApplication.isPlaying = false; // Stops Play mode when this menu runs inside the Unity Editor.
#endif // Ends the Editor-only compilation section.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
