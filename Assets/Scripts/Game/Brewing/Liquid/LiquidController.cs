using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Fake-liquid system. The body sprite is a 1x1 unit sprite with a BOTTOM pivot; we scale its
/// height by the current fill (0..1) so it "rises" from the cup bottom. A separate surface sprite
/// (the meniscus / highlight ellipse) sits on top and is driven by <see cref="LiquidWobble"/>.
///
/// Both renderers use SpriteMaskInteraction.VisibleInsideMask, so the cup's SpriteMask clips them
/// to the inner cavity — no real fluid simulation needed. Colour is a volume-weighted blend of
/// every ingredient poured in, tweened with DOTween for a smooth, satisfying mix.
/// </summary>
public class LiquidController : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("1-unit tall sprite, BOTTOM pivot. Scaled in Y to fill the cup.")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Thin ellipse / highlight that rides the top of the liquid.")]
    [SerializeField] private SpriteRenderer surface;

    [Header("Cup cavity (local space of this transform)")]
    [Tooltip("Local Y of the cup's inner bottom.")]
    [SerializeField] private float bottomLocalY = -1.5f;
    [Tooltip("How tall the fillable cavity is, in local units.")]
    [SerializeField] private float cavityHeight = 3f;
    [Tooltip("Horizontal scale of the body sprite to cover the cup width.")]
    [SerializeField] private float bodyWidth = 2f;
    [Tooltip("Thickness (height) of the surface meniscus band, in local units.")]
    [SerializeField] private float surfaceThickness = 0.5f;

    [Header("Feel")]
    [Tooltip("Highest the liquid is allowed to rise, 0..1 of the cavity. <1 leaves headroom at the " +
             "top so it never reaches the rim (room for toppings / no overflow look).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float maxFill = 0.85f;
    [SerializeField] private float fillTweenDuration = 0.35f;
    [SerializeField] private Ease fillEase = Ease.OutCubic;
    [Tooltip("Starting tint before anything is poured (very light, near-empty look).")]
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0f);

    /// <summary>Current fill in 0..1.</summary>
    public float Fill { get; private set; }
    public bool IsEmpty => Fill <= 0.001f;
    public bool IsFull => Fill >= maxFill - 0.001f;
    public Color CurrentColor { get; private set; }

    /// <summary>Raised whenever the fill changes (immediate target value, not the tweened value).</summary>
    public event Action<float> OnFillChanged;

    private float totalVolume; // accumulated volume used for colour weighting
    private Tween fillTween;
    private Tween colorTween;

    private void Awake()
    {
        CurrentColor = emptyColor;
        ApplyFillInstant(0f);
        if (body != null) body.color = emptyColor;
        if (surface != null) surface.color = emptyColor;
    }

    /// <summary>Re-shape the cavity at runtime when the player swaps cup. Re-applies current fill.</summary>
    public void ConfigureCavity(float bottomY, float height, float width)
    {
        bottomLocalY = bottomY;
        cavityHeight = height;
        bodyWidth = width;
        ApplyFillInstant(Fill);
    }

    /// <summary>Local Y of the liquid surface for the current fill — used for topping buoyancy.</summary>
    public float SurfaceLocalY => bottomLocalY + Fill * cavityHeight;

    /// <summary>World position of the liquid surface centre.</summary>
    public Vector3 SurfaceWorldPosition =>
        transform.TransformPoint(new Vector3(0f, SurfaceLocalY, 0f));

    /// <summary>
    /// Pour ingredient in. <paramref name="volume"/> is 0..1 of the cup; <paramref name="color"/>
    /// is blended into the mix weighted by how much liquid is already present.
    /// </summary>
    public void AddLiquid(float volume, Color color)
    {
        // Cap at maxFill (not 1) so the drink stops below the rim.
        float newFill = Mathf.Clamp(Fill + volume, 0f, maxFill);
        float added = newFill - Fill;
        if (added <= 0f) return;

        // Volume-weighted colour blend so the first pour dominates, later pours nudge the hue.
        float newTotal = totalVolume + added;
        Color target = newTotal > 0f
            ? Color.Lerp(CurrentColor, color, added / newTotal)
            : color;
        totalVolume = newTotal;

        SetFill(newFill, target);
    }

    /// <summary>Drain liquid (used by the drinking phase). Colour is preserved.</summary>
    public void DrainLiquid(float volume)
    {
        SetFill(Mathf.Clamp01(Fill - volume), CurrentColor);
    }

    public void ResetLiquid()
    {
        fillTween?.Kill();
        colorTween?.Kill();
        totalVolume = 0f;
        CurrentColor = emptyColor;
        Fill = 0f;
        ApplyFillInstant(0f);
        if (body != null) body.color = emptyColor;
        if (surface != null) surface.color = emptyColor;
        OnFillChanged?.Invoke(0f);
    }

    private void SetFill(float newFill, Color targetColor)
    {
        Fill = newFill;
        CurrentColor = targetColor;
        OnFillChanged?.Invoke(Fill);

        fillTween?.Kill();
        fillTween = DOTween.To(GetTweenFill, ApplyFillInstant, newFill, fillTweenDuration)
                           .SetEase(fillEase);

        colorTween?.Kill();
        Color opaque = targetColor; opaque.a = Mathf.Max(targetColor.a, newFill > 0f ? 1f : 0f);
        if (body != null) colorTween = body.DOColor(opaque, fillTweenDuration);
        if (surface != null)
        {
            // Surface is a touch lighter than the body for a glossy meniscus look.
            Color top = Color.Lerp(opaque, Color.white, 0.25f);
            surface.DOColor(top, fillTweenDuration);
        }
    }

    private float tweenFillCache;
    private float GetTweenFill() => tweenFillCache;

    private void ApplyFillInstant(float f)
    {
        tweenFillCache = f;
        float h = f * cavityHeight;

        if (body != null)
        {
            body.transform.localScale = new Vector3(bodyWidth, Mathf.Max(h, 0.0001f), 1f);
            body.transform.localPosition = new Vector3(0f, bottomLocalY, 0f);
            body.enabled = f > 0.0001f;
        }
        if (surface != null)
        {
            surface.transform.localScale = new Vector3(bodyWidth, surfaceThickness, 1f);
            surface.transform.localPosition = new Vector3(0f, bottomLocalY + h, 0f);
            surface.enabled = f > 0.0001f;
        }
    }

    private void OnDestroy()
    {
        fillTween?.Kill();
        colorTween?.Kill();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualise the cavity range so designers can line it up with the cup sprite.
        Gizmos.color = Color.cyan;
        Vector3 bottom = transform.TransformPoint(new Vector3(0f, bottomLocalY, 0f));
        Vector3 top = transform.TransformPoint(new Vector3(0f, bottomLocalY + cavityHeight, 0f));
        Gizmos.DrawLine(bottom + Vector3.left, bottom + Vector3.right);
        Gizmos.DrawLine(top + Vector3.left, top + Vector3.right);
    }
#endif
}
