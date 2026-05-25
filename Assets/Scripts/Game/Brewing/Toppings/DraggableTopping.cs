using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// A topping (pearl / jelly / fruit / ice) that lives in a tray and is dragged into the cup.
/// While in the tray it is kinematic; dropped inside the cup mouth it becomes a dynamic Rigidbody2D
/// so it bounces and settles, then <see cref="ToppingBuoyancy"/> makes it bob in the liquid.
/// Dropped outside the cup it springs back to its tray slot.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DraggableTopping : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private DrinkContainer container;
    [SerializeField] private BrewingAudio brewAudio;
    [Tooltip("Optional one-shot particle burst spawned at the splash point on drop.")]
    [SerializeField] private ParticleSystem dropBurstPrefab;

    [Header("Physics on drop")]
    [SerializeField] private float dropGravityScale = 1.4f;
    [SerializeField] private float dropDownVelocity = 1.2f;
    [SerializeField] private float spinOnDrop = 40f;

    [Header("Feel")]
    [SerializeField] private float grabScale = 1.15f;
    [SerializeField] private float returnDuration = 0.3f;

    [Header("Input mode")]
    [Tooltip("Old flow: player drags this topping from a tray into the cup. Turn OFF when the topping " +
             "is spawned by ToppingMenu and dropped on a button press (no dragging).")]
    [SerializeField] private bool draggable = true;

    /// <summary>Raised when this topping is committed into the cup (so a spawner can refill the slot).</summary>
    public event Action<DraggableTopping> OnConsumed;

    /// <summary>How much cup space this topping takes up — used by ToppingMenu to cap the fill so big
    /// toppings fill the cup faster than small ones. Set when spawned; default 1.</summary>
    public float FillCost { get; set; } = 1f;

    [Header("Identity")]
    [Tooltip("Topping type id (e.g. \"pearl\", \"ice\"). Lives on the prefab so dropped instances and " +
             "recipe references share it — used by recipe matching.")]
    [SerializeField] private string id;
    /// <summary>Topping type id. Serialized on the prefab; settable at runtime if needed.</summary>
    public string Id { get => id; set => id = value; }
    /// <summary>Reads the sprite directly from the SpriteRenderer on this prefab.</summary>
    public Sprite Icon => GetComponent<SpriteRenderer>()?.sprite;

    private Rigidbody2D rb;
    private Collider2D col;
    private ToppingBuoyancy buoyancy;
    private Vector3 homePosition;
    private Vector3 grabOffset;
    private bool dragging;
    private bool consumed;
    private bool interactable = true;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        buoyancy = GetComponent<ToppingBuoyancy>();
        homePosition = transform.position;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        if (buoyancy != null) buoyancy.enabled = false;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
    }

    /// <summary>Inject scene dependencies — needed because prefabs can't reference scene objects.</summary>
    public void Configure(DrinkContainer containerRef, Camera camRef, BrewingAudio audioRef)
    {
        if (containerRef != null) container = containerRef;
        if (camRef != null) cam = camRef;
        if (audioRef != null) brewAudio = audioRef;
        if (buoyancy != null) buoyancy.SetContainer(container); // so buoyancy knows the liquid surface
    }

    /// <summary>
    /// Spawn-and-drop entry point for the no-drag flow (ToppingMenu). Places the topping at
    /// <paramref name="worldPos"/> then immediately commits it into the drink with the usual physics
    /// + juicy feedback. No tray, no dragging.
    /// </summary>
    public void DropAt(Vector3 worldPos)
    {
        if (consumed) return;
        transform.position = worldPos;
        homePosition = worldPos;
        DropIntoCup(worldPos);
    }

    private void Update()
    {
        if (consumed || !interactable || !draggable) return;

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

        Vector3 target = DragInput2D.WorldPosition(cam) + grabOffset;
        target.z = homePosition.z;
        rb.MovePosition(target);
    }

    private void BeginDrag(Vector3 pointerWorld)
    {
        dragging = true;
        grabOffset = transform.position - pointerWorld;
        transform.DOScale(grabScale, 0.12f).SetEase(Ease.OutBack);
    }

    private void EndDrag()
    {
        dragging = false;
        DragInput2D.Release(this);
        transform.DOScale(1f, 0.15f);

        // Forgiving drop test: count it as "in the cup" if EITHER the topping centre OR the
        // pointer is inside the mouth. Grabbing a topping by its edge offsets the body from the
        // finger, so checking only the centre made drops feel random ("lúc được lúc không").
        Vector3 pos = transform.position;
        Vector3 pointer = DragInput2D.WorldPosition(cam);
        if (container != null && (container.IsInsideMouth(pos) || container.IsInsideMouth(pointer)))
            DropIntoCup(pos);
        else
            ReturnToTray();
    }

    private void DropIntoCup(Vector3 dropPos)
    {
        consumed = true;
        if (container.ToppingContainer != null)
            transform.SetParent(container.ToppingContainer, true);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = dropGravityScale;
        rb.velocity = Vector2.down * dropDownVelocity;
        rb.angularVelocity = UnityEngine.Random.Range(-spinOnDrop, spinOnDrop);

        if (buoyancy != null) buoyancy.enabled = true;

        // Juicy feedback: squash on impact-ish, plop sound, splash burst, liquid slosh.
        transform.DOPunchScale(new Vector3(0.25f, -0.25f, 0f), 0.3f, 8, 0.6f);
        brewAudio?.PlayPlop();
        container.Wobble?.AddImpulse(0.25f);
        if (dropBurstPrefab != null)
        {
            var burst = Instantiate(dropBurstPrefab, container.Liquid.SurfaceWorldPosition, Quaternion.identity);
            burst.Play();
            Destroy(burst.gameObject, 2f);
        }

        OnConsumed?.Invoke(this);
    }

    private void ReturnToTray()
    {
        transform.DOMove(homePosition, returnDuration).SetEase(Ease.OutBack);
    }

    private void OnDestroy()
    {
        DragInput2D.Release(this);
    }
}
