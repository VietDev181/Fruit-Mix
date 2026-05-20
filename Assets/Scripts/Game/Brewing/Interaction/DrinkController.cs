using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Drag the finished cup upward (toward the "mouth") to drink it: the cup tilts, the liquid drains
/// in proportion to how far it's lifted, a slurp/straw ASMR loop plays, and toppings sink as the
/// surface drops. Empties to <see cref="OnEmptied"/>. Active during the Drink phase only.
/// </summary>
public class DrinkController : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private CupController cup;
    [Tooltip("The transform that moves when drinking (usually the cup root).")]
    [SerializeField] private Transform cupRoot;
    [Tooltip("Collider used to grab the cup.")]
    [SerializeField] private Collider2D cupCollider;
    [SerializeField] private BrewingAudio brewAudio;

    [Header("Drink feel")]
    [Tooltip("How far the cup must be lifted above home before it starts draining.")]
    [SerializeField] private float liftThreshold = 0.6f;
    [Tooltip("Max fraction of the cup drained per second at full lift.")]
    [SerializeField] private float drainRatePerSecond = 0.5f;
    [Tooltip("Cup tilt (deg) at full lift, simulating tipping toward the mouth.")]
    [SerializeField] private float maxTilt = 22f;
    [SerializeField] private float liftForFullDrain = 2.5f;
    [SerializeField] private float returnDuration = 0.35f;

    public event Action OnEmptied;

    private bool active;
    private bool dragging;
    private bool sipping;
    private bool emptied;
    private Vector3 homePosition;
    private Vector3 grabOffset;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (cupRoot == null) cupRoot = cup != null ? cup.transform : transform;
        homePosition = cupRoot.position;
    }

    public void SetActive(bool value)
    {
        active = value;
        if (!active && dragging) EndDrag();
    }

    public void CaptureHome() => homePosition = cupRoot.position;

    private void Update()
    {
        if (!active || emptied) return;

        if (!dragging && DragInput2D.PressedThisFrame && !DragInput2D.HasOwner)
        {
            Vector3 p = DragInput2D.WorldPosition(cam);
            if (cupCollider != null && cupCollider.OverlapPoint(p) && DragInput2D.TryClaim(this))
            {
                dragging = true;
                grabOffset = cupRoot.position - p;
            }
        }

        if (!dragging) return;

        if (DragInput2D.ReleasedThisFrame || !DragInput2D.IsOwner(this))
        {
            EndDrag();
            return;
        }

        DrinkTick();
    }

    private void DrinkTick()
    {
        Vector3 target = DragInput2D.WorldPosition(cam) + grabOffset;
        target.x = homePosition.x;          // constrain to vertical drinking motion
        target.z = homePosition.z;
        cupRoot.position = Vector3.Lerp(cupRoot.position, target, 0.5f);

        float lift = cupRoot.position.y - homePosition.y;
        float t = Mathf.Clamp01((lift - liftThreshold) / Mathf.Max(0.01f, liftForFullDrain - liftThreshold));

        cupRoot.localRotation = Quaternion.Euler(0f, 0f, -maxTilt * t);

        bool shouldSip = t > 0f && !cup.Liquid.IsEmpty;
        if (shouldSip)
        {
            cup.Liquid.DrainLiquid(drainRatePerSecond * t * Time.deltaTime);
            cup.Wobble?.AddImpulse(0.03f * t);
            if (!sipping) { brewAudio?.StartSipLoop(); sipping = true; }

            if (cup.Liquid.IsEmpty)
            {
                emptied = true;
                StopSip();
                brewAudio?.PlayFinalGulp();
                OnEmptied?.Invoke();
            }
        }
        else if (sipping)
        {
            StopSip();
        }
    }

    private void EndDrag()
    {
        dragging = false;
        DragInput2D.Release(this);
        StopSip();
        cupRoot.DOMove(homePosition, returnDuration).SetEase(Ease.OutBack);
        cupRoot.DOLocalRotateQuaternion(Quaternion.identity, returnDuration).SetEase(Ease.OutCubic);
    }

    private void StopSip()
    {
        if (!sipping) return;
        sipping = false;
        brewAudio?.StopSipLoop();
    }

    public void ResetDrink()
    {
        emptied = false;
        StopSip();
        if (cupRoot != null)
        {
            cupRoot.position = homePosition;
            cupRoot.localRotation = Quaternion.identity;
        }
    }

    private void OnDestroy() => DragInput2D.Release(this);
}
