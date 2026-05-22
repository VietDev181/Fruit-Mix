using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simple dropdown: one toggle button shows/hides a panel that "drops" open (unrolls from the top
/// with a little fade). Use it to collapse the row of pour buttons behind a single button — tap to
/// reveal the water choices, tap again (or pick one) to close.
///
/// Setup in Unity:
///   1. Make a panel (a child object) that holds the buttons you want inside the dropdown — set its
///      RectTransform PIVOT to the TOP (Y = 1) so it unrolls downward.
///   2. Assign that panel to <see cref="panel"/> and the trigger button to <see cref="toggleButton"/>.
///   3. Optionally tick <see cref="closeOnPick"/> so choosing any button inside also closes the menu.
/// </summary>
public class DropdownMenu : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Button that opens / closes the dropdown.")]
    [SerializeField] private Button toggleButton;
    [Tooltip("The panel that drops open. Set its pivot Y to 1 (top) so it unrolls downward.")]
    [SerializeField] private RectTransform panel;
    [Tooltip("Optional arrow/chevron icon rotated 180° while open.")]
    [SerializeField] private RectTransform arrow;

    [Header("Behaviour")]
    [SerializeField] private bool startOpen = false;
    [Tooltip("Close the menu automatically when any Button inside the panel is pressed.")]
    [SerializeField] private bool closeOnPick = true;
    [SerializeField] private float animDuration = 0.22f;

    private CanvasGroup group;
    private bool open;
    private Tween anim;

    private void Awake()
    {
        if (panel != null)
        {
            group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.gameObject.AddComponent<CanvasGroup>();
        }

        if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);

        if (closeOnPick && panel != null)
        {
            foreach (var btn in panel.GetComponentsInChildren<Button>(true))
                btn.onClick.AddListener(Close);
        }

        SetOpenInstant(startOpen);
    }

    public void Toggle() { if (open) Close(); else Open(); }

    public void Open()
    {
        if (panel == null || open) return;
        open = true;
        panel.gameObject.SetActive(true);

        anim?.Kill();
        anim = DOTween.Sequence()
            .Append(panel.DOScaleY(1f, animDuration).SetEase(Ease.OutBack))
            .Join(group.DOFade(1f, animDuration * 0.8f));
        group.interactable = true;
        group.blocksRaycasts = true;
        RotateArrow(true);
    }

    public void Close()
    {
        if (panel == null || !open) return;
        open = false;

        group.interactable = false;
        group.blocksRaycasts = false;
        anim?.Kill();
        anim = DOTween.Sequence()
            .Append(panel.DOScaleY(0f, animDuration).SetEase(Ease.InBack))
            .Join(group.DOFade(0f, animDuration * 0.8f))
            .OnComplete(() => panel.gameObject.SetActive(false));
        RotateArrow(false);
    }

    private void RotateArrow(bool isOpen)
    {
        if (arrow == null) return;
        arrow.DOKill();
        arrow.DOLocalRotate(new Vector3(0f, 0f, isOpen ? 180f : 0f), animDuration);
    }

    private void SetOpenInstant(bool isOpen)
    {
        open = isOpen;
        if (panel == null) return;

        panel.gameObject.SetActive(isOpen);
        panel.localScale = new Vector3(panel.localScale.x, isOpen ? 1f : 0f, panel.localScale.z);
        if (group != null)
        {
            group.alpha = isOpen ? 1f : 0f;
            group.interactable = isOpen;
            group.blocksRaycasts = isOpen;
        }
        if (arrow != null) arrow.localRotation = Quaternion.Euler(0f, 0f, isOpen ? 180f : 0f);
    }
}
