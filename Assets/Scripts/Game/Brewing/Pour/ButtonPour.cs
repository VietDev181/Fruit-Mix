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
    [Header("Refs")]
    [SerializeField] private CupController cup;
    [SerializeField] private PourStream stream;
    [SerializeField] private BrewingAudio brewAudio;
    [Tooltip("WORLD-space empty at the top of the screen above the cup; the stream starts here. " +
             "If empty, falls back to a point above the cup's pour target.")]
    [SerializeField] private Transform pourOrigin;

    [Header("Ingredient")]
    [SerializeField] private Color liquidColor = new Color(0.85f, 0.6f, 0.35f, 1f);
    [SerializeField] private float fillRatePerSecond = 0.5f;
    [Tooltip("Minimum amount a single quick tap adds, so taps feel responsive.")]
    [SerializeField] private float minPourPerTap = 0.06f;

    [Header("Fallback origin")]
    [Tooltip("Used only when pourOrigin is not assigned: height above the cup's pour target.")]
    [SerializeField] private float fallbackHeight = 6f;

    private bool pouring;
    private float pouredThisPress;

    private Vector3 OriginPosition =>
        pourOrigin != null
            ? pourOrigin.position
            : (cup != null ? cup.PourTargetPosition + Vector3.up * fallbackHeight : transform.position);

    public void OnPointerDown(PointerEventData _) => BeginPour();
    public void OnPointerUp(PointerEventData _) => EndPour();
    public void OnPointerExit(PointerEventData _) => EndPour(); // dragging the finger off the button stops it

    private void BeginPour()
    {
        if (cup == null || cup.Liquid.IsFull) return;
        pouring = true;
        pouredThisPress = 0f;
        stream?.Begin(liquidColor);
        brewAudio?.StartPourLoop();
    }

    private void Update()
    {
        if (!pouring) return;

        float add = fillRatePerSecond * Time.deltaTime;
        cup.Liquid.AddLiquid(add, liquidColor);
        pouredThisPress += add;
        cup.Wobble?.AddImpulse(0.04f);
        stream?.UpdatePositions(OriginPosition, cup.Liquid.SurfaceWorldPosition);

        if (cup.Liquid.IsFull) EndPour();
    }

    private void EndPour()
    {
        if (!pouring) return;

        // Guarantee a quick tap still adds a satisfying splash.
        if (pouredThisPress < minPourPerTap && cup != null && !cup.Liquid.IsFull)
            cup.Liquid.AddLiquid(minPourPerTap - pouredThisPress, liquidColor);

        pouring = false;
        stream?.End();
        brewAudio?.StopPourLoop();
    }

    private void OnDisable() => EndPour();
}
