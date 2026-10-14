// Controls the hero: camera-relative movement, jumping, stamina, attacks, damage, and respawning.
// Study guide and script map: CODE_GUIDE.md in the project root.

using System.Collections; // Imports IEnumerator so coroutine methods can pause with yield and continue later.
using System.Collections.Generic; // Imports generic collections such as HashSet, which stores unique objects without duplicates.
using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.
using UnityEngine.InputSystem; // Imports Input System types used to read the current keyboard and mouse.

[RequireComponent(typeof(CharacterController), typeof(Animator))] // Tells Unity this player component requires a CharacterController for movement and an Animator for animations.
public sealed class TribalPlayerController : MonoBehaviour // Defines an attachable player component; public permits access from other scripts, and sealed prevents subclasses.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [Header("Movement")] // Adds a Movement heading in the Inspector to group the following settings.
    [SerializeField, Min(0.1f)] private float walkSpeed = 2.2f; // Default walking speed is 2.2 units per second; SerializeField exposes the private field and Min limits Inspector input.
    [SerializeField, Min(0.1f)] private float runSpeed = 4.5f; // Default sprinting speed is 4.5 units per second; saved scene values can override this default.
    [SerializeField, Min(1f)] private float rotationSpeed = 12f; // Controls how quickly rotation blends toward movement direction; this is a blending factor, not degrees per second.
    [SerializeField] private float gravity = -24f; // Sets downward acceleration to -24 units per second squared by default; negative Y points downward.
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.25f; // Sets the intended jump height to 1.25 units by default; the launch-speed formula uses this value.
    private const float CoyoteTime = 0.10f; // Allows a jump within 0.10 game seconds of leaving the ground; const means this value cannot change at runtime.

    [Header("Combat")] // Adds a Combat heading above the visible combat settings in the Inspector.
    private const float AttackDuration = 0.9f; // Sets one light-attack step's total coroutine wait time to 0.9 game seconds.
    private const float AttackImpactDelay = 0.28f; // Delays a light attack's damage check by 0.28 game seconds to match the strike.
    private const float ComboResetDelay = 0.8f; // Keeps the combo available for another 0.8 game seconds after a light attack finishes.
    private const float AttackMovementMultiplier = 0.4f; // Reduces normal movement to 40 percent while performing a light attack.
    [SerializeField, Min(0f)] private float attackDamage = 35f; // Sets default light-attack damage to 35; Min prevents negative Inspector input.
    [SerializeField, Min(0.1f)] private float attackReach = 1.6f; // Sets the default forward distance to the center of a light attack's hit sphere to 1.6 units.
    [SerializeField, Min(1f)] private float maxHealth = 100f; // Sets maximum health to 100 by default, with an Inspector minimum of one.

    [Header("Heavy attack")] // Adds a Heavy attack heading to organize Inspector settings.
    [SerializeField, Min(1f)] private float heavyDamageMultiplier = 2f; // Multiplies normal damage by two for heavy attacks by default; Inspector input must be at least one.
    [SerializeField, Min(0f)] private float heavyStaminaCost = 25f; // Sets the default stamina price of one heavy attack to 25.
    private const float HeavyDuration = 1.4f; // Sets the heavy attack's total coroutine wait time to 1.4 game seconds.
    private const float HeavyImpactDelay = 0.48f; // Schedules the heavy attack's impact 0.48 game seconds after starting it.

    [Header("Stamina and dash")] // Adds a Stamina and dash heading in the Inspector.
    [SerializeField, Min(1f)] private float maxStamina = 100f; // Sets the default maximum stamina pool to 100, with an Inspector minimum of one.
    [SerializeField, Min(0f)] private float sprintStaminaPerSecond = 22f; // Sets the default sprint drain to 22 stamina per game second.
    [SerializeField, Min(0f)] private float staminaRecoveryPerSecond = 18f; // Sets the default recovery rate to 18 stamina per game second once recovery is allowed.
    [SerializeField, Min(0f)] private float dashStaminaCost = 35f; // Sets the default cost of starting a dash to 35 stamina.
    [SerializeField, Min(0.1f)] private float dashSpeed = 12f; // Sets the default dash movement speed to 12 world units per second.
    [SerializeField, Min(0.05f)] private float dashDuration = 0.22f; // Sets the default dash duration to 0.22 game seconds; Inspector input cannot be below 0.05.

    private CharacterController controller; // Stores the collision-aware movement component attached to the player.
    private Animator animator; // Stores the Animator that receives walking, jumping, attack, and death parameters.
    private Transform cameraTransform; // Stores the camera Transform so movement can follow the viewing direction.
    private float verticalVelocity; // Tracks upward or downward speed separately from horizontal movement.
    private float health; // Stores current health, which can change independently of maximum health.
    private float stamina; // Stores current stamina, spent by sprinting, dashing, and heavy attacks.
    private float nextStaminaRecoveryTime; // Stores the earliest game-clock time when stamina recovery may resume.
    private float dashRemaining; // Stores how many game seconds remain in the current dash.
    private Vector3 dashDirection; // Stores the direction chosen at dash start so the dash maintains that direction.
    private bool exhausted; // Tracks sprint exhaustion, which prevents sprinting again until enough stamina recovers.
    private bool dead; // Tracks whether the player has died.
    private bool attacking; // Tracks whether either a light or heavy attack is active.
    private bool heavyAttacking; // Tracks specifically whether a heavy attack is active.
    private bool jumpConsumed; // Remembers whether the available jump was used, preventing a second jump before landing.
    private float lastGroundedTime = float.NegativeInfinity; // Remembers the last grounded time; negative infinity initially prevents a false coyote-time allowance.
    private readonly HashSet<IslandEnemyAI> damagedEnemies = new HashSet<IslandEnemyAI>(); // Creates a reusable set of enemies hit by one strike; readonly prevents replacing the set, but its contents can change.
    private int comboStep; // Tracks the current numbered step of the three-hit light-attack combo.
    private int queuedComboClicks; // Counts accepted extra clicks waiting to continue the light-attack combo.
    private float comboResetTime; // Stores the deadline after which a later light attack starts a fresh combo.

    public float Health => health; // Lets other scripts read current health without directly assigning the private health field; => returns the expression.
    public float MaxHealth => maxHealth; // Provides read-only access to maximum health for HUDs and other systems.
    public float Stamina => stamina; // Provides read-only access to current stamina.
    public float MaxStamina => maxStamina; // Provides read-only access to maximum stamina.
    public bool IsDead => dead; // Provides read-only access to the player's death flag.
    public bool IsGrounded => controller != null && controller.isGrounded && verticalVelocity <= 0f; // Reports grounded only when a controller exists, it reports ground contact, and vertical speed is not upward.
    public bool IsAttacking => attacking; // Lets other systems read whether an attack is currently running.
    public bool IsHeavyAttacking => heavyAttacking; // Lets other systems distinguish a heavy attack from other actions.
    private bool GameplayBlocked => dead || (IslandGameSession.Instance != null && IslandGameSession.Instance.BlocksGameplay); // Blocks input when dead or when an existing session blocks gameplay; && avoids accessing a missing session.

    // Unity calls this when the component loads. Cache the controller, Animator, and camera, then fill health and stamina.
    // Disable animation root motion because this script moves the character through CharacterController.Move.
    private void Awake() // Unity calls this during initialization to cache components and set initial player state.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        controller = GetComponent<CharacterController>(); // Finds and caches the CharacterController on this same GameObject.
        animator = GetComponent<Animator>(); // Finds and caches the Animator on this same GameObject.
        cameraTransform = Camera.main != null ? Camera.main.transform : null; // Caches the main camera's Transform if a main camera exists; otherwise stores null.
        health = maxHealth; // Starts the player with full health.
        stamina = maxStamina; // Starts the player with full stamina.
        animator.applyRootMotion = false; // Prevents animation root motion from moving the object because this controller supplies movement.
    } // Ends the block opened by the matching opening brace.

    // Unity calls this before the first Update. Place the player's feet on the ground and fit the collision capsule.
    private void Start() // Unity calls Start before the first Update, after this component has initialized.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        SnapToGround(); // Places the player's feet on the ground and adjusts its collision capsule.
    } // Ends the block opened by the matching opening brace.

    // Unity calls this every frame. Read keyboard/mouse input, manage stamina, move the player, and update animation.
    // Skip input while dead or when the session blocks gameplay; movement uses deltaTime to account for frame length.
    private void Update() // Unity calls this each frame while the component and GameObject are active.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (GameplayBlocked) // Checks whether death or the game session currently prevents gameplay input.
            return; // Ends this Update immediately while gameplay is blocked.

        Keyboard keyboard = Keyboard.current; // Gets the currently active keyboard from Unity's Input System.
        if (keyboard == null) // Checks whether a keyboard device is unavailable.
            return; // Skips input and movement for this frame when no keyboard is available.

        bool wasGrounded = IsGrounded; // Saves the grounded state before this frame's input and movement, allowing landing detection later.
        if (wasGrounded) // Updates grounded bookkeeping only if the player started this frame on the ground.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            lastGroundedTime = Time.time; // Refreshes the last grounded timestamp used by the short coyote-time window.
            jumpConsumed = false; // Makes the jump available again after the player has landed.
        } // Ends the block opened by the matching opening brace.
        if (keyboard.spaceKey.wasPressedThisFrame) TryJump(); // On a fresh Space press, asks TryJump to perform a jump if its state checks permit it.
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) TryHeavyAttack(); // If a mouse exists and right-click was just pressed, attempts a heavy attack.
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) TryLightAttack(); // Otherwise, if left-click was just pressed, attempts a light attack; right-click has priority when both arrive together.

        Vector2 input = Vector2.zero; // Starts the two-axis movement input at zero, meaning no direction.
        if (keyboard.wKey.isPressed) input.y += 1f; // Holding W adds forward input on the input vector's Y axis.
        if (keyboard.sKey.isPressed) input.y -= 1f; // Holding S subtracts forward input, producing backward movement.
        if (keyboard.dKey.isPressed) input.x += 1f; // Holding D adds rightward input on the input vector's X axis.
        if (keyboard.aKey.isPressed) input.x -= 1f; // Holding A subtracts rightward input, producing leftward movement.
        // Keep diagonal movement from being faster than moving straight ahead.
        input = Vector2.ClampMagnitude(input, 1f); // Limits the input vector's length to one so pressing two directions does not increase movement speed.

        // After exhausting stamina, recover to 25% before sprinting can start again.
        if (exhausted && stamina >= maxStamina * 0.25f) // Checks whether an exhausted player has recovered at least one quarter of maximum stamina.
            exhausted = false; // Removes the exhaustion restriction so sprinting may start again.
        bool wantsSprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed; // Treats either Shift key as a request to sprint; || means OR.
        bool running = wantsSprint && !exhausted && stamina > 0f && input.sqrMagnitude > 0.01f && !attacking; // Allows sprinting only with Shift, available stamina, movement input, no exhaustion, and no active attack.
        if (running) // Chooses the stamina-draining branch when sprinting is allowed this frame.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            stamina = Mathf.Max(0f, stamina - sprintStaminaPerSecond * Time.deltaTime); // Spends stamina according to frame duration and clamps the result so it cannot drop below zero.
            nextStaminaRecoveryTime = Time.time + 0.65f; // Postpones stamina recovery until at least 0.65 game seconds after this sprinting frame.
            if (stamina <= 0f) // Checks whether sprinting has emptied the stamina pool.
            { // Begins this block; the statements inside belong to the class, method, or condition above.
                running = false; // Stops using sprint speed when stamina runs out.
                exhausted = true; // Requires recovery to the exhaustion threshold before another sprint can begin.
            } // Ends the block opened by the matching opening brace.
        } // Ends the block opened by the matching opening brace.
        else if (Time.time >= nextStaminaRecoveryTime) // When not sprinting, recovers stamina only after the recovery deadline has passed.
            stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * Time.deltaTime); // Adds this frame's stamina recovery and caps the result at maximum stamina.
        float moveSpeed = running ? runSpeed : walkSpeed; // Chooses sprint or walking speed with a conditional expression: condition ? trueValue : falseValue.
        if (attacking) // Checks whether movement should be slowed by an active attack.
            moveSpeed *= heavyAttacking ? .15f : AttackMovementMultiplier; // Uses 15 percent movement speed for heavy attacks or 40 percent for light attacks; *= multiplies the current value.
        // Flatten the camera directions so WASD follows the view without moving into the air.
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward; // Uses the camera's forward direction, falling back to world forward if the cached camera is missing.
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right; // Uses the camera's right direction, falling back to world right if the cached camera is missing.
        forward.y = 0f; // Removes the forward vector's vertical part so camera tilt does not directly move the player upward.
        right.y = 0f; // Removes the right vector's vertical part for horizontal movement.
        forward.Normalize(); // Normalizes the flattened forward vector to length one when it has a nonzero length.
        right.Normalize(); // Normalizes the flattened right vector so its length does not scale sideways speed.
        Vector3 direction = forward * input.y + right * input.x; // Combines forward/back and sideways input into a direction relative to the camera.

        // Q buys a short grounded dash with stamina. Use facing direction if no movement key is held.
        if (keyboard.qKey.wasPressedThisFrame && IsGrounded && !attacking && dashRemaining <= 0f && stamina >= dashStaminaCost) // Starts a dash only on a fresh Q press while grounded, not attacking or already dashing, and with enough stamina.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            dashDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward; // Chooses the normalized movement direction for the dash, or the player's facing direction if no movement is requested.
            dashRemaining = dashDuration; // Starts the dash timer with the configured duration.
            stamina -= dashStaminaCost; // Pays the dash's stamina cost once when the dash begins.
            nextStaminaRecoveryTime = Time.time + 0.8f; // Delays stamina recovery for 0.8 game seconds after starting the dash.
            if (LevelPresentation.Instance != null) // Checks whether the shared sound-and-effects manager is available.
            { // Begins this block; the statements inside belong to the class, method, or condition above.
                LevelPresentation.Instance.Play(LevelSound.Dash, transform.position, .8f); // Requests a dash sound at the player's position with a 0.8 volume multiplier.
                LevelPresentation.Instance.Burst(transform.position + Vector3.up * .3f, // Starts a particle-burst request positioned 0.3 units above the player; arguments continue on the next line.
                    new Color(.35f, .8f, 1f, .55f), 25, 2f, .14f); // Chooses a pale-blue color, 25 particles, speed scale 2, and size scale 0.14 for the dash burst.
            } // Ends the block opened by the matching opening brace.
        } // Ends the block opened by the matching opening brace.
        if (dashRemaining > 0f) // Checks whether there is still time left in the dash.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            dashRemaining = Mathf.Max(0f, dashRemaining - Time.deltaTime); // Subtracts this frame's duration from the dash timer without letting it become negative.
            direction = dashDirection; // Overrides normal movement direction with the direction saved at dash start.
            moveSpeed = dashSpeed; // Overrides walking/sprinting speed with the configured dash speed for this frame.
        } // Ends the block opened by the matching opening brace.

        if (direction.sqrMagnitude > 0.001f && !heavyAttacking) // Turns toward movement only for a meaningful direction and while not locked into a heavy attack.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            Quaternion targetRotation = Quaternion.LookRotation(direction); // Creates a target rotation that faces the chosen movement direction.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); // Smoothly blends the current rotation toward the target using rotationSpeed multiplied by frame duration.
        } // Ends the block opened by the matching opening brace.

        // CharacterController does not apply gravity automatically; keep track of vertical speed here.
        if (controller.isGrounded && verticalVelocity < 0f) // Checks for ground contact while the vertical velocity is downward.
            verticalVelocity = -2f; // Uses a small downward speed to help maintain contact with the ground.
        verticalVelocity += gravity * Time.deltaTime; // Applies gravity over this frame by adding acceleration multiplied by elapsed frame time.

        CollisionFlags collisions = controller.Move((direction * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime); // Combines horizontal and vertical velocity, converts it to frame distance, and moves with collision detection.
        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f; // Uses a bitwise flag check for a ceiling collision and cancels upward velocity if the player hit something above.
        bool grounded = IsGrounded; // Reads the grounded state again after movement and collision resolution.
        animator.SetBool("Grounded", grounded); // Sends the updated ground-contact state to the Animator's Grounded parameter.
        if (!wasGrounded && grounded && LevelPresentation.Instance != null) // Detects landing this frame and requires an effects manager before playing landing feedback.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            LevelPresentation.Instance.Play(LevelSound.Step_3, transform.position, .65f); // Requests a landing footstep sound with a 0.65 volume multiplier.
            LevelPresentation.Instance.Burst(transform.position, new Color(.7f, .61f, .45f, .4f), 14, 1.1f, .14f); // Requests 14 dust-colored landing particles with speed scale 1.1 and size scale 0.14.
        } // Ends the block opened by the matching opening brace.
        // Send idle/walk/run values to the Animator; airborne and attacking states use their own animations.
        float animationSpeed = !grounded || attacking || input.sqrMagnitude < 0.01f ? 0f : running ? 1f : 0.4f; // Selects animation speed: zero when airborne, attacking, or idle; otherwise one for sprinting or 0.4 for walking.
        animator.SetFloat("Speed", animationSpeed, 0.12f, Time.deltaTime); // Updates the Animator's Speed parameter with a 0.12-second damping value for smoother transitions.
    } // Ends the block opened by the matching opening brace.

    // Called by Space input. Return true only when a jump is allowed and its upward velocity has been set.
    // The short coyote-time window permits jumping just after leaving a ledge; jumpConsumed prevents a second jump.
    public bool TryJump() // Attempts a jump and returns a bool telling the caller whether the jump was accepted.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (GameplayBlocked || attacking || dashRemaining > 0f || jumpConsumed || // Starts the rejection check for blocked gameplay, attacks, an active dash, or a previously consumed jump.
            (!IsGrounded && Time.time - lastGroundedTime > CoyoteTime)) return false; // Also rejects jumping when off the ground beyond the coyote-time window; false means no jump was started.
        jumpConsumed = true; // Uses up this jump allowance until grounded bookkeeping makes another jump available.
        lastGroundedTime = float.NegativeInfinity; // Clears the remembered grounded time so this jump cannot reuse the same coyote-time allowance.
        // From v^2 = 2gh: choose the initial upward speed needed to reach the configured jump height.
        verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight); // Calculates the upward launch speed from jump height and gravity using the square root of 2 times gravity magnitude times height.
        animator.SetBool("Grounded", false); // Immediately tells the Animator the player is no longer grounded.
        animator.SetTrigger("Jump"); // Sets the Animator's Jump trigger to request the jump animation.
        if (LevelPresentation.Instance != null) // Checks for the shared effects manager before requesting jump feedback.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            LevelPresentation.Instance.Play(LevelSound.Step_1, transform.position, .4f); // Requests a quiet takeoff sound using Step_1 with a 0.4 volume multiplier.
            LevelPresentation.Instance.Burst(transform.position, new Color(.7f, .61f, .45f, .3f), 8, .7f, .12f); // Requests eight dust-colored takeoff particles with speed scale 0.7 and size scale 0.12.
        } // Ends the block opened by the matching opening brace.
        return true; // Reports that the jump was accepted and its upward velocity has been set.
    } // Ends the block opened by the matching opening brace.

    // Called by right-click input. Check grounded/action/stamina requirements, pay the stamina cost, and start the heavy attack.
    // Return false if blocked; a successful heavy attack clears the light-combo queue.
    public bool TryHeavyAttack() // Attempts a heavy attack and reports whether its requirements were met.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (GameplayBlocked || !IsGrounded || attacking || dashRemaining > 0f || stamina < heavyStaminaCost) return false; // Rejects the attack if gameplay is blocked, airborne, already attacking, dashing, or short of stamina.
        stamina -= heavyStaminaCost; // Spends the heavy attack's stamina cost once the attack has been accepted.
        nextStaminaRecoveryTime = Time.time + HeavyDuration + .35f; // Prevents stamina recovery until the heavy attack's duration plus another 0.35 seconds has passed.
        queuedComboClicks = 0; // Discards queued light-attack clicks when beginning a heavy attack.
        comboStep = 0; // Resets the light-attack combo counter.
        StartCoroutine(PlayHeavyAttack()); // Starts the coroutine that animates the heavy attack, waits for impact, and handles recovery.
        return true; // Reports that the heavy attack started; this does not guarantee that its later hit check finds an enemy.
    } // Ends the block opened by the matching opening brace.

    // Called by left-click input. Start the next light attack or queue clicks while a light attack is already playing.
    // Return false when blocked; true means the input was accepted, not that an enemy was hit.
    public bool TryLightAttack() // Attempts a light attack or queues another combo click, returning whether the input was accepted.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (GameplayBlocked || !IsGrounded || heavyAttacking || dashRemaining > 0f) return false; // Rejects light attacks during blocked gameplay, airborne movement, heavy attacks, or an active dash.
        if (attacking) // Checks whether a light attack is already running and the click should be queued instead.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            queuedComboClicks = Mathf.Min(queuedComboClicks + 1, 3 - comboStep); // Adds one queued click but caps the queue at the number of steps remaining in the three-step combo.
            return true; // Reports acceptance without starting a second attack coroutine while the current strike runs.
        } // Ends the block opened by the matching opening brace.

        if (Time.time > comboResetTime || comboStep >= 3) // Checks whether the combo's continuation window expired or the third step was already reached.
            comboStep = 0; // Resets the combo counter so the next increment starts at step one.
        comboStep++; // Increases the combo step by one; ++ is shorthand for adding one.
        StartCoroutine(PlayComboStep(comboStep)); // Starts the light-attack coroutine for the chosen numbered step.
        return true; // Reports that the light attack input was accepted.
    } // Ends the block opened by the matching opening brace.

    // Clear pending Animator attack triggers so an old trigger does not start the wrong animation.
    private void ResetAttackTriggers() // Defines a helper that clears all pending attack-animation triggers.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        animator.ResetTrigger("Attack1");animator.ResetTrigger("Attack2");animator.ResetTrigger("Attack3"); // Clears Attack1, Attack2, and Attack3 so an old light-attack trigger cannot fire later.
        animator.ResetTrigger("HeavyAttack"); // Clears any pending HeavyAttack trigger as well.
    } // Ends the block opened by the matching opening brace.

    // Coroutine started by TryHeavyAttack: animate, wait for the impact, apply stronger damage, then recover.
    // WaitForSeconds spreads the action over game time without blocking the frame; pausing also pauses these waits.
    private IEnumerator PlayHeavyAttack() // Defines a coroutine for the heavy attack; IEnumerator permits yield statements that wait across frames.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        attacking = heavyAttacking = true; // Sets both the general attacking flag and the specific heavy-attacking flag to true.
        ResetAttackTriggers();animator.SetTrigger("HeavyAttack"); // Clears old attack triggers, then requests the HeavyAttack animation.
        yield return new WaitForSeconds(.24f); // Waits 0.24 scaled game seconds before showing the heavy swing feedback.
        if (!dead && LevelPresentation.Instance != null) LevelPresentation.Instance.HeavySwing(transform); // If still alive and an effects manager exists, requests the heavy swing sound and sword trails.
        yield return new WaitForSeconds(HeavyImpactDelay - .24f); // Waits the remaining time until the configured heavy-impact moment, subtracting the time already waited.
        if (!dead && IsGrounded) // Applies the strike only if the player survived and is still grounded at impact.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            bool hit = ApplyAttackDamage(attackDamage * heavyDamageMultiplier, attackReach + .4f, 1.35f); // Checks a larger hit sphere with multiplied damage and extra forward reach, saving whether any living enemy was hit.
            if (hit && LevelPresentation.Instance != null) // Requests an impact ring only if an enemy was hit and the effects manager exists.
                LevelPresentation.Instance.Ring(transform.position + transform.forward * 1.3f, new Color(1f, .65f, .25f, .55f), 1.1f); // Shows a warm-colored ring 1.3 units in front of the player with radius 1.1.
        } // Ends the block opened by the matching opening brace.
        yield return new WaitForSeconds(HeavyDuration - HeavyImpactDelay); // Waits for the heavy attack's remaining recovery time after impact.
        attacking = heavyAttacking = false; // Clears both attack flags so normal actions can resume.
        comboResetTime = 0f; // Expires the light-combo continuation deadline after finishing the heavy attack.
    } // Ends the block opened by the matching opening brace.

    // Coroutine for one light-combo step: animate, wait for the strike, deal damage, and wait for recovery.
    // Then consume a queued click to start another step, or leave the combo ready until its reset deadline.
    private IEnumerator PlayComboStep(int step) // Defines the coroutine for one light attack, with step choosing its numbered animation.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        attacking = true; // Marks a light attack active, enabling movement restrictions and combo queuing.
        string trigger = "Attack" + step; // Builds an animation trigger name such as Attack1 by appending the step number.
        ResetAttackTriggers(); // Clears previously queued attack-animation triggers.
        animator.SetTrigger(trigger); // Requests the animation for the current combo step.
        if (LevelPresentation.Instance != null) // Checks that the shared effects manager exists before requesting swing feedback.
            LevelPresentation.Instance.Swing(transform, step, true); // Requests the step's swing sound and player sword trails; true identifies the actor as the player.

        yield return new WaitForSeconds(AttackImpactDelay); // Waits until the light attack's configured impact time using scaled game seconds.
        if (!dead && IsGrounded) // Only checks for hits if the player is still alive and grounded.
            ApplyAttackDamage(attackDamage, attackReach, 1.2f); // Applies normal attack damage using the configured forward reach and a hit-sphere radius of 1.2.

        yield return new WaitForSeconds(AttackDuration - AttackImpactDelay); // Waits out the remaining light-attack recovery time after the impact moment.
        attacking = false; // Marks the current light attack finished.
        comboResetTime = Time.time + ComboResetDelay; // Sets a new deadline for continuing this combo with another light attack.

        if (!dead && IsGrounded && queuedComboClicks > 0) // Automatically continues only while alive, grounded, and holding at least one queued combo click.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            queuedComboClicks--; // Consumes one queued click; -- subtracts one.
            comboStep = comboStep >= 3 ? 1 : comboStep + 1; // Advances to the next step, wrapping back to step one if the current step is already three.
            StartCoroutine(PlayComboStep(comboStep)); // Starts the coroutine for the next accepted combo step.
        } // Ends the block opened by the matching opening brace.
        else queuedComboClicks = 0; // If continuation is not allowed, discards any remaining queued clicks.
    } // Ends the block opened by the matching opening brace.

    // Check a sphere in front of the player and damage each living enemy found; return whether any enemy was hit.
    // The HashSet prevents one enemy with several colliders from receiving damage more than once per strike.
    private bool ApplyAttackDamage(float damage, float reach, float radius) // Defines the hit-check helper: damage is health loss, reach is sphere-center offset, and radius is sphere size.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        damagedEnemies.Clear(); // Clears the previous strike's enemy set so each enemy may be hit once by this new strike.
        Vector3 center = transform.position + Vector3.up * .65f + transform.forward * reach; // Places the hit sphere 0.65 units above the player and reach units in the player's facing direction.
        foreach (Collider hit in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore)) // Loops through overlapping colliders on every layer (~0), excluding trigger colliders from the hit query.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            IslandEnemyAI enemy = hit.GetComponentInParent<IslandEnemyAI>(); // Looks for enemy AI on the hit collider's object or one of its parents.
            if (enemy != null && !enemy.IsDead && damagedEnemies.Add(enemy)) // Requires a living enemy and a successful first addition to the set, preventing duplicate damage from multiple colliders.
                enemy.TakeDamage(damage); // Passes the requested damage amount to that enemy's health logic.
        } // Ends the block opened by the matching opening brace.
        return damagedEnemies.Count > 0; // Returns true if the strike found at least one eligible enemy; otherwise returns false.
    } // Ends the block opened by the matching opening brace.

    // Raycast downward to place the feet above the floor and size the CharacterController using the model's bones.
    // Temporarily disable the controller while repositioning, then reset the grounded/jump state.
    private void SnapToGround() // Defines the helper that places the player on the floor and adjusts the collision capsule.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        bool wasEnabled = controller.enabled; // Remembers whether the CharacterController was enabled before adjusting position and shape.
        controller.enabled = false; // Temporarily disables the controller while repositioning the player.

        Vector3 rayOrigin = transform.position + Vector3.up * 8f; // Starts the ground-detection ray eight world units above the player.
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore)) // Casts downward up to 20 units on all layers, ignoring triggers; out stores the collision details in hit.
        { // Begins this block; the statements inside belong to the class, method, or condition above.
            float feetY = transform.position.y; // Starts with the object's Y position as a fallback estimate of foot height.
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftToes) ?? animator.GetBoneTransform(HumanBodyBones.LeftFoot); // Tries the left toe bone first, falling back to the left foot bone when the first result is null; ?? supplies the fallback.
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightToes) ?? animator.GetBoneTransform(HumanBodyBones.RightFoot); // Finds the right toe bone or, if missing, the right foot bone.
            if (leftFoot != null && rightFoot != null) // Uses the foot-bone approach only if both left and right references exist.
                feetY = Mathf.Min(leftFoot.position.y, rightFoot.position.y); // Uses the lower of the two foot positions as the estimated foot height.
            else // Uses renderer bounds instead when suitable foot-bone references were not found.
            { // Begins this block; the statements inside belong to the class, method, or condition above.
                Renderer[] renderers = GetComponentsInChildren<Renderer>(); // Collects renderers on the player object and its active children to estimate the bottom of the visible model.
                if (renderers.Length > 0) // Checks that at least one renderer was found before reading the first array element.
                { // Begins this block; the statements inside belong to the class, method, or condition above.
                    feetY = renderers[0].bounds.min.y; // Starts the fallback foot height at the bottom of the first renderer's world-space bounding box.
                    for (int i = 1; i < renderers.Length; i++) // Loops through the remaining renderers, starting after the first one already examined.
                        feetY = Mathf.Min(feetY, renderers[i].bounds.min.y); // Keeps the lowest bottom edge found among those renderers.
                } // Ends the block opened by the matching opening brace.
            } // Ends the block opened by the matching opening brace.

            float feetOffset = feetY - transform.position.y; // Measures the estimated feet's vertical offset from the player's object origin before moving it.
            transform.position += Vector3.up * (hit.point.y + 0.025f - feetY); // Moves the player vertically so the estimated feet sit 0.025 units above the raycast's ground hit.

            Transform head = animator.GetBoneTransform(HumanBodyBones.Head); // Looks up the humanoid head bone to help estimate capsule height.
            float characterHeight = head != null ? head.position.y - feetY + 0.3f : 1.85f; // Estimates height from the current head position and the earlier feet height plus 0.3; falls back to 1.85 if no head exists.
            controller.height = Mathf.Max(1f, characterHeight); // Sets the controller capsule's height to the estimate, with a minimum of one unit.
            controller.center = new Vector3(0f, feetOffset + controller.height * 0.5f, 0f); // Places the capsule's center half its height above the saved foot offset, keeping it centered horizontally.
        } // Ends the block opened by the matching opening brace.

        controller.enabled = wasEnabled; // Restores the controller's enabled state to what it was before this helper ran.
        verticalVelocity = -2f; // Resets vertical speed to a small downward value for ground contact.
        jumpConsumed = false; // Restores the player's jump allowance.
        lastGroundedTime = Time.time; // Records the current time as the latest grounded time.
        animator.SetBool("Grounded", true); // Tells the Animator to use the grounded state after repositioning.
    } // Ends the block opened by the matching opening brace.

    // Called by enemies and hazards. Reduce health unless dead, dashing, in dialogue, or given non-positive damage.
    // At zero health, stop attacks, play death, and disable movement; IslandGameSession handles lives.
    public void TakeDamage(float amount) // Exposes the method enemies and hazards call with their requested damage amount.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (dead || dashRemaining > 0f || amount <= 0f || // Starts a damage-rejection check for an already dead player, an active dash, or a non-positive amount.
            (IslandGameSession.Instance != null && IslandGameSession.Instance.IsInDialogue)) // Also rejects damage while an existing session reports dialogue is running.
            return; // Ends the damage request if any immunity or invalid-amount check above passed.

        health = Mathf.Max(0f, health - amount); // Subtracts damage from current health and clamps the result to zero or above.
        if (LevelPresentation.Instance != null) // Checks for the shared effects manager before displaying hit feedback.
            LevelPresentation.Instance.Hit(transform, true, health <= 0f); // Requests player hit feedback, passing whether this hit reduced health to zero.
        if (health > 0f) // Checks whether the player survived with positive health.
            return; // Finishes damage handling without running the death sequence when the player survived.

        dead = true; // Marks the player dead so input and later damage checks recognize the death.
        attacking = false; // Clears the general attacking state.
        heavyAttacking = false; // Clears the heavy-attack state.
        queuedComboClicks = 0; // Discards queued light-attack clicks.
        StopAllCoroutines(); // Stops all coroutines on this controller, cancelling pending attack impacts and recoveries.
        animator.SetFloat("Speed", 0f); // Sets the Animator's movement-speed parameter to zero.
        ResetAttackTriggers();animator.ResetTrigger("Jump"); // Clears pending attack triggers and the pending Jump trigger before requesting death.
        animator.SetTrigger("Die"); // Sets the Animator's Die trigger to request the death animation.
        controller.enabled = false; // Disables the CharacterController after death; session code later handles lives and respawning.
    } // Ends the block opened by the matching opening brace.

    // Called by a healing pickup. Restore maximum health and return true only if the player was alive and needed healing.
    public bool HealToFull() // Attempts full healing and returns whether any health was actually restored.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (dead || health >= maxHealth) // Checks whether the player is dead or already has maximum health.
            return false; // Reports no healing so a pickup can remain available instead of being wasted.

        health = maxHealth; // Refills current health to the configured maximum.
        return true; // Reports successful healing to the caller.
    } // Ends the block opened by the matching opening brace.

    // Called by the session after a lost life. Move to the checkpoint, refill health/stamina, and clear active actions.
    // Reset the Animator and ground position so the player can control the character again.
    public void RespawnAt(Vector3 position) // Exposes the method the game session calls to respawn the player at a supplied world position.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        StopAllCoroutines(); // Cancels any attack coroutine still associated with the previous player state.
        controller.enabled = false; // Disables the controller before teleporting the player.
        transform.position = position; // Moves the player's object to the requested checkpoint or spawn position.
        health = maxHealth; // Restores full health for the new life.
        stamina = maxStamina; // Restores full stamina for the new life.
        dead = false; // Clears the death flag so gameplay input can be accepted again.
        exhausted = false; // Removes the sprint-exhaustion restriction.
        attacking = false; // Clears the general attack flag.
        heavyAttacking = false; // Clears the heavy-attack flag.
        dashRemaining = 0f; // Cancels any remaining dash time.
        queuedComboClicks = 0; // Discards queued combo inputs from before respawning.
        comboStep = 0; // Resets the light-combo step counter.
        animator.Rebind(); // Rebinds the Animator's animated properties and resets it for the respawned character.
        animator.Update(0f); // Evaluates the Animator immediately with zero elapsed time to apply its reset state.
        controller.enabled = true; // Enables the CharacterController again for the living player.
        SnapToGround(); // Adjusts the respawn position to the ground and restores ground/jump bookkeeping.
        if (LevelPresentation.Instance != null) // Checks for the shared effects manager before requesting respawn feedback.
            LevelPresentation.Instance.Reward(LevelSound.Checkpoint, transform.position, new Color(.25f, .85f, 1f, .7f)); // Requests the checkpoint reward sound and blue particles/ring at the respawn position.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
