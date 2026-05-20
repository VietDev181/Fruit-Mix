using DG.Tweening;
using UnityEngine;

/// <summary>
/// A draggable ingredient bottle (tea / milk / soda / syrup). Drag it over the cup mouth; while the
/// spout sits above the cup it tilts and pours, raising the liquid and tinting the mix. Releasing
/// or dragging away stops the pour and the bottle springs back to its shelf position.
///
/// Drag uses the shared single-pointer lock in <see cref="DragInput2D"/> so only one object follows
/// the finger at a time. Requires a Collider2D on this object for hit testing.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class IngredientBottle : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private CupController cup;
    [SerializeField] private PourStream stream;
    [SerializeField] private BrewingAudio brewAudio;
    [Tooltip("Empty child at the bottle's pouring lip; the stream starts here.")]
    [SerializeField] private Transform spout;

    [Header("Ingredient")]
    [SerializeField] private Color liquidColor = new Color(0.85f, 0.6f, 0.35f, 1f);
    [Tooltip("Fraction of the cup filled per second while pouring.")]
    [SerializeField] private float fillRatePerSecond = 0.45f;

    [Header("Pour zone")]
    [Tooltip("Pour starts when the spout is within this 2D distance of the cup's pour target.")]
    [SerializeField] private float pourRadius = 3f;

    [Header("Feel")]
    [SerializeField] private float pourTiltAngle = -65f;
    [SerializeField] private float returnDuration = 0.35f;
    [SerializeField] private float grabScale = 1.12f;

    private Collider2D col;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Vector3 grabOffset;
    private bool dragging;
    private bool pouring;
    private bool interactable = true;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        col = GetComponent<Collider2D>();
        homePosition = transform.position;
        homeRotation = transform.rotation;
        if (spout == null) spout = transform;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!value && dragging) EndDrag();
    }

    // --- Debug read-outs ---
    public bool Interactable => interactable;
    public bool Dragging => dragging;
    public bool Pouring => pouring;
    public float SpoutDistanceToCup =>
        (cup != null && spout != null) ? Vector2.Distance(spout.position, cup.PourTargetPosition) : -1f;
    public float PourRadius => pourRadius;
    public bool PointerInsideCollider =>
        col != null && col.OverlapPoint(DragInput2D.WorldPosition(cam));
    public Vector3 ColliderWorldCenter => col != null ? (Vector3)col.bounds.center : transform.position;

    private void Update()
    {
        if (!interactable) return;

        if (!dragging && DragInput2D.PressedThisFrame && !DragInput2D.HasOwner)
        {
            Vector3 p = DragInput2D.WorldPosition(cam);
            if (col.OverlapPoint(p) && DragInput2D.TryClaim(this))
                BeginDrag(p);
        }

        if (!dragging) return;

        if (DragInput2D.ReleasedThisFrame || !DragInput2D.IsOwner(this))
        {
            EndDrag();
            return;
        }

        FollowPointer();
        UpdatePourState();
    }

    private void BeginDrag(Vector3 pointerWorld)
    {
        dragging = true;
        grabOffset = transform.position - pointerWorld;
        transform.DOScale(grabScale, 0.12f).SetEase(Ease.OutBack);
    }

    private void FollowPointer()
    {
        Vector3 target = DragInput2D.WorldPosition(cam) + grabOffset;
        target.z = homePosition.z;
        transform.position = Vector3.Lerp(transform.position, target, 0.5f);
    }

    private void UpdatePourState()
    {
        Vector3 mouth = cup != null ? cup.PourTargetPosition : transform.position;
        bool overCup = Vector2.Distance(spout.position, mouth) < pourRadius;

        bool canPour = overCup && cup != null && !cup.Liquid.IsFull;

        if (canPour && !pouring) StartPour();
        else if (!canPour && pouring) StopPour();

        if (pouring) PourTick();
    }

    private void StartPour()
    {
        pouring = true;
        transform.DORotate(new Vector3(0f, 0f, pourTiltAngle), 0.2f).SetEase(Ease.OutCubic);
        stream?.Begin(liquidColor);
        brewAudio?.StartPourLoop();
    }

    private void PourTick()
    {
        cup.Liquid.AddLiquid(fillRatePerSecond * Time.deltaTime, liquidColor);
        cup.Wobble?.AddImpulse(0.04f);
        stream?.UpdatePositions(spout.position, cup.Liquid.SurfaceWorldPosition);
        if (cup.Liquid.IsFull) StopPour();
    }

    private void StopPour()
    {
        if (!pouring) return;
        pouring = false;
        transform.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutCubic);
        stream?.End();
        brewAudio?.StopPourLoop();
    }

    private void EndDrag()
    {
        StopPour();
        dragging = false;
        DragInput2D.Release(this);
        transform.DOScale(1f, 0.15f);
        transform.DOMove(homePosition, returnDuration).SetEase(Ease.OutBack);
        transform.DORotateQuaternion(homeRotation, returnDuration).SetEase(Ease.OutCubic);
    }

    private void OnDestroy()
    {
        DragInput2D.Release(this);
    }
}
