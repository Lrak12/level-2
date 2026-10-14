// Animates a healing pickup and consumes it only when a living player actually receives healing.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

[RequireComponent(typeof(SphereCollider))] // Declares the SphereCollider needed by the pickup; setup must also mark the collider as a trigger.
public sealed class Level3HealingPickup : MonoBehaviour // Defines the attachable healing-pickup component; sealed prevents subclasses.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private Transform floatingVisual; // Saves the visual Transform that should float and spin without moving the trigger object.
    [SerializeField, Min(0f)] private float bobHeight = 0.18f; // Sets the default floating amplitude to 0.18 local units; Min prevents negative Inspector input.
    [SerializeField, Min(0f)] private float spinDegreesPerSecond = 45f; // Sets the default spinning speed to 45 degrees per game second.

    private Vector3 restingLocalPosition; // Remembers the visual's starting position relative to its parent.
    private bool collected; // Tracks whether this pickup has already been used, preventing repeated collection.

    // Assign the visual child that should float and spin above the pickup.
    public void Configure(Transform visual) => floatingVisual = visual; // Stores the supplied visual Transform; => is shorthand for this one-expression method body.

    // Save the visual's resting local position before its floating animation starts.
    private void Start() // Unity calls this before the component's first Update to record its starting position.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (floatingVisual != null) // Checks that a visual was assigned before reading its position.
            restingLocalPosition = floatingVisual.localPosition; // Saves the resting local position as the center of the floating motion.
    } // Ends the block opened by the matching opening brace.

    // While uncollected, bob the visual with a sine wave and rotate it at a frame-rate-adjusted speed.
    private void Update() // Unity calls this each frame to animate an uncollected pickup.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (floatingVisual == null || collected) // Checks whether the visual is missing OR the pickup has already been collected.
            return; // Skips animation if either condition above is true.

        floatingVisual.localPosition = restingLocalPosition + // Starts calculating the visual's new position from its saved resting position.
            Vector3.up * (Mathf.Sin(Time.time * 2.5f) * bobHeight); // Adds a vertical sine-wave offset with the configured height and a rate of 2.5 radians per game second.
        floatingVisual.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self); // Rotates around the visual's local Y axis; deltaTime converts degrees per second to this frame's rotation.
    } // Ends the block opened by the matching opening brace.

    // When a collider first enters the trigger, attempt to heal its player.
    private void OnTriggerEnter(Collider other) => TryCollect(other); // When Unity reports a collider entering the trigger, passes that collider to the collection check.

    // Retry healing while a collider stays inside, so a player who later needs health can still collect the pickup.
    private void OnTriggerStay(Collider other) => TryCollect(other); // While a collider remains in the trigger, retries collection so later damage can make healing useful.

    // Reject blocked gameplay or non-player colliders; ask the player to heal and consume the pickup only if healing succeeds.
    // Play the reward effect and disable this object so it cannot be collected twice.
    private void TryCollect(Collider other) // Defines the helper that checks the entering collider and attempts to heal its player.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (collected || // Starts an early-exit check for an already collected pickup.
            (IslandGameSession.Instance != null && IslandGameSession.Instance.BlocksGameplay)) // Also blocks collection when an existing game session blocks gameplay.
            return; // Stops the collection attempt when either condition above is true.

        TribalPlayerController player = other.GetComponentInParent<TribalPlayerController>(); // Finds a player controller on the collider's own object or one of its parent objects.
        if (player == null || !player.HealToFull()) // Rejects non-players or unsuccessful healing; short-circuit OR avoids calling HealToFull on a missing player.
            return; // Leaves the pickup available if no living, injured player received healing.

        collected = true; // Marks the pickup consumed before playing effects or disabling it.
        if (LevelPresentation.Instance != null) // Checks that the shared effects manager exists.
            LevelPresentation.Instance.Reward(LevelSound.Heal, transform.position, new Color(.15f, 1.6f, .6f, .85f)); // Requests the healing sound, particles, and ring at this position with a bright green color.
        gameObject.SetActive(false); // Disables the pickup object, including its visuals and collider, so it cannot be collected again.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
