// Adds the final boss's warning ring and delayed area slam on top of the ordinary enemy AI.
// Study guide and script map: CODE_GUIDE.md in the project root.

using System.Collections; // Makes IEnumerator available so the slam can pause and continue as a coroutine.
using UnityEngine; // Makes Unity types such as MonoBehaviour, Transform, Vector3, and Time available.
using UnityEngine.AI; // Makes NavMeshAgent available for controlling the boss's navigation.

/// <summary>A telegraphed area attack used only by the Ashen Citadel final boss.</summary>
[RequireComponent(typeof(IslandEnemyAI), typeof(NavMeshAgent))] // Tells Unity to add these two required components when this boss component is added.
public sealed class Level3FinalBoss : MonoBehaviour // Defines an attachable Unity component; public allows access, and sealed prevents subclasses.
{ // Begins the Level3FinalBoss class: its fields and methods are inside these braces.
    [SerializeField] private Transform target; // Stores the player's position reference; SerializeField exposes this private field in the Inspector.
    [SerializeField] private LineRenderer warningRing; // Stores the assigned warning-ring renderer; this script shows and hides it rather than creating it.
    [SerializeField, Min(1f)] private float slamRadius = 4.3f; // Default hit radius is 4.3 world units; Min limits Inspector input to at least 1. The f means float.
    [SerializeField, Min(0f)] private float slamDamage = 18f; // Default damage is 18; Inspector input cannot be negative. Saved scene values can override this default.
    [SerializeField, Min(0.2f)] private float windupSeconds = 1.15f; // Default warning time is 1.15 game seconds before impact; Inspector minimum is 0.2.
    [SerializeField, Min(1f)] private float slamCooldown = 8f; // Default gap between slam starts is 8 game seconds; Inspector minimum is 1.

    private IslandEnemyAI enemy; // Keeps a reference to the ordinary enemy AI, which also reports whether the boss is dead.
    private NavMeshAgent agent; // Keeps a reference to the component that moves the boss along the navigation mesh.
    private float nextSlamTime; // Stores the game-clock time when another slam is allowed to start.
    private bool slamming; // True means a slam is already running; it prevents starting a second slam at the same time.

    // Assign the player target and the LineRenderer used to warn about the slam area.
    public void Configure(Transform player, LineRenderer ring) // Other scripts call this method to provide the player and warning ring; void means no returned value.
    { // Begins Configure.
        target = player; // Saves the supplied player Transform as the target of the slam.
        warningRing = ring; // Saves the supplied LineRenderer so the attack can display its warning.
    } // Ends Configure.

    // Set slam damage and cooldown while keeping damage non-negative and cooldown at least one second.
    public void ConfigureAttack(float damage, float cooldown) // Lets another script set the damage amount and seconds between slam starts.
    { // Begins ConfigureAttack.
        slamDamage = Mathf.Max(0f, damage); // Keeps whichever is larger: zero or the supplied damage, preventing negative damage.
        slamCooldown = Mathf.Max(1f, cooldown); // Keeps whichever is larger: one second or the supplied cooldown.
    } // Ends ConfigureAttack.

    // Cache the enemy/navigation components, delay the first slam, and hide the warning ring.
    private void Awake() // Unity calls Awake during this component's initialization, before its first Update.
    { // Begins Awake.
        enemy = GetComponent<IslandEnemyAI>(); // Finds and stores the enemy AI attached to this same GameObject.
        agent = GetComponent<NavMeshAgent>(); // Finds and stores the navigation agent attached to this same GameObject.
        nextSlamTime = Time.time + 4f; // Delays the first possible slam until four game seconds after initialization.
        if (warningRing != null) // Checks that a warning ring exists before accessing it; null means no usable reference.
            warningRing.enabled = false; // Hides the ring at startup; without braces, only this next statement belongs to the if.
    } // Ends Awake.

    // Check for a living boss, available cooldown, active gameplay, and a nearby target before starting a slam.
    // Hide the warning if the boss has died.
    private void Update() // Unity calls Update once each frame while this component is enabled and its object is active.
    { // Begins Update.
        if (enemy.IsDead) // Checks the ordinary enemy AI's death state before allowing any new attack.
        { // Begins the block that runs when the boss is dead.
            if (warningRing != null) // Makes sure the ring exists before trying to hide it.
                warningRing.enabled = false; // Removes the attack warning because a dead boss cannot slam.
            return; // Ends this frame's Update immediately, skipping every check below.
        } // Ends the dead-boss block.

        if (slamming || Time.time < nextSlamTime || // Starts an OR check: stop if already slamming or if the cooldown has not expired.
            (IslandGameSession.Instance != null && IslandGameSession.Instance.BlocksGameplay)) // Also stop if a session exists AND blocks gameplay; && avoids reading a missing session.
            return; // Skips the remaining Update code whenever any of the conditions above is true.

        if (target == null) // Searches for a player only when no usable target has been assigned.
        { // Begins the target-search block.
            GameObject player = GameObject.FindGameObjectWithTag("Player"); // Finds an active GameObject carrying the Player tag.
            if (player != null) // Checks that the search actually found a player object.
                target = player.transform; // Stores that object's Transform so the boss can read its position.
        } // Ends the target-search block.

        if (target != null && Vector3.Distance(transform.position, target.position) <= 8f) // Requires a target within 8 world units in 3D; this starts the warning, not guaranteed damage.
            StartCoroutine(Slam()); // Starts the attack coroutine, which can wait across frames without freezing the game.
    } // Ends Update.

