// Detects the player entering a checkpoint and passes its index to the game session.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

[RequireComponent(typeof(SphereCollider))] // Declares that this component needs a SphereCollider; setup must still configure that collider as a trigger.
public sealed class IslandCheckpoint : MonoBehaviour // Defines the attachable checkpoint component; sealed prevents subclasses.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private int index; // Saves this checkpoint's index in the session's checkpoint list and exposes it in the Inspector.
    [SerializeField] private Renderer marker; // Saves the renderer whose color changes when the player reaches this checkpoint.
    [SerializeField] private Color activeColor = new Color(0.12f, 0.56f, 0.32f); // Defines a default green reached-checkpoint color using red, green, and blue values.

    // Set this checkpoint's position in the session's checkpoint array and its visible marker renderer.
    public void Configure(int checkpointIndex, Renderer visual) // Lets setup code provide this checkpoint's index and its visible marker.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        index = checkpointIndex; // Stores the supplied checkpoint index for the later session notification.
        marker = visual; // Stores the renderer that should show the reached-checkpoint color.
    } // Ends the block opened by the matching opening brace.

    // Set the marker color used after the player reaches this checkpoint.
    public void ConfigureActiveColor(Color color) => activeColor = color; // Stores a replacement marker color; => is a short form for a method with one expression.

    // Unity calls this when a collider enters the trigger. For the player, notify the session and recolor the marker.
    // The session decides whether this checkpoint advances the saved respawn position.
    private void OnTriggerEnter(Collider other) // Unity supplies the other collider when it enters this object's trigger.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (other.GetComponentInParent<TribalPlayerController>() == null) // Looks on the entering collider's object and parents for the player controller; rejects it if none exists.
            return; // Stops processing because the entering collider is not part of the player.
        if (IslandGameSession.Instance == null) // Checks whether the shared game session is unavailable.
            return; // Stops because there is no session to receive the checkpoint update.

        IslandGameSession.Instance.ReachCheckpoint(index); // Passes this checkpoint's index to the session, which decides whether to update the respawn checkpoint.
        if (marker != null) // Checks that a visible marker was assigned before accessing its material.
            marker.material.color = activeColor; // Changes this marker's material instance to the configured active color.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
