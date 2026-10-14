// Connects menu settings to the assigned audio sources and fullscreen setting; values are not saved here.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.
using UnityEngine.UI; // Imports Canvas-based UI types such as Button and Image.

public class OptionsMenu : MonoBehaviour // Defines an attachable component whose public methods can be connected to settings controls.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private AudioSource musicSource; // Saves the music AudioSource assigned in the Inspector.
    [SerializeField] private AudioSource sfxSource; // Saves the sound-effects AudioSource assigned in the Inspector.

    // Called by the music slider. Set the assigned music source's volume to the supplied slider value.
    public void SetMusicVolume(float volume) // Accepts the music slider's floating-point value when its event calls this method.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        musicSource.volume = volume; // Applies the supplied value to the assigned music source's volume; AudioSource volume uses 0..1.
    } // Ends the block opened by the matching opening brace.

    // Called by the sound-effects slider. Set the assigned SFX source's volume to the supplied value.
    public void SetSFXVolume(float volume) // Accepts the sound-effects slider's value when its event calls this method.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        sfxSource.volume = volume; // Applies the supplied value to the assigned sound-effects source's volume.
    } // Ends the block opened by the matching opening brace.

    // Called by the fullscreen toggle. Apply its on/off value to Unity's screen mode.
    public void SetFullscreen(bool fullscreen) // Accepts the toggle's on/off state as a bool: true means fullscreen is requested.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        Screen.fullScreen = fullscreen; // Applies the requested fullscreen state; this script does not save it as a preference.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
