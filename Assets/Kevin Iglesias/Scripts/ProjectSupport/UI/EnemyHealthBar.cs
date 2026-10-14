// Creates and updates a floating health bar above an enemy and turns it toward the camera.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHealthBar : MonoBehaviour
{
    private const float BarWorldWidth = 1.55f;
    private const float BarWorldHeight = 0.16f;
    private const float HeightPadding = 0.28f;

    private static readonly Color HealthyColor = new Color(0.2f, 0.86f, 0.25f, 1f);
    private static readonly Color HurtColor = new Color(0.95f, 0.16f, 0.1f, 1f);
    private static readonly Color BackgroundColor = new Color(0.035f, 0.035f, 0.035f, 0.88f);

    private IslandEnemyAI enemy;
    private RectTransform canvasRect;
    private RectTransform fillRect;
    private UnityEngine.UI.Image fillImage;
    private Camera targetCamera;

    // Called by enemy setup. Store the owner, create the bar if needed, place it above the enemy, and display health.
    public void Initialize(IslandEnemyAI owner)
    {
        enemy = owner;
        if (canvasRect == null)
            BuildBar();
        PositionAboveEnemy();
        Refresh();
    }

    // Provide initial bar setup if an enemy did not explicitly call Initialize before the first frame.
    private void Start()
    {
        if (enemy == null)
            enemy = GetComponent<IslandEnemyAI>();
        if (canvasRect == null)
            BuildBar();
        PositionAboveEnemy();
        Refresh();
    }

    // After normal updates, rotate the world-space bar to face the main camera and refresh its health display.
    private void LateUpdate()
    {
        if (enemy == null || canvasRect == null)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera != null)
            canvasRect.rotation = targetCamera.transform.rotation;

        Refresh();
    }

    // Reuse a complete existing bar or create a world-space Canvas with background and fill images.
    // Disable image raycasts because this display is decorative and should not intercept UI input.
    private void BuildBar()
    {
        Transform existing = transform.Find("EnemyHealthBar");
        if (existing != null)
        {
            canvasRect = existing as RectTransform;
            Transform existingFill = existing.Find("Fill");
            if (existingFill != null)
            {
                fillRect = existingFill as RectTransform;
                fillImage = existingFill.GetComponent<UnityEngine.UI.Image>();
            }
            if (canvasRect != null && fillRect != null && fillImage != null)
                return;
        }

        GameObject canvasObject = new GameObject("EnemyHealthBar", typeof(RectTransform), typeof(Canvas));
        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.SetParent(transform, false);
        canvasRect.sizeDelta = new Vector2(BarWorldWidth, BarWorldHeight);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        UnityEngine.UI.Image background = canvasObject.AddComponent<UnityEngine.UI.Image>();
        background.color = BackgroundColor;
        background.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.SetParent(canvasRect, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(0.035f, 0.035f);
        fillRect.offsetMax = new Vector2(-0.035f, -0.035f);

        fillImage = fillObject.GetComponent<UnityEngine.UI.Image>();
        fillImage.color = HealthyColor;
        fillImage.raycastTarget = false;
    }

    // Find the top of the enemy's rendered model and place the health bar above it with a small gap.
    private void PositionAboveEnemy()
    {
        if (canvasRect == null)
            return;

        float top = transform.position.y + 2f;
        foreach (Renderer enemyRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (enemyRenderer.transform.IsChildOf(canvasRect))
                continue;
            top = Mathf.Max(top, enemyRenderer.bounds.max.y);
        }

        Vector3 worldPosition = new Vector3(transform.position.x, top + HeightPadding, transform.position.z);
        canvasRect.localPosition = transform.InverseTransformPoint(worldPosition);
    }

    // Convert health to fill width and color, and hide the entire bar when its enemy is dead.
    private void Refresh()
    {
        if (enemy == null || fillRect == null || fillImage == null)
            return;

        float normalizedHealth = enemy.MaxHealth > 0f ? Mathf.Clamp01(enemy.Health / enemy.MaxHealth) : 0f;
        fillRect.gameObject.SetActive(normalizedHealth > 0f);
        fillRect.anchorMax = new Vector2(normalizedHealth, 1f);
        fillImage.color = Color.Lerp(HurtColor, HealthyColor, normalizedHealth);
        canvasRect.gameObject.SetActive(!enemy.IsDead);
    }
}
