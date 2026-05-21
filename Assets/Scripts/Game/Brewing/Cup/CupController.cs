using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Central hub for the cup on screen. Owns the cup sprite, the SpriteMask that clips the liquid +
/// toppings to the cavity, the LiquidController/LiquidWobble, and the container that parents
/// dropped toppings. Other systems (pour, toppings, stir, drink) talk to the cup through here.
/// </summary>
[Serializable]
public class CupDefinition
{
    public string id;
    public Sprite cupSprite;
    [Tooltip("Silhouette of the inner cavity used by the SpriteMask. Often the same art as the cup interior.")]
    public Sprite maskSprite;
    [Tooltip("Local Y of the cavity bottom for this cup shape.")]
    public float cavityBottomLocalY = -1.5f;
    [Tooltip("Cavity height in local units for this cup shape.")]
    public float cavityHeight = 3f;
    [Tooltip("Body width the liquid should span for this cup.")]
    public float liquidWidth = 2f;
}

public class CupController : MonoBehaviour
{
    [Header("Cup visuals")]
    [SerializeField] private SpriteRenderer cupBody;
    [SerializeField] private SpriteMask cavityMask;

    [Header("Liquid")]
    [SerializeField] private LiquidController liquid;
    [SerializeField] private LiquidWobble wobble;
    [Tooltip("Shader-based liquid clip. Its mask must follow the chosen cup, so SetCup updates it.")]
    [SerializeField] private LiquidShaderClip liquidClip;

    [Header("Toppings")]
    [Tooltip("Empty transform that parents dropped toppings (also masked by the cavity).")]
    [SerializeField] private Transform toppingContainer;
    [Tooltip("Builds wall/floor colliders matching the cup shape so toppings don't fall through.")]
    [SerializeField] private CupCavityCollider cavityCollider;
    [Tooltip("Trigger collider matching the cavity opening; used to detect drops into the cup.")]
    [SerializeField] private Collider2D mouthTrigger;

    [Header("Pour anchor")]
    [Tooltip("World point a bottle aims at when pouring (top-centre of the cup).")]
    [SerializeField] private Transform pourTarget;

    public LiquidController Liquid => liquid;
    public LiquidWobble Wobble => wobble;
    public Transform ToppingContainer => toppingContainer;
    public Vector3 PourTargetPosition => pourTarget != null ? pourTarget.position : transform.position;

    public event Action OnCupChanged;

    /// <summary>Apply a cup shape: sprite, mask and cavity dimensions, then reset its contents.</summary>
    public void SetCup(CupDefinition def)
    {
        if (def == null) return;

        Sprite mask = def.maskSprite != null ? def.maskSprite : def.cupSprite;
        if (cupBody != null) cupBody.sprite = def.cupSprite;
        if (cavityMask != null) cavityMask.sprite = mask;
        if (liquidClip != null) liquidClip.SetMask(mask); // shader clip is what actually clips the liquid
        if (cavityCollider != null) cavityCollider.Rebuild(mask); // walls/floor follow the cup shape

        liquid?.ConfigureCavity(def.cavityBottomLocalY, def.cavityHeight, def.liquidWidth);
        liquid?.ResetLiquid();

        ClearToppings();
        wobble?.ResetWobble();
        OnCupChanged?.Invoke();

        // Little pop when a new cup is placed.
        if (cupBody != null)
        {
            cupBody.transform.localScale = Vector3.one * 0.85f;
            cupBody.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
    }

    /// <summary>Apply the same cup shape this controller already has, refreshing the visual pop.</summary>
    public void ReplayPlacePop()
    {
        if (cupBody == null) return;
        cupBody.transform.localScale = Vector3.one * 0.85f;
        cupBody.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    /// <summary>True if a world point is inside the cup mouth (used when dropping toppings).</summary>
    public bool IsInsideMouth(Vector3 worldPoint)
    {
        return mouthTrigger != null && mouthTrigger.OverlapPoint(worldPoint);
    }

    /// <summary>
    /// A random world point just inside the cup mouth to drop a topping into. X is randomised across
    /// the mouth width (minus padding so it never clips the rim); Y sits at the top of the mouth so
    /// the topping falls in. Falls back to the pour target if no mouth trigger is wired.
    /// </summary>
    public Vector3 RandomDropPointInMouth(float horizontalPadding = 0.2f)
    {
        if (mouthTrigger == null) return PourTargetPosition;

        Bounds b = mouthTrigger.bounds;
        float halfPad = Mathf.Min(horizontalPadding, b.extents.x * 0.9f);
        float x = UnityEngine.Random.Range(b.min.x + halfPad, b.max.x - halfPad);
        return new Vector3(x, b.max.y, transform.position.z);
    }

    public void ClearToppings()
    {
        if (toppingContainer == null) return;
        for (int i = toppingContainer.childCount - 1; i >= 0; i--)
            Destroy(toppingContainer.GetChild(i).gameObject);
    }

    public void ResetCup()
    {
        liquid?.ResetLiquid();
        wobble?.ResetWobble();
        ClearToppings();
    }
}