    // Coroutine that temporarily stops regular AI, shows a warning, waits, and damages a target still inside the radius.
    // Restore regular AI/navigation if the boss survived; the warning gives the player time to escape.
    private IEnumerator Slam() // Defines the coroutine; IEnumerator allows yield statements to pause and resume its execution.
    { // Begins Slam.
        slamming = true; // Marks the attack as active so Update cannot start another slam while this one runs.
        if (LevelPresentation.Instance != null) // Checks whether the shared sound-and-effects manager exists.
            LevelPresentation.Instance.Play(LevelSound.Boss_Warning, transform.position, .65f); // Requests the warning sound at the boss's position with a 0.65 volume multiplier.
        nextSlamTime = Time.time + slamCooldown; // Starts the cooldown now, at the beginning of the warning rather than after the impact.
        enemy.StopAllCoroutines(); // Cancels coroutines on the ordinary AI, including pending attacks; this separate Slam coroutine keeps running.
        enemy.enabled = false; // Pauses the ordinary AI's Update so its normal actions do not compete with the slam.
        if (agent.isOnNavMesh) // Checks that the navigation agent is placed on a NavMesh before using its movement controls.
        { // Begins the navigation-stop block.
            agent.isStopped = true; // Stops the agent from moving while the boss prepares its attack.
            agent.ResetPath(); // Clears the old navigation route so the boss does not continue following it.
        } // Ends the navigation-stop block.

        if (warningRing != null) // Only changes the warning ring when a renderer has been assigned.
        { // Begins the warning-display block.
            warningRing.transform.localPosition = Vector3.up * 0.15f; // Places the ring at local position (0, 0.15, 0), slightly above its parent's origin.
            warningRing.widthMultiplier = 0.14f; // Sets the starting line-thickness multiplier; this does not change the slam's hit radius.
            warningRing.enabled = true; // Makes the warning ring visible so the player can react.
        } // Ends the warning-display block.

        float elapsed = 0f; // Starts a local timer at zero for this attack's warning period.
        while (elapsed < windupSeconds && !enemy.IsDead) // Repeats while warning time remains AND the boss is alive; ! means NOT.
        { // Begins one iteration of the warning loop.
            elapsed += Time.deltaTime; // Adds this frame's scaled duration; a timeScale of zero stops this timer from advancing.
            if (warningRing != null) // Checks the renderer again before changing its thickness.
                warningRing.widthMultiplier = Mathf.Lerp(0.14f, 0.36f, elapsed / windupSeconds); // Blends thickness from 0.14 to 0.36 using the completed fraction of the warning time.
            yield return null; // Pauses only this coroutine until the next frame, allowing the rest of the game to keep updating.
        } // Ends this loop iteration; the while condition is checked again after resuming.

        if (warningRing != null) // Checks that the ring still exists after the warning loop.
            warningRing.enabled = false; // Hides the warning when the wait finishes or the boss dies.

        if (!enemy.IsDead && target != null && // Requires the boss to still be alive and the target to still exist before applying impact.
            (IslandGameSession.Instance == null || !IslandGameSession.Instance.BlocksGameplay)) // Also requires either no session or a session that allows gameplay; || means OR.
        { // Begins the impact block; it runs only if all the required checks pass.
            if (LevelPresentation.Instance != null) // Plays impact effects only when the shared effects manager exists.
            { // Begins the impact sound-and-effects block.
                LevelPresentation.Instance.Play(LevelSound.Boss_Slam, transform.position, 1f); // Requests the slam sound at the boss's position with a volume multiplier of 1.
                LevelPresentation.Instance.Ring(transform.position, new Color(2f, .65f, .08f, .9f), slamRadius); // Displays an orange impact ring with this radius; Color values are red, green, blue, and opacity.
                LevelPresentation.Instance.Burst(transform.position + Vector3.up * .2f, new Color(2f, .55f, .1f, .8f), 90, 6f, .18f); // Emits 90 orange particles 0.2 units above the boss, using speed scale 6 and size scale 0.18.
            } // Ends the effects block; the damage check below can still run without the effects manager.
            Vector3 delta = target.position - transform.position; // Calculates the position difference from the boss to the target at impact time.
            delta.y = 0f; // Ignores height, so the hit check measures horizontal distance along the ground.
            if (delta.sqrMagnitude <= slamRadius * slamRadius) // Compares squared distance with squared radius, including the boundary and avoiding a square root.
                target.SendMessage("TakeDamage", slamDamage, SendMessageOptions.DontRequireReceiver); // Calls TakeDamage on components on the target object, passing damage; a missing receiver causes no error.
        } // Ends the impact block.

        if (!enemy.IsDead) // Restores ordinary behavior only if the boss survived the attack sequence.
        { // Begins the surviving-boss recovery block.
            enemy.enabled = true; // Allows the ordinary enemy AI to run Update and choose its next action again.
            if (agent.isOnNavMesh) // Checks that the navigation agent is still on a valid NavMesh.
                agent.isStopped = false; // Allows navigation movement again when the AI supplies a route.
        } // Ends the recovery block.
        slamming = false; // Clears the active-attack flag; the next slam must still wait for its cooldown.
    } // Ends Slam; reaching here completes the coroutine.
} // Ends the Level3FinalBoss class.
