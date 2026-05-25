using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Button-driven pouring (no dragging). Attach to a UI Button / Image. HOLD the button to pour: a
/// stream flows from <see cref="pourOrigin"/> (place it at the top of the screen above the cup) down
/// into the cup, the liquid rises and tints, and the ASMR pour loop plays. RELEASE to stop.
///
/// A quick tap also pours a small fixed amount so single clicks feel responsive. The button's
/// GameObject must live under a Canvas that has a GraphicRaycaster, with an EventSystem in the scene
/// (your scene already has both).
/// </summary>
public class ButtonPour : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Target (screen-fill liquid)")]
    [Tooltip("Liquid to pour into (the screen-fill liquid).")]
    [SerializeField] private LiquidController liquid;
    [Tooltip("Optional sloshing for the liquid.")]
    [SerializeField] private LiquidWobble wobble;

    [Header("FX")]
    [SerializeField] private PourStream stream;
    [SerializeField] private BrewingAudio brewAudio;
    [Tooltip("WORLD-space empty at the top of the screen; the stream starts here. If empty, falls " +
             "back to a point above the liquid surface.")]
    [SerializeField] private Transform pourOrigin;

    [Header("Ingredient")]
    [Tooltip("Ingredient type id (e.g. \"tea\", \"milk\"). Used by recipe matching.")]
    [SerializeField] private string ingredientId;
    [SerializeField] private Color liquidColor = new Color(0.85f, 0.6f, 0.35f, 1f);
    [SerializeField] private float fillRatePerSecond = 0.5f;
    [Tooltip("Minimum amount a single quick tap adds, so taps feel responsive.")]
    [SerializeField] private float minPourPerTap = 0.06f;

    [Header("Fallback origin")]
    [Tooltip("Used only when pourOrigin is not assigned: height above the liquid surface.")]
    [SerializeField] private float fallbackHeight = 6f;

    /// <summary>The ingredient's type id, exposed for recipe matching.</summary>
    public string IngredientId => ingredientId;
    /// <summary>The liquid color for this ingredient — used by the recipe slot UI.</summary>
    public Color LiquidColor => liquidColor;
    /// <summary>Raised the moment this button starts pouring into the cup (passes its id).</summary>
    public event Action<string> OnPourStarted;

    private bool pouring;
    private bool interactable = true;
    private float pouredThisPress;

    /// <summary>The liquid this button pours into (the screen-fill liquid).</summary>
    private LiquidController Liquid => liquid;
    private LiquidWobble Wobble => wobble;

    /// <summary>Enable/disable pouring for this button (driven by BrewingManager per phase).</summary>
    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!value) EndPour();
    }

    private Vector3 OriginPosition
    {
        get
        {
            if (pourOrigin != null) return pourOrigin.position;
            var l = Liquid;
            if (l != null) return l.SurfaceWorldPosition + Vector3.up * fallbackHeight;
            return transform.position;
        }
    }

    public void OnPointerDown(PointerEventData _) => BeginPour();
    public void OnPointerUp(PointerEventData _) => EndPour();
    public void OnPointerExit(PointerEventData _) => EndPour(); // dragging the finger off the button stops it

    private void BeginPour()
    {
        var l = Liquid;
        if (!interactable || l == null || l.IsFull) return;
        pouring = true;
        pouredThisPress = 0f;
        OnPourStarted?.Invoke(ingredientId);
        stream?.Begin(liquidColor);
        brewAudio?.StartPourLoop();
    }

    private void Update()
    {
        if (!pouring) return;

        var l = Liquid;
        if (l == null) { EndPour(); return; }

        float add = fillRatePerSecond * Time.deltaTime;
        l.AddLiquid(add, liquidColor);
        pouredThisPress += add;
        Wobble?.AddImpulse(0.04f);
        stream?.UpdatePositions(OriginPosition, l.SurfaceWorldPosition);

        if (l.IsFull) EndPour();
    }

    private void EndPour()
    {
        if (!pouring) return;

        // Guarantee a quick tap still adds a satisfying splash.
        var l = Liquid;
        if (pouredThisPress < minPourPerTap && l != null && !l.IsFull)
            l.AddLiquid(minPourPerTap - pouredThisPress, liquidColor);

        pouring = false;
        stream?.End();
        brewAudio?.StopPourLoop();
    }

    private void OnDisable() => EndPour();
}
