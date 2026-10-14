// Gently pans and zooms the menu painting while keeping it large enough to cover the viewport.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine;
using UnityEngine.UI;

/// <summary>Unscaled, seamless camera drift over the original menu painting.</summary>
[RequireComponent(typeof(Image))]
public sealed class MenuBackdropMotion : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float motionAmount = 1f;
    private RectTransform artwork;
    private Image painting;
    private float elapsed;

    // Cache the background UI references when this component loads.
    private void Awake() { Cache(); }
    // Refresh references and apply the current animation pose whenever the background becomes enabled.
    private void OnEnable() { Cache(); EvaluateAt(elapsed); }
    // Advance the animation with unscaled time so menu motion continues even if gameplay time is paused.
    private void Update() { elapsed += Time.unscaledDeltaTime; EvaluateAt(elapsed); }

    // Get the RectTransform and Image, and disable image raycasts so the artwork does not intercept menu input.
    private void Cache()
    {
        artwork = (RectTransform)transform;
        painting = GetComponent<Image>();
        painting.raycastTarget = false;
    }

    // Calculate background size and gentle drift for a supplied time, using extra image coverage to avoid exposed edges.
    // Taking an explicit time also lets preview tools reproduce the same pose.
    public void EvaluateAt(float seconds)
    {
        if (artwork == null) Cache();
        var viewport = artwork.parent as RectTransform;
        if (viewport == null || painting.sprite == null) return;
        Vector2 size = viewport.rect.size;
        float aspect = painting.sprite.rect.width / painting.sprite.rect.height;
        float height = Mathf.Max(size.y, size.x / aspect);
        float zoom = 1.08f + .016f * Mathf.Sin(seconds * .11f);
        Vector2 cover = new Vector2(height * aspect, height) * zoom;
        artwork.anchorMin = artwork.anchorMax = new Vector2(.5f, .5f);
        artwork.sizeDelta = cover;
        // Overscan is larger than the travel range, including on ultrawide displays.
        Vector2 travel = (cover - size) * .32f * motionAmount;
        artwork.anchoredPosition = new Vector2(Mathf.Sin(seconds * .085f) * travel.x,
            Mathf.Sin(seconds * .12f + .8f) * travel.y);
        artwork.localScale = Vector3.one;
    }
}
