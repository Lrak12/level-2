// Reduces horizontal hip sway during running while keeping the animation's vertical movement.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Animator))]
public sealed class HumanoidSwayStabilizer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float runStabilization = 0.65f;

    private Animator animator;
    private Transform hips;
    private Vector3 referenceLocalPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Unity runs this after scene loading. Add stabilization to the active player and enemy components it finds.
    private static void AddToCombatants()
    {
        foreach (TribalPlayerController player in FindObjectsByType<TribalPlayerController>(FindObjectsSortMode.None))
            EnsureOn(player.gameObject);
        foreach (IslandEnemyAI enemy in FindObjectsByType<IslandEnemyAI>(FindObjectsSortMode.None))
            EnsureOn(enemy.gameObject);
    }

    // Attach one stabilizer to the supplied object only if it does not already have one.
    private static void EnsureOn(GameObject target)
    {
        if (target.GetComponent<HumanoidSwayStabilizer>() == null)
            target.AddComponent<HumanoidSwayStabilizer>();
    }

    // Cache the Animator and hip bone, then save the hips' initial local position as the stabilization reference.
    private void Awake()
    {
        animator = GetComponent<Animator>();
        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips != null)
            referenceLocalPosition = hips.localPosition;
    }

    // After animation, blend the hips' horizontal position toward the reference according to the running animation speed.
    private void LateUpdate()
    {
        if (hips == null)
            return;

        float runAmount = Mathf.InverseLerp(0.55f, 1f, animator.GetFloat("Speed"));
        float strength = runStabilization * runAmount;
        Vector3 animated = hips.localPosition;
        animated.x = Mathf.Lerp(animated.x, referenceLocalPosition.x, strength);
        animated.z = Mathf.Lerp(animated.z, referenceLocalPosition.z, strength);
        hips.localPosition = animated;
    }
}
