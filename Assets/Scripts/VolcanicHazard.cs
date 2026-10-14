// Applies repeated damage while the player touches a hazard, with a cooldown between damage attempts.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

[RequireComponent(typeof(Collider))] // Declares that the hazard needs a Collider; its configured collider must use trigger mode for these callbacks.
public sealed class VolcanicHazard : MonoBehaviour // Defines the attachable repeating-damage component; sealed prevents subclasses.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField, Min(0f)] private float damage = 10f; // Defaults to 10 damage per accepted damage attempt; Min prevents negative Inspector input.
    [SerializeField, Min(0.1f)] private float tickSeconds = 1.2f; // Defaults to a 1.2-second gap between damage attempts; the Inspector minimum is 0.1 seconds.
    private float nextDamageTime; // Stores the earliest game-clock time when this hazard may try damaging the player again.

    // Set the damage per tick and minimum time between ticks, clamping both to their allowed ranges.
    public void Configure(float amount, float interval) // Lets setup code provide the damage amount and time between damage attempts.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        damage = Mathf.Max(0f, amount); // Stores the requested damage, clamping negative amounts to zero.
        tickSeconds = Mathf.Max(0.1f, interval); // Stores the requested interval, clamping it to at least 0.1 seconds.
    } // Ends the block opened by the matching opening brace.

    // Attempt a damage tick when a collider first enters the hazard.
    private void OnTriggerEnter(Collider other) => TryDamage(other); // When Unity reports a collider entering the trigger, passes it to the damage check.

    // Attempt further damage ticks while a collider remains inside the hazard.
    private void OnTriggerStay(Collider other) => TryDamage(other); // While a collider stays in the trigger, repeats the damage check; the timer controls how often damage is attempted.

    // Damage a living player only after the cooldown and while gameplay is active, then set the next allowed tick time.
    private void TryDamage(Collider other) // Defines the shared helper used for both entering and remaining inside the hazard.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (Time.time < nextDamageTime || // Starts an early-exit check for a cooldown that has not yet expired.
            (IslandGameSession.Instance != null && IslandGameSession.Instance.BlocksGameplay)) // Also stops when an existing game session blocks gameplay; && checks the session exists before reading it.
            return; // Skips this damage attempt if either blocking condition is true.

        TribalPlayerController player = other.GetComponentInParent<TribalPlayerController>(); // Looks for the player controller on the collider's object or one of its parents.
        if (player == null || player.IsDead) // Checks whether this collider is not a player or belongs to a dead player.
            return; // Skips damage when no living player was found.

        player.TakeDamage(damage); // Asks the player to take the configured damage; the player's own immunity checks may reject it.
        nextDamageTime = Time.time + tickSeconds; // Schedules the next permitted attempt, even if the player rejected this damage.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
