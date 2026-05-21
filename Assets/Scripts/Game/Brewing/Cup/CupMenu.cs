using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tap-to-select cup menu, mirroring ToppingMenu: an optional "open" button reveals a scroll view
/// with one icon button per cup shape (from <see cref="CupSelector.Cups"/>). Tapping a cup applies
/// it via <see cref="CupSelector.Select"/>.
///
/// Setup in Unity:
///   1. Build a vertical (or horizontal) ScrollRect; its Content needs a LayoutGroup +
///      ContentSizeFitter. Assign Content to <see cref="content"/>.
///   2. Make a Button prefab with a child Image (the thumbnail); assign to <see cref="buttonPrefab"/>.
///   3. Assign the <see cref="CupSelector"/>. Buttons are generated from its Cups list at runtime.
///   4. Optionally assign <see cref="openButton"/> + <see cref="scrollViewRoot"/> and tick
///      <see cref="hideUntilOpened"/> so the list only appears when the open button is tapped.
/// </summary>
public class CupMenu : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CupSelector selector;

    [Header("UI")]
    [Tooltip("Content RectTransform of the ScrollRect (needs a LayoutGroup).")]
    [SerializeField] private RectTransform content;
    [Tooltip("Button prefab with a child Image used to show each cup thumbnail.")]
    [SerializeField] private Button buttonPrefab;
    [Tooltip("Optional button that opens the cup scroll view (e.g. a 'Choose cup' button).")]
    [SerializeField] private Button openButton;
    [Tooltip("Optional root object of the scroll view, shown/hidden by the open button.")]
    [SerializeField] private GameObject scrollViewRoot;
    [Tooltip("Keep the scroll view hidden until the open button is tapped.")]
    [SerializeField] private bool hideUntilOpened = true;
    [Tooltip("Hide the scroll view again right after a cup is picked.")]
    [SerializeField] private bool closeOnPick = true;

    private readonly List<Button> spawnedButtons = new List<Button>();
    private bool interactable = true;

    private void Start()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (selector != null) selector.OnCupSelected += HighlightSelected;

        BuildButtons();

        if (hideUntilOpened && scrollViewRoot != null)
            scrollViewRoot.SetActive(false);

        ApplyInteractable();
    }

    private void OnDestroy()
    {
        if (selector != null) selector.OnCupSelected -= HighlightSelected;
    }

    /// <summary>Enable/disable the whole menu (e.g. only during the SelectCup phase).</summary>
    public void SetInteractable(bool value)
    {
        interactable = value;
        ApplyInteractable();
    }

    public void Open()
    {
        if (!interactable) return;
        if (scrollViewRoot != null) scrollViewRoot.SetActive(true);
    }

    public void Close()
    {
        if (scrollViewRoot != null) scrollViewRoot.SetActive(false);
    }

    private void BuildButtons()
    {
        foreach (var btn in spawnedButtons)
            if (btn != null) Destroy(btn.gameObject);
        spawnedButtons.Clear();

        if (buttonPrefab == null || content == null || selector == null) return;

        var cups = selector.Cups;
        for (int i = 0; i < cups.Count; i++)
        {
            var def = cups[i];
            if (def == null) continue;

            Button btn = Instantiate(buttonPrefab, content);
            btn.gameObject.SetActive(true);
            SetButtonIcon(btn, def.cupSprite);

            int index = i; // capture by value
            btn.onClick.AddListener(() => PickCup(index, btn));
            spawnedButtons.Add(btn);
        }

        HighlightSelected(selector.CurrentIndex);
    }

    private void SetButtonIcon(Button btn, Sprite icon)
    {
        if (icon == null) return;
        foreach (var img in btn.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject == btn.gameObject) continue;
            img.sprite = icon;
            img.preserveAspect = true;
            return;
        }
        if (btn.image != null) btn.image.sprite = icon;
    }

    private void PickCup(int index, Button source)
    {
        if (!interactable || selector == null) return;

        selector.Select(index);

        source.transform.DOKill();
        source.transform.localScale = Vector3.one;
        source.transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6, 0.6f);

        if (closeOnPick) Close();
    }

    /// <summary>Slightly enlarge the chosen cup's button so the current selection is obvious.</summary>
    private void HighlightSelected(int index)
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            var btn = spawnedButtons[i];
            if (btn == null) continue;
            float scale = (i == index) ? 1.15f : 1f;
            btn.transform.DOScale(scale, 0.15f);
        }
    }

    private void ApplyInteractable()
    {
        foreach (var btn in spawnedButtons)
            if (btn != null) btn.interactable = interactable;
        if (openButton != null) openButton.interactable = interactable;
    }
}
