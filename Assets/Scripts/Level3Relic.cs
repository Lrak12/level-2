// Collects a relic once, updates the level objective, and hides its visual effects.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

[RequireComponent(typeof(SphereCollider))] // Declares the SphereCollider required by this pickup; setup must configure it as a trigger.
public sealed class Level3Relic : MonoBehaviour // Defines the attachable relic-collection component; sealed prevents subclasses.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private Renderer crystal; // Saves the renderer whose crystal GameObject will be hidden after collection.
    private bool collected; // Remembers whether this relic was already collected; a bool starts false by default.

    // Assign the crystal renderer whose object is hidden after collection.
    public void Configure(Renderer visual) => crystal = visual; // Stores the supplied crystal renderer using a one-expression method.

    // When the player enters during active gameplay, mark the relic collected, award it through the session, and show feedback.
    // Stop its particles and hide the lights, rings, and crystal so the collected relic no longer appears available.
    private void OnTriggerEnter(Collider other) // Unity calls this with the collider that has entered the relic's trigger.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (collected || other.GetComponentInParent<TribalPlayerController>() == null) // Rejects an already collected relic or a collider with no player controller on itself or its parents.
            return; // Ends this trigger callback when the relic cannot be collected by this collider.

        IslandGameSession session = IslandGameSession.Instance; // Stores a local reference to the shared game session for the checks and objective update below.
        if (session == null || session.BlocksGameplay) // Checks whether no session exists or the session currently blocks gameplay.
            return; // Skips collection until a valid session allows gameplay.

        collected = true; // Marks this relic collected before notifying other systems, preventing another award.
        if (LevelPresentation.Instance != null) // Checks that the shared effects manager exists before requesting feedback.
            LevelPresentation.Instance.Reward(LevelSound.Relic, transform.position, new Color(2f, 1f, .18f, .85f)); // Requests the relic reward sound, particles, and ring using a bright golden color.
        session.CollectRelic(); // Notifies the session that one relic was collected so it can update progress and objectives.
        foreach (var particles in GetComponentsInChildren<ParticleSystem>()) // Loops through particle systems on this object and its active children.
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Stops emission and clears existing particles on each system and its children.
        foreach (var glow in GetComponentsInChildren<Light>()) glow.enabled = false; // Finds lights on this object and its active children and turns each one off.
        foreach (var ring in GetComponentsInChildren<LineRenderer>()) ring.enabled = false; // Finds line-rendered rings on this object and its active children and hides each one.
        if (crystal != null) // Checks that the crystal renderer was assigned before accessing its GameObject.
            crystal.gameObject.SetActive(false); // Disables the crystal's GameObject so the collected relic is no longer visible.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
