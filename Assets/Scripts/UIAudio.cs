// Plays short sounds for menu hover and click events using the assigned AudioSource.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

public class UIAudio : MonoBehaviour // Defines an attachable component whose public methods provide menu sound feedback.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private AudioSource audioSource; // Saves the AudioSource assigned in the Inspector to play the interface sounds.
    [SerializeField] private AudioClip hoverSound; // Saves the sound clip used when the pointer hovers over a connected menu control.
    [SerializeField] private AudioClip clickSound; // Saves the sound clip used when a connected menu control is clicked.

    // Called by a menu hover event. Play the hover clip once without replacing the AudioSource's main clip.
    public void PlayHover() // Exposes the method connected to a menu hover event.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        audioSource.PlayOneShot(hoverSound); // Plays the hover clip once through the assigned source without replacing its main clip.
    } // Ends the block opened by the matching opening brace.

    // Called by a menu click event. Play the click clip once as immediate button feedback.
    public void PlayClick() // Exposes the method connected to a menu click event.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        audioSource.PlayOneShot(clickSound); // Plays the click clip once through the assigned source; its source volume still applies.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
