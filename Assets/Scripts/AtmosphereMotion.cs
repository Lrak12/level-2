// Provides small decorative motions: flickering lights, floating objects, or following a target.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

/// <summary>Small ambient motion with no material instances or frame allocations.</summary>
public sealed class AtmosphereMotion : MonoBehaviour // Defines an attachable decorative-motion component; sealed prevents other classes from inheriting it.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    public enum Motion { Flicker, Float, Follow } // Defines the three named choices for this effect: light flicker, floating/spinning, or following a target.
    [SerializeField] private Motion motion; // Saves the selected mode; SerializeField exposes this private field in the Inspector.
    [SerializeField] private Transform target; // Saves the optional Transform to follow, so the effect can read its position.
    [SerializeField] private float strength = .14f; // Default movement amplitude or flicker strength is 0.14; saved Inspector values can override it.
    [SerializeField] private float speed = 1.6f; // Sets the rate used by the floating sine wave; the default is 1.6 radians per game second.
    private Vector3 origin; // Remembers the starting position relative to this object's parent.
    private Light glow; // Holds the Light component used for flickering, if one is attached.
    private float intensity; // Remembers the light's starting brightness so flicker varies around that value.
    private float phase; // Stores an offset that gives effects at different positions different motion patterns.

    // Choose the motion mode, optional follow target, and motion strength during setup.
    public void Configure(Motion type, Transform follow = null, float amount = .14f) // Lets setup code choose the mode, optional target, and strength; omitted arguments use the listed defaults.
    { motion = type; target = follow; strength = amount; } // Stores the supplied mode, follow target, and strength in the component's fields.

    // Save the starting position/light intensity and derive a phase offset so nearby effects do not all move identically.
    private void Awake() // Unity calls this during initialization to save the values needed by later animation.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        origin = transform.localPosition; // Records the object's initial local position as the center of its floating motion.
        glow = GetComponent<Light>(); // Looks for a Light on this same GameObject.
        intensity = glow != null ? glow.intensity : 0f; // Stores the light's intensity if it exists, otherwise zero; ?: chooses between two values.
        phase = Mathf.Abs(transform.position.x * .73f + transform.position.z * .39f); // Uses the world X and Z coordinates to calculate a non-negative offset for this object's motion.
    } // Ends the block opened by the matching opening brace.

    // Apply the selected mode each frame: follow above a target, bob/spin an object, or vary a light with smooth noise.
    private void Update() // Unity calls this once each frame while the component and its object are active.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (motion == Motion.Follow) // Checks whether this effect should follow another object.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            if (target != null) transform.position = target.position + Vector3.up * 3f; // If a target exists, positions this object three world units above it.
            return; // Ends this Update after following, skipping the float and flicker code.
        } // Ends the block opened by the matching opening brace.
        if (motion == Motion.Float) // Checks whether this effect should float and spin.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * speed + phase) * strength); // Moves up and down around the starting position using a sine wave, with strength controlling the distance.
            transform.Rotate(Vector3.up, Time.deltaTime * 23f, Space.World); // Rotates around the world's vertical axis at 23 degrees per game second; deltaTime adjusts for frame duration.
        } // Ends the block opened by the matching opening brace.
        else if (glow != null) // For a mode other than Follow or Float, continues only if a Light was found.
            glow.intensity = intensity * (1f + strength * (Mathf.PerlinNoise(phase, Time.time * 5f) * 2f - 1f)); // Uses smooth noise mapped from 0..1 to -1..1 to vary brightness around the saved intensity.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
