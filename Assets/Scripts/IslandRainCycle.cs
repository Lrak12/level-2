// Repeats clear/weather periods and keeps the rain or ash particle effect above the player.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

/// <summary>Local tropical showers that move with the player and recur through the level.</summary>
public sealed class IslandRainCycle : MonoBehaviour // Defines the attachable recurring-weather component; sealed prevents subclasses.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private Transform target; // Saves the Transform to follow, usually the player, as an Inspector reference.
    [SerializeField] private ParticleSystem rain; // Saves the particle system that displays rain or another configured weather effect.
    [SerializeField, Min(1f)] private float clearSeconds = 35f; // Defaults to 35 game seconds of clear weather; Min restricts Inspector input to at least one second.
    [SerializeField, Min(1f)] private float rainSeconds = 18f; // Defaults to 18 game seconds of active weather; saved Inspector values can override this default.
    [SerializeField] private string clearLabel = "CLEAR"; // Stores the text the HUD displays while the weather effect is inactive.
    [SerializeField] private string activeLabel = "RAIN"; // Stores the text the HUD displays while the weather effect is active.

    public bool IsRaining { get; private set; } // Lets other scripts read the weather state, but only this class can change it through the private setter.
    public string ConditionLabel => IsRaining ? activeLabel : clearLabel; // Returns the active or clear label according to IsRaining; ?: selects between the two strings.

    // Assign the transform to follow and the particle system used for active weather.
    public void Configure(Transform follow, ParticleSystem particles) // Lets setup code assign the target and the particle system to use.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        target = follow; // Stores the target Transform so Update can follow its position.
        rain = particles; // Stores the particle system that will be started and stopped.
    } // Ends the block opened by the matching opening brace.

    // Set clear/effect durations and the labels shown by the HUD; each phase lasts at least one second.
    public void ConfigureCycle(float quietSeconds, float effectSeconds, string quietLabel, string effectLabel) // Accepts durations and display labels for both phases of the weather cycle.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        clearSeconds = Mathf.Max(1f, quietSeconds); // Stores the requested clear duration, clamping it to at least one second.
        rainSeconds = Mathf.Max(1f, effectSeconds); // Stores the requested active-weather duration, clamping it to at least one second.
        clearLabel = quietLabel; // Replaces the label displayed during clear weather.
        activeLabel = effectLabel; // Replaces the label displayed while the weather effect runs.
    } // Ends the block opened by the matching opening brace.

    // Keep the effect above the player and use elapsed level time modulo the cycle length to choose the weather phase.
    // Only play or stop particle emission when the phase changes.
    private void Update() // Unity calls this each frame to follow the player and check whether the weather phase changed.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (target != null) // Checks that the follow target exists before reading its position.
            transform.position = target.position + Vector3.up * 11f; // Positions this effect object eleven world units above the target.

        bool shouldRain = (Time.timeSinceLevelLoad % (clearSeconds + rainSeconds)) >= clearSeconds; // Uses remainder (%) to locate the current time within a repeating cycle; the effect starts after the clear phase.
        if (shouldRain == IsRaining) // Checks whether the calculated weather state already matches the stored state.
            return; // Skips particle controls when there is no phase change, avoiding repeated Play or Stop calls.

        IsRaining = shouldRain; // Saves the new state so the HUD and other scripts can read it.
        if (rain == null) // Checks whether a particle system has been assigned.
            return; // Stops here if there is no particle system, while retaining the updated weather state.
        if (IsRaining) // Chooses the start-emitting action when the new state is active weather.
            rain.Play(); // Starts the weather particle system and, by default, its child particle systems.
        else // Handles the opposite case: the new state is clear weather.
            rain.Stop(true, ParticleSystemStopBehavior.StopEmitting); // Stops new particles on this system and its children, letting existing particles finish naturally.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
