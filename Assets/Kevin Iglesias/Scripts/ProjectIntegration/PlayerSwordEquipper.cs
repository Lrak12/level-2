// Loads both sword prefabs and aligns their grips with the animated hands; attack damage lives in the player controller.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Animator))]
public sealed class PlayerSwordEquipper : MonoBehaviour
{
    private const string RightSwordResourcePath = "Player/TribalSword";
    private const string LeftSwordResourcePath = "Player/TribalSwordLeft";
    private const string RightSwordName = "PlayerSword_Right";
    private const string LeftSwordName = "PlayerSword_Left";
    private const string PreviousSwordName = "PlayerSword_OneHanded";
    private const string LegacySwordName = "PlayerSword";
    private const float SwordScale = 0.30f;
    // Mesh-space centers of the handles, below each crossguard (blade axis is +Y).
    private const float RightHandleCenter = -0.30f;
    private const float LeftHandleCenter = -0.15f;

    private Animator animator;
    private Transform rightSword;
    private Transform leftSword;
    private Pose rightGrip;
    private Pose leftGrip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Unity runs this after a scene loads. Add the equipment component to the player if needed and equip both swords.
    private static void EquipRuntimePlayer()
    {
        TribalPlayerController player = FindFirstObjectByType<TribalPlayerController>();
        if (player == null)
            return;

        PlayerSwordEquipper equipper = player.GetComponent<PlayerSwordEquipper>();
        if (equipper == null)
            equipper = player.gameObject.AddComponent<PlayerSwordEquipper>();
        equipper.Equip();
    }

    // Cache the Animator so later functions can access the humanoid hand and finger bones.
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Equip both swords before the first regular frame update.
    private void Start()
    {
        Equip();
    }

    // After animation updates, reapply the saved local grip poses so both swords stay aligned with their hands.
    private void LateUpdate()
    {
        if (animator == null || rightSword == null || leftSword == null)
            return;

        // A rigid hand socket follows the full animated wrist rotation. Blending world
        // rotations here made the sword lag and point away from the fist during swings.
        rightSword.SetLocalPositionAndRotation(rightGrip.position, rightGrip.rotation);
        leftSword.SetLocalPositionAndRotation(leftGrip.position, leftGrip.rotation);
    }

    // Find both humanoid hands, remove obsolete sword objects, calculate grip poses, and create or reuse the current swords.
    public void Equip()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
            return;

        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        if (rightHand == null || leftHand == null)
            return;

        RemoveSword(rightHand, LegacySwordName);
        RemoveSword(rightHand, PreviousSwordName);
        RemoveSword(rightHand, LeftSwordName);
        RemoveSword(leftHand, LegacySwordName);
        RemoveSword(leftHand, PreviousSwordName);
        RemoveSword(leftHand, RightSwordName);

        rightGrip = BuildGrip(rightHand, false, RightHandleCenter);
        leftGrip = BuildGrip(leftHand, true, LeftHandleCenter);
        rightSword = EquipSword(rightHand, RightSwordResourcePath, RightSwordName, rightGrip);
        leftSword = EquipSword(leftHand, LeftSwordResourcePath, LeftSwordName, leftGrip);
    }

    // Use hand/finger positions to calculate a local sword position and rotation that place the handle in the palm.
    // Return that Pose so the sword can follow the animated hand without a world-space rotation lag.
    private Pose BuildGrip(Transform hand, bool left, float handleCenter)
    {
        Transform index = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
        Transform little = animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
        Transform middle = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
        Vector3 palmForward = middle != null ? hand.InverseTransformPoint(middle.position).normalized : Vector3.up;
        Vector3 bladeAxis = left ? Vector3.right : Vector3.left;
        Vector3 palmCenter = palmForward * .045f;
        if (index != null && little != null)
        {
            Vector3 indexPoint = hand.InverseTransformPoint(index.position);
            Vector3 littlePoint = hand.InverseTransformPoint(little.position);
            // Out through the thumb side of the closed fist, mirrored for each hand.
            bladeAxis = Vector3.ProjectOnPlane(indexPoint - littlePoint, palmForward).normalized;
            palmCenter = (indexPoint + littlePoint) * .35f;
        }
        Vector3 bladeNormal = Vector3.Cross(bladeAxis, palmForward).normalized;
        Quaternion rotation = Quaternion.LookRotation(bladeNormal, bladeAxis);
        return new Pose(palmCenter - rotation * (Vector3.up * handleCenter * SwordScale), rotation);
    }

    // Reuse a sword already on this hand, or load its Resources prefab and attach a new instance at the grip pose.
    // New sword colliders are disabled because TribalPlayerController calculates melee hits with a sphere query.
    private Transform EquipSword(Transform hand, string resourcePath, string swordName, Pose grip)
    {
        Transform equippedSword = hand.Find(swordName);
        if (equippedSword != null)
        {
            equippedSword.SetLocalPositionAndRotation(grip.position, grip.rotation);
            equippedSword.localScale = Vector3.one * SwordScale;
            return equippedSword;
        }

        GameObject swordPrefab = Resources.Load<GameObject>(resourcePath);
        if (swordPrefab == null)
            return null;

        GameObject sword = Instantiate(swordPrefab, hand);
        sword.name = swordName;
        sword.transform.SetLocalPositionAndRotation(grip.position, grip.rotation);
        sword.transform.localScale = Vector3.one * SwordScale;

        foreach (Collider swordCollider in sword.GetComponentsInChildren<Collider>(true))
            swordCollider.enabled = false;

        return sword.transform;
    }

    // Remove a named old sword from a hand, using the appropriate destruction call for Play mode or editor setup.
    private void RemoveSword(Transform hand, string swordName)
    {
        Transform sword = hand.Find(swordName);
        if (sword == null)
            return;

        if (Application.isPlaying)
            Destroy(sword.gameObject);
        else
            DestroyImmediate(sword.gameObject);
    }
}
