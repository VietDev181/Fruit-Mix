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
    [SerializeField] private CupController cup;
    [SerializeField] private BrewingAudio brewAudio;
    [Tooltip("Optional one-shot particle burst spawned at the splash point on drop.")]
    [SerializeField] private ParticleSystem dropBurstPrefab;

    [Header("Physics on drop")]
    [SerializeField] private float dropGravityScale = 1.4f;
    [SerializeField] private float dropDownVelocity = 3f;
    [SerializeField] private float spinOnDrop = 120f;

    [Header("Feel")]
    [SerializeField] private float grabScale = 1.15f;
    [SerializeField] private float returnDuration = 0.3f;

    /// <summary>Raised when this topping is committed into the cup (so a spawner can refill the slot).</summary>
    public event Action<DraggableTopping> OnConsumed;

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
    public void Configure(CupController cupRef, Camera camRef, BrewingAudio audioRef)
    {
        if (cupRef != null) cup = cupRef;
        if (camRef != null) cam = camRef;
        if (audioRef != null) brewAudio = audioRef;
    }

    private void Update()
    {
        if (consumed || !interactable) return;

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

        Vector3 pos = transform.position;
        if (cup != null && cup.IsInsideMouth(pos))
            DropIntoCup(pos);
        else
            ReturnToTray();
    }

    private void DropIntoCup(Vector3 dropPos)
    {
        consumed = true;
        if (cup.ToppingContainer != null)
            transform.SetParent(cup.ToppingContainer, true);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = dropGravityScale;
        rb.velocity = Vector2.down * dropDownVelocity;
        rb.angularVelocity = UnityEngine.Random.Range(-spinOnDrop, spinOnDrop);

        if (buoyancy != null) buoyancy.enabled = true;

        // Juicy feedback: squash on impact-ish, plop sound, splash burst, liquid slosh.
        transform.DOPunchScale(new Vector3(0.25f, -0.25f, 0f), 0.3f, 8, 0.6f);
        brewAudio?.PlayPlop();
        cup.Wobble?.AddImpulse(0.25f);
        if (dropBurstPrefab != null)
        {
            var burst = Instantiate(dropBurstPrefab, cup.Liquid.SurfaceWorldPosition, Quaternion.identity);
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
