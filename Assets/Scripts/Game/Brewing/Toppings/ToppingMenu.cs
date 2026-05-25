using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tap-to-drop topping menu with two levels:
///   • Category buttons (Ice / Fruit / Jelly / Pearl…) — one per <see cref="ToppingCategory"/>.
///     Pressing one loads that category's items into the scroll view.
///   • A vertical ScrollRect whose Content is (re)populated with an icon button per topping in the
///     selected category. Tapping a topping button drops one into the cup (physics + buoyancy).
///
/// Setup in Unity:
///   1. Build a vertical ScrollRect; its Content needs a VerticalLayoutGroup + ContentSizeFitter
///      (Vertical = Preferred Size). Assign Content to <see cref="content"/>.
///   2. Make a Button prefab with a child Image (the icon); assign to <see cref="buttonPrefab"/>.
///   3. Place your category buttons in the scene (Ice, Fruit, …). For each, add a
///      <see cref="ToppingCategory"/> entry: drag the button into 'Category Button' and fill 'Items'.
///   4. Optionally assign <see cref="scrollViewRoot"/> (the panel to show/hide) and tick
///      <see cref="hideUntilCategoryChosen"/> so the list stays hidden until a category is tapped.
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

    [Serializable]
    public class ToppingCategory
    {
        public string id;
        [Tooltip("The scene button that loads this category into the scroll view (e.g. the 'Ice' button).")]
        public Button categoryButton;
        [Tooltip("Toppings shown in the scroll view when this category is selected.")]
        public ToppingMenuItem[] items;
    }

    [Header("Categories")]
    [SerializeField] private ToppingCategory[] categories;

    [Header("UI")]
    [Tooltip("Content RectTransform of the vertical ScrollRect (needs a VerticalLayoutGroup).")]
    [SerializeField] private RectTransform content;
    [Tooltip("Button prefab with a child Image used to show each topping icon.")]
    [SerializeField] private Button buttonPrefab;
    [Tooltip("Optional root object of the scroll view, shown/hidden as categories are selected.")]
    [SerializeField] private GameObject scrollViewRoot;
    [Tooltip("Keep the scroll view hidden until the player taps a category button.")]
    [SerializeField] private bool hideUntilCategoryChosen = true;

    [Header("Scene refs (injected into each spawned topping)")]
    [SerializeField] private DrinkContainer container;
    [SerializeField] private Camera cam;
    [SerializeField] private BrewingAudio brewAudio;
    [SerializeField] private BrewingManager brewingManager;
    [SerializeField] private RecipeIngredientUI ingredientUI;

    [Header("Drop")]
    [Tooltip("Fixed point toppings drop from. Leave null to use the screen's top-centre.")]
    [SerializeField] private Transform dropPoint;
    [Tooltip("Briefly block re-tapping after a drop so a single tap = one topping.")]
    [SerializeField] private float dropCooldown = 0.15f;
    [Tooltip("Total topping space the cup holds. A drop is blocked when the sum of toppings' FillCost " +
             "would exceed this — so many small toppings or few big ones. 0 = no limit.")]
    [SerializeField] private float capacity = 12f;
    [Tooltip("Hard cap on how many toppings can exist at once (anti-spam / anti-lag). 0 = no limit.")]
    [SerializeField] private int maxToppings = 40;

    private readonly List<Button> spawnedButtons = new List<Button>();
    private bool interactable = true;
    private float nextDropTime;
    private ToppingCategory activeCategory;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (container == null) container = FindObjectOfType<DrinkContainer>();
    }

    private void Start()
    {
        WireCategoryButtons();
        if (hideUntilCategoryChosen && scrollViewRoot != null)
            scrollViewRoot.SetActive(false);
        ApplyInteractable();
    }

    /// <summary>Enable/disable the whole menu (driven by BrewingManager per phase).</summary>
    public void SetInteractable(bool value)
    {
        interactable = value;
        ApplyInteractable();
    }

    private void WireCategoryButtons()
    {
        if (categories == null) return;
        foreach (var cat in categories)
        {
            if (cat == null || cat.categoryButton == null) continue;
            var captured = cat; // avoid closure capturing the loop variable
            cat.categoryButton.onClick.AddListener(() => ShowCategory(captured));
        }
    }

    /// <summary>
    /// Load a category's toppings into the scroll view and reveal it. Tapping the category that is
    /// already open closes the scroll view again (toggle).
    /// </summary>
    public void ShowCategory(ToppingCategory category)
    {
        if (!interactable || category == null) return;

        bool isOpen = scrollViewRoot == null || scrollViewRoot.activeSelf;
        if (isOpen && category == activeCategory)
        {
            HideScrollView();
            return;
        }

        activeCategory = category;
        if (scrollViewRoot != null) scrollViewRoot.SetActive(true);
        BuildButtons(category.items);
    }

    /// <summary>Hide the scroll view and clear which category is active.</summary>
    public void HideScrollView()
    {
        activeCategory = null;
        if (scrollViewRoot != null) scrollViewRoot.SetActive(false);
    }

    private void BuildButtons(ToppingMenuItem[] items)
    {
        // Clear the previous category's buttons.
        foreach (var btn in spawnedButtons)
            if (btn != null) Destroy(btn.gameObject);
        spawnedButtons.Clear();

        if (buttonPrefab == null || content == null || items == null) return;

        foreach (var item in items)
        {
            if (item == null || item.prefab == null) continue;

            Button btn = Instantiate(buttonPrefab, content);
            btn.gameObject.SetActive(true);
            SetButtonIcon(btn, item.icon);

            // Pop each icon in with a slight stagger so the list "unrolls" instead of snapping.
            btn.transform.localScale = Vector3.zero;
            btn.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetDelay(spawnedButtons.Count * 0.04f);

            var captured = item; // avoid closure capturing the loop variable
            btn.onClick.AddListener(() => DropTopping(captured, btn));
            spawnedButtons.Add(btn);
        }

        ApplyInteractable();
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
        if (!interactable || container == null || item.prefab == null) return;
        if (Time.unscaledTime < nextDropTime) return;

        // Block the drop if it would push the drink past its space budget (big toppings cost more).
        if (capacity > 0f && CurrentFill() + item.fillCost > capacity) return;

        // Hard count cap: never let the scene fill with toppings (physics lag / spam guard).
        if (maxToppings > 0)
        {
            Transform parent = container.ToppingContainer;
            if (parent != null && parent.childCount >= maxToppings) return;
        }

        nextDropTime = Time.unscaledTime + dropCooldown;

        Vector3 spawnPos = dropPoint != null ? dropPoint.position : container.PourTargetPosition;
        var topping = Instantiate(item.prefab, spawnPos, Quaternion.identity);
        topping.Configure(container, cam, brewAudio);
        topping.FillCost = item.fillCost;
        topping.SetInteractable(true); // id comes from the prefab itself (DraggableTopping.Id)
        topping.DropAt(spawnPos);
        brewingManager?.NotifyToppingAdded();
        ingredientUI?.NotifyToppingAdded(topping.Id);

        // Little tactile pop on the tapped button.
        source.transform.DOKill();
        source.transform.localScale = Vector3.one;
        source.transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6, 0.6f);
    }

    /// <summary>Sum of FillCost over toppings currently in the cup. Reads live from the container so
    /// clearing the cup (new round) frees the space automatically.</summary>
    private float CurrentFill()
    {
        Transform parent = container != null ? container.ToppingContainer : null;
        if (parent == null) return 0f;

        float total = 0f;
        for (int i = 0; i < parent.childCount; i++)
        {
            var t = parent.GetChild(i).GetComponent<DraggableTopping>();
            if (t != null) total += t.FillCost;
        }
        return total;
    }

    private void ApplyInteractable()
    {
        foreach (var btn in spawnedButtons)
            if (btn != null) btn.interactable = interactable;

        if (categories != null)
            foreach (var cat in categories)
                if (cat != null && cat.categoryButton != null)
                    cat.categoryButton.interactable = interactable;
    }
}
