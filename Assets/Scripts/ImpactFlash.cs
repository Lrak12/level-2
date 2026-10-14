// Briefly tints a hit character using renderer property blocks, then restores its original appearance.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

/// <summary>Brief per-renderer hit response; shared materials remain untouched.</summary>
public sealed class ImpactFlash : MonoBehaviour // Defines the attachable hit-flash component; sealed prevents subclasses.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    private Renderer[] renderers; // Stores the character's renderers so the flash can be applied to each visible model part.
    private MaterialPropertyBlock[] original; // Stores each renderer's original shader-property overrides for later restoration.
    private MaterialPropertyBlock flash; // Stores a reusable set of shader-property overrides for the flash color.
    private float until; // Stores the game-clock time when the current flash should finish.
    private bool active; // Tracks whether a flash currently needs to be restored.

    // Cache the character's skinned renderers and original property blocks, and prepare a reusable flash block.
    private void Awake() // Unity calls this during initialization to find renderers and save their starting overrides.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>(); // Finds skinned-mesh renderers on this object and its active child objects.
        original = new MaterialPropertyBlock[renderers.Length]; // Creates an array with one slot for every renderer's saved property block.
        flash = new MaterialPropertyBlock(); // Creates the property block that will be reused for hit flashes.
        for (int i = 0; i < renderers.Length; i++) // Loops through all renderer indices, starting at zero and increasing by one.
        { original[i] = new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(original[i]); } // Creates a block for this renderer and copies its current property overrides into that block.
    } // Ends the block opened by the matching opening brace.

    // Called by hit feedback. Apply the flash tint and set the short deadline for restoring the original appearance.
    public void Pulse() // Other scripts call this method when the character should briefly flash after a hit.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        until = Time.time + .09f; active = true; // Sets the flash deadline 0.09 game seconds ahead and marks the flash active.
        flash.SetColor("_BaseColor", new Color(1f, .76f, .46f)); // Sets the shader's base-color override to a warm tint using red, green, and blue values; opacity defaults to one.
        foreach (var renderer in renderers) renderer.SetPropertyBlock(flash); // Applies the flash overrides to every cached renderer without changing its shared material asset.
    } // Ends the block opened by the matching opening brace.

    // Restore the appearance once the flash deadline has passed in game time.
    private void Update() { if (active && Time.time >= until) Restore(); } // Each frame, restores the saved appearance if a flash is active and its deadline has arrived.
    // Restore any active flash when the component is disabled so the character cannot remain tinted.
    private void OnDisable() { if (active) Restore(); } // When Unity disables this component or its object, restores any active flash immediately.
    // Reapply each renderer's saved property block and mark the flash inactive.
    private void Restore() // Defines the helper that puts each renderer's saved overrides back.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].SetPropertyBlock(original[i]); // Loops through renderers, skips any missing renderer, and restores the matching saved property block.
        active = false; // Marks the flash finished so Update stops trying to restore it.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
