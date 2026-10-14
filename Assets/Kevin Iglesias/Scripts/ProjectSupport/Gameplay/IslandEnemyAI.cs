// Runs enemy patrol, chase, melee attacks, health, and death using a NavMeshAgent and Animator.
// Study guide and script map: CODE_GUIDE.md in the project root.

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
public sealed class IslandEnemyAI : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField, Min(1f)] private float patrolRadius = 8f;
    [SerializeField, Min(0f)] private float patrolPause = 1.5f;
    [SerializeField, Min(0.1f)] private float walkSpeed = 1.6f;
    [SerializeField, Min(0.1f)] private float runSpeed = 3.2f;

    [Header("Combat")]
    [SerializeField, Min(0f)] private float detectionRange = 12f;
    [SerializeField, Min(0f)] private float attackRange = 2.2f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 3.1f;
    [SerializeField, Min(0f)] private float attackDamage = 15f;
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private Transform target;

    private Animator animator;
    private NavMeshAgent agent;
    private Vector3 home;
    private float health;
    private float nextPatrolTime;
    private float nextAttackTime;
    private bool dead;
    private bool attacking;
    private const float AttackImpactDelay = 0.22f;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public bool IsDead => dead;

    // Set damage, attack cooldown, and maximum health with safe minimums, then refill the enemy's health.
    public void ConfigureCombat(float damage, float cooldown, float hitPoints)
    {
        attackDamage = Mathf.Max(0f, damage);
        attackCooldown = Mathf.Max(0.1f, cooldown);
        maxHealth = Mathf.Max(1f, hitPoints);
        health = maxHealth;
    }

    // Apply combat settings, then increase detection/attack ranges and movement speeds for a boss.
    public void ConfigureBossCombat(float damage, float cooldown, float hitPoints)
    {
        ConfigureCombat(damage, cooldown, hitPoints);
        detectionRange = 20f;
        attackRange = 2.8f;
        walkSpeed = 2.1f;
        runSpeed = 4.1f;
    }

    // Cache the Animator and NavMeshAgent, record the patrol home, initialize health/navigation, and attach a health bar.
    // The agent controls movement, so animation root motion is disabled.
    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        home = transform.position;
        health = maxHealth;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0.4f;
        agent.autoBraking = true;
        agent.updatePosition = true;
        agent.updateRotation = true;
        animator.applyRootMotion = false;

        EnemyHealthBar healthBar = GetComponent<EnemyHealthBar>();
        if (healthBar == null)
            healthBar = gameObject.AddComponent<EnemyHealthBar>();
        healthBar.Initialize(this);
    }

    // Before the first Update, align the enemy with the ground and place it on a nearby NavMesh position if possible.
    private void Start()
    {
        SnapToGround();
    }

    // Each frame, choose by player distance: attack nearby, chase within detection range, or patrol farther away.
    // Dead enemies, agents outside the NavMesh, and blocked gameplay skip this decision loop.
    private void Update()
    {
        if (dead || !agent.isOnNavMesh ||
            (IslandGameSession.Instance != null && IslandGameSession.Instance.BlocksGameplay))
            return;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        if (attacking)
        {
            animator.SetFloat("Speed", 0f);
            if (Time.time >= nextAttackTime)
                attacking = false;
            return;
        }

        float distance = target == null ? float.PositiveInfinity : Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange)
        {
            Attack();
        }
        else if (distance <= detectionRange)
        {
            agent.speed = runSpeed;
            agent.stoppingDistance = attackRange * 0.8f;
            agent.SetDestination(target.position);
        }
        else
        {
            Patrol();
        }

        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed < 0.1f ? 0f : speed >= (walkSpeed + runSpeed) * 0.5f ? 1f : 0.4f, 0.15f, Time.deltaTime);
    }

    // After reaching a destination and waiting, sample random points near home until a valid NavMesh destination is found.
    private void Patrol()
    {
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0.4f;
        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.3f || Time.time < nextPatrolTime)
            return;

        nextPatrolTime = Time.time + patrolPause + Random.Range(0f, 1.5f);
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            Vector3 point = home + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, agent.areaMask))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    // When the cooldown allows, clear the current path, face the player, play the attack, and schedule delayed damage.
    private void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        agent.isStopped = true;
        agent.ResetPath();
        Vector3 look = target.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(look);

        attacking = true;
        nextAttackTime = Time.time + attackCooldown;
        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("Attack");
        if (LevelPresentation.Instance != null)
            LevelPresentation.Instance.Swing(transform, 1, false);
        StartCoroutine(DealDamageAfterWindup(target));
        agent.isStopped = false;
    }

    // Wait until the swing reaches impact time, then damage the same target only if it is still close enough.
    // Recheck death and session state so an interrupted attack cannot apply its pending hit.
    private IEnumerator DealDamageAfterWindup(Transform attackTarget)
    {
        yield return new WaitForSeconds(AttackImpactDelay);

        if (dead || attackTarget == null || target != attackTarget ||
            (IslandGameSession.Instance != null && IslandGameSession.Instance.BlocksGameplay))
            yield break;

        Vector3 separation = attackTarget.position - transform.position;
        separation.y = 0f;
        if (separation.sqrMagnitude <= (attackRange + 0.3f) * (attackRange + 0.3f))
            attackTarget.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
    }

    // Assign the transform to chase and attack; the session uses this when activating the boss.
    public void SetTarget(Transform newTarget) => target = newTarget;

    // Align the model's feet using the agent offset, then move to a sampled NavMesh position or terrain height.
    // Convert world-space clearance to local units so scaled characters still stand above the floor.
    public void SnapToGround()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (agent == null)
            return;

        float feetOffset = 0f;
        if (animator != null && animator.isHuman)
        {
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftToes) ?? animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightToes) ?? animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot != null && rightFoot != null)
                feetOffset = Mathf.Min(transform.InverseTransformPoint(leftFoot.position).y,
                    transform.InverseTransformPoint(rightFoot.position).y);
        }

        agent.radius = 0.42f;
        agent.height = 1.8f;
        // NavMeshAgent offsets use local units. Using ankle height in world units
        // pushed the scaled guardian and warlord's soles underneath the floor.
        float scaleY = Mathf.Max(0.001f, transform.lossyScale.y);
        agent.baseOffset = 0.025f / scaleY - feetOffset;

        Vector3 samplePoint = transform.position;
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
            samplePoint.y = terrain.transform.position.y + terrain.SampleHeight(samplePoint);

        if (NavMesh.SamplePosition(samplePoint, out NavMeshHit navHit, 5f, agent.areaMask))
        {
            agent.Warp(navHit.position);
            home = transform.position;
        }
        else if (terrain != null)
        {
            Vector3 groundedPosition = transform.position;
            groundedPosition.y = samplePoint.y - feetOffset * scaleY + 0.025f;
            transform.position = groundedPosition;
            home = groundedPosition;
        }
    }

    // Reduce health unless damage is invalid or dialogue protects combatants; show hit feedback.
    // At zero health, stop navigation, animate death, and report the defeat to IslandGameSession once.
    public void TakeDamage(float amount)
    {
        if (dead || amount <= 0f || (IslandGameSession.Instance != null && IslandGameSession.Instance.IsInDialogue))
            return;

        health = Mathf.Max(0f, health - amount);
        if (LevelPresentation.Instance != null)
            LevelPresentation.Instance.Hit(transform, false, health <= 0f);
        if (health > 0f)
            return;

        dead = true;
        agent.isStopped = true;
        agent.ResetPath();
        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("Die");
        if (IslandGameSession.Instance != null)
            IslandGameSession.Instance.EnemyDefeated(this);
        enabled = false;
    }
}
