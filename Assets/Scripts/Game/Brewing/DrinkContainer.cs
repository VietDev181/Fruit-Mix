using UnityEngine;

/// <summary>
/// "The screen IS the drink." Central hub for the cup-less, screen-fill flow that replaces the old
/// CupController. Owns the screen-fill <see cref="LiquidController"/> + <see cref="LiquidWobble"/> and
/// the transform that parents dropped toppings. Other systems (pour, toppings, stir, drink) talk to
/// the drink through here, exactly as they used to talk to the cup — only now the "mouth" is the whole
/// screen instead of a cup silhouette.
///
/// Setup in Unity:
///   1. Drop this on a scene object and assign the screen-fill Liquid (the one driven by
///      <see cref="ScreenLiquidFitter"/>), its Wobble, and an empty ToppingContainer transform.
///   2. Assign the Camera (or leave empty to use Camera.main) — it defines the on-screen drop area.
/// </summary>
public class DrinkContainer : MonoBehaviour
{
    [Header("Liquid")]
    [SerializeField] private LiquidController liquid;
    [SerializeField] private LiquidWobble wobble;

    [Header("Toppings")]
    [Tooltip("Empty transform that parents dropped toppings.")]
    [SerializeField] private Transform toppingContainer;

    [Header("Drop area")]
    [Tooltip("Camera that defines the on-screen drop area. Empty = Camera.main.")]
    [SerializeField] private Camera cam;
    [Tooltip("Fraction of the screen height (from the top) where new toppings drop in from.")]
    [Range(0f, 1f)]
    [SerializeField] private float dropFromTopFraction = 0.92f;

    public LiquidController Liquid => liquid;
    public LiquidWobble Wobble => wobble;
    public Transform ToppingContainer => toppingContainer;

    /// <summary>World point a pour stream aims at: top-centre of the screen.</summary>
    public Vector3 PourTargetPosition
    {
        get
        {
            var c = Cam;
            if (c == null || !c.orthographic) return transform.position;
            float halfH = c.orthographicSize;
            return new Vector3(c.transform.position.x, c.transform.position.y + halfH, transform.position.z);
        }
    }

    private Camera Cam => cam != null ? cam : (cam = Camera.main);

    /// <summary>
    /// True if a world point is somewhere on screen — the whole view is the drink, so a topping
    /// dropped anywhere visible counts as "in".
    /// </summary>
    public bool IsInsideMouth(Vector3 worldPoint)
    {
        var c = Cam;
        if (c == null || !c.orthographic) return true;
        float halfH = c.orthographicSize;
        float halfW = halfH * c.aspect;
        Vector3 ctr = c.transform.position;
        return Mathf.Abs(worldPoint.x - ctr.x) <= halfW &&
               Mathf.Abs(worldPoint.y - ctr.y) <= halfH;
    }

    /// <summary>
    /// A random world point near the top of the screen to drop a topping in from. X is randomised
    /// across the view (minus padding so it never starts off-screen).
    /// </summary>
    public Vector3 RandomDropPointInMouth(float horizontalPadding = 0.2f)
    {
        var c = Cam;
        if (c == null || !c.orthographic) return PourTargetPosition;

        float halfH = c.orthographicSize;
        float halfW = halfH * c.aspect;
        Vector3 ctr = c.transform.position;

        float pad = Mathf.Min(horizontalPadding, halfW * 0.9f);
        float x = ctr.x + Random.Range(-halfW + pad, halfW - pad);
        float y = ctr.y - halfH + dropFromTopFraction * (halfH * 2f);
        return new Vector3(x, y, transform.position.z);
    }

    public void ClearToppings()
    {
        if (toppingContainer == null) return;
        for (int i = toppingContainer.childCount - 1; i >= 0; i--)
            Destroy(toppingContainer.GetChild(i).gameObject);
    }

    /// <summary>Reset the drink to empty for a fresh attempt.</summary>
    public void ResetContainer()
    {
        liquid?.ResetLiquid();
        wobble?.ResetWobble();
        ClearToppings();
    }
}
