using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Replaces the drag-from-tray flow with a vertical, scrollable menu of topping types. Each entry
/// is shown as an icon button inside a ScrollRect; tapping a button drops one topping of that type
/// into the cup at a random point inside the mouth (it then falls + bobs via the usual physics).
///
/// Setup in Unity:
///   1. Build a UI ScrollRect (vertical) with a Content object that has a VerticalLayoutGroup +
///      ContentSizeFitter (Vertical = Preferred Size).
///   2. Make a Button prefab with an Image child for the icon, assign it to <see cref="buttonPrefab"/>
///      and the Content RectTransform to <see cref="content"/>.
///   3. Fill <see cref="items"/> with one entry per topping (icon sprite + DraggableTopping prefab).
///   4. Wire cup / cam / brewAudio (or leave null to auto-find).
/// The buttons are generated at runtime from <see cref="items"/>.
/// </summary>
public class ToppingMenu : MonoBehaviour
{
    [Serializable]
    public class ToppingMenuItem
    {
        public string id;
        [Tooltip("Icon shown in the scroll list for this topping.")]
        public Sprite icon;
        [Tooltip("The topping prefab to drop. Set its 'Draggable' to OFF on the prefab.")]
        public DraggableTopping prefab;
        [Tooltip("How much cup space one of these takes. Small toppings (e.g. pearls) ~0.5; big " +
                 "toppings (e.g. a fruit slice / ice cube) ~2. The cup fills up by Capacity total.")]
        public float fillCost = 1f;
    }

    [Header("Menu data")]
    [SerializeField] private ToppingMenuItem[] items;

    [Header("UI")]
    [Tooltip("Content RectTransform of the vertical ScrollRect (needs a VerticalLayoutGroup).")]
    [SerializeField] private RectTransform content;
    [Tooltip("Button prefab with a child Image used to show each topping icon.")]
    [SerializeField] private Button buttonPrefab;

    [Header("Scene refs (injected into each spawned topping)")]
    [SerializeField] private CupController cup;
    [SerializeField] private Camera cam;
    [SerializeField] private BrewingAudio brewAudio;

    [Header("Drop")]
    [Tooltip("Fixed point toppings drop from. Leave null to use the cup's pour target (top-centre).")]
    [SerializeField] private Transform dropPoint;
    [Tooltip("Briefly block re-tapping after a drop so a single tap = one topping.")]
    [SerializeField] private float dropCooldown = 0.15f;
    [Tooltip("Total topping space the cup holds. A drop is blocked when the sum of toppings' FillCost " +
             "would exceed this — so many small toppings or few big ones. 0 = no limit.")]
    [SerializeField] private float capacity = 12f;

    private readonly List<Button> spawnedButtons = new List<Button>();
    private bool interactable = true;
    private float nextDropTime;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (cup == null) cup = FindObjectOfType<CupController>();
    }

    private void Start()
    {
        BuildButtons();
        ApplyInteractable();
    }

    /// <summary>Enable/disable the whole menu (driven by BrewingManager per phase).</summary>
    public void SetInteractable(bool value)
    {
        interactable = value;
        ApplyInteractable();
    }

    private void BuildButtons()
    {
        if (buttonPrefab == null || content == null || items == null) return;

        foreach (var item in items)
        {
            if (item == null || item.prefab == null) continue;

            Button btn = Instantiate(buttonPrefab, content);
            btn.gameObject.SetActive(true);
            SetButtonIcon(btn, item.icon);

            var captured = item; // avoid closure capturing the loop variable
            btn.onClick.AddListener(() => DropTopping(captured, btn));
            spawnedButtons.Add(btn);
        }
    }

    private void SetButtonIcon(Button btn, Sprite icon)
    {
        if (icon == null) return;
        // Use the first Image that isn't the button's own background target graphic.
        foreach (var img in btn.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject == btn.gameObject) continue;
            img.sprite = icon;
            img.preserveAspect = true;
            return;
        }
        // Fallback: no child image, paint the button graphic itself.
        if (btn.image != null) btn.image.sprite = icon;
    }

    private void DropTopping(ToppingMenuItem item, Button source)
    {
        if (!interactable || cup == null || item.prefab == null) return;
        if (Time.unscaledTime < nextDropTime) return;

        // Block the drop if it would push the cup past its space budget (big toppings cost more).
        if (capacity > 0f && CurrentFill() + item.fillCost > capacity) return;

        nextDropTime = Time.unscaledTime + dropCooldown;

        Vector3 spawnPos = dropPoint != null ? dropPoint.position : cup.PourTargetPosition;
        var topping = Instantiate(item.prefab, spawnPos, Quaternion.identity);
        topping.Configure(cup, cam, brewAudio);
        topping.FillCost = item.fillCost;
        topping.SetInteractable(true);
        topping.DropAt(spawnPos);

        // Little tactile pop on the tapped button.
        source.transform.DOKill();
        source.transform.localScale = Vector3.one;
        source.transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6, 0.6f);
    }

    /// <summary>Sum of FillCost over toppings currently in the cup. Reads live from the container so
    /// clearing the cup (new round) frees the space automatically.</summary>
    private float CurrentFill()
    {
        Transform container = cup != null ? cup.ToppingContainer : null;
        if (container == null) return 0f;

        float total = 0f;
        for (int i = 0; i < container.childCount; i++)
        {
            var t = container.GetChild(i).GetComponent<DraggableTopping>();
            if (t != null) total += t.FillCost;
        }
        return total;
    }

    private void ApplyInteractable()
    {
        foreach (var btn in spawnedButtons)
            if (btn != null) btn.interactable = interactable;
    }
}
