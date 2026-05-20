using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class UIStart : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform titleImage;
    [SerializeField] private RectTransform titleText;
    [SerializeField] private Button playButton;

    [Header("Animation Settings")]
    [SerializeField] private float introDuration = 0.8f;
    [SerializeField] private float idleScale = 1.05f;
    [SerializeField] private float idleDuration = 1.5f;
    [SerializeField] private float exitDuration = 0.5f;
    [SerializeField] private float fadeDuration = 0.6f;

    private Sequence introSequence;
    private Sequence idleSequence;
    private bool isTransitioning;

    private CanvasGroup imageGroup;
    private CanvasGroup textGroup;

    private void Awake()
    {
        SetupCanvasGroups();
        playButton.onClick.AddListener(OnPlayClicked);
    }

    private void Start()
    {
        PlayIntroAnimation();
    }

    // =======================
    // Setup
    // =======================

    private void SetupCanvasGroups()
    {
        imageGroup = GetOrAddCanvasGroup(titleImage);
        textGroup = GetOrAddCanvasGroup(titleText);

        imageGroup.alpha = 0;
        textGroup.alpha = 0;
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform target)
    {
        var group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    // =======================
    // Intro Animation
    // =======================

    private void PlayIntroAnimation()
    {
        titleImage.localScale = Vector3.one * 0.8f;
        titleText.localScale = Vector3.one * 0.8f;

        titleImage.anchoredPosition += new Vector2(0, 150f);
        titleText.anchoredPosition -= new Vector2(0, 120f);

        introSequence = DOTween.Sequence();

        introSequence
            .Append(imageGroup.DOFade(1f, introDuration))
            .Join(titleImage.DOAnchorPosY(titleImage.anchoredPosition.y - 150f, introDuration)
                .SetEase(Ease.OutBack))
            .Join(titleImage.DOScale(1f, introDuration)
                .SetEase(Ease.OutBack))

            .AppendInterval(0.1f)

            .Append(textGroup.DOFade(1f, introDuration))
            .Join(titleText.DOAnchorPosY(titleText.anchoredPosition.y + 120f, introDuration)
                .SetEase(Ease.OutBack))
            .Join(titleText.DOScale(1f, introDuration)
                .SetEase(Ease.OutBack))

            .OnComplete(StartIdleAnimation);
    }

    // =======================
    // Idle Animation
    // =======================

    private void StartIdleAnimation()
    {
        idleSequence = DOTween.Sequence();

        idleSequence
            .Append(titleImage.DOScale(idleScale, idleDuration)
                .SetEase(Ease.InOutSine))
            .Join(titleText.DOScale(idleScale, idleDuration)
                .SetEase(Ease.InOutSine))
            .Append(titleImage.DOScale(1f, idleDuration))
            .Join(titleText.DOScale(1f, idleDuration))
            .SetLoops(-1);
    }

    // =======================
    // Exit Animation
    // =======================

    private void OnPlayClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        StopAllAnimations();
        PlayExitAnimation();
    }

    private void PlayExitAnimation()
    {
        Sequence exitSequence = DOTween.Sequence();

        exitSequence
            .Append(titleImage.DOScale(0.8f, exitDuration))
            .Join(titleText.DOScale(0.8f, exitDuration))
            .Join(imageGroup.DOFade(0f, exitDuration))
            .Join(textGroup.DOFade(0f, exitDuration))
            .Append(CreateFadeOverlay())
            .OnComplete(() =>
            {
                SceneManager.LoadScene("GameScene");
            });
    }

    private Tween CreateFadeOverlay()
    {
        var fadeObj = new GameObject("FadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeObj.transform.SetParent(transform.parent, false);

        var image = fadeObj.GetComponent<Image>();
        image.color = new Color(0, 0, 0, 0);

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image.DOFade(1f, fadeDuration);
    }

    // =======================
    // Cleanup
    // =======================

    private void StopAllAnimations()
    {
        introSequence?.Kill();
        idleSequence?.Kill();
    }
}