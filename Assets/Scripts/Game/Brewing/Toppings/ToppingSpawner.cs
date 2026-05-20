using DG.Tweening;
using UnityEngine;

/// <summary>
/// Keeps one draggable topping waiting in a tray slot. When the current topping is dropped into the
/// cup it spawns a fresh one with a little pop, so the tray always looks stocked. Wire the prefab's
/// CupController / Camera / BrewingAudio references on the prefab itself (or let them auto-find).
/// </summary>
public class ToppingSpawner : MonoBehaviour
{
    [SerializeField] private DraggableTopping toppingPrefab;
    [Tooltip("Where the waiting topping sits. Defaults to this transform.")]
    [SerializeField] private Transform slot;
    [SerializeField] private float spawnPopDuration = 0.25f;
    [Tooltip("Wire dependencies onto each spawned instance (leave null to use prefab values).")]
    [SerializeField] private CupController cup;
    [SerializeField] private Camera cam;
    [SerializeField] private BrewingAudio brewAudio;

    private DraggableTopping current;
    private bool interactable = true;

    private void Start()
    {
        if (slot == null) slot = transform;
        Spawn();
    }

    /// <summary>Enable/disable dragging for the waiting topping and all future spawns.</summary>
    public void SetInteractable(bool value)
    {
        interactable = value;
        if (current != null) current.SetInteractable(value);
    }

    private void Spawn()
    {
        if (toppingPrefab == null) return;

        current = Instantiate(toppingPrefab, slot.position, slot.rotation, slot);
        current.Configure(cup, cam, brewAudio);
        current.SetInteractable(interactable);
        current.OnConsumed += HandleConsumed;

        current.transform.localScale = Vector3.zero;
        current.transform.DOScale(1f, spawnPopDuration).SetEase(Ease.OutBack);
    }

    private void HandleConsumed(DraggableTopping topping)
    {
        topping.OnConsumed -= HandleConsumed;
        // The consumed topping now belongs to the cup; refill the slot.
        Spawn();
    }
}
