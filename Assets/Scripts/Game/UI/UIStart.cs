using System.Collections;
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

    [Header("Floating Fruits")]
    [Tooltip("Các sprite hoa quả sẽ bay lên từ dưới màn hình. Kéo sprite vào đây.")]
    [SerializeField] private Sprite[] fruitSprites;
    [Tooltip("Parent RectTransform chứa các hoa quả sinh ra (tạo 1 child Panel/RectTransform trống đặt sau background).")]
    [SerializeField] private RectTransform fruitContainer;
    [SerializeField] private float fruitSpawnInterval = 1.2f;
    [SerializeField] private float fruitFloatDuration = 5f;
    [SerializeField] private float fruitSizeMin = 60f;
    [SerializeField] private float fruitSizeMax = 130f;
    [Range(0f, 1f)]
    [SerializeField] private float fruitMaxAlpha = 0.75f;

    [Header("Play Button Pulse")]
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.65f;

    [Header("Parallax (Gyro / Tilt)")]
    [Tooltip("Layer background cần dịch chuyển khi nghiêng máy. Tạo 1 RectTransform con chứa background image, set anchor = center.")]
    [SerializeField] private RectTransform parallaxLayer;
    [Tooltip("Biên độ dịch chuyển tối đa (pixels) khi nghiêng hết góc.")]
    [SerializeField] private float parallaxStrength = 45f;
    [SerializeField] private float parallaxSmoothing = 7f;

    private Sequence introSequence;
    private Sequence idleSequence;
    private Sequence pulseSequence;
    private bool isTransitioning;

    private CanvasGroup imageGroup;
    private CanvasGroup textGroup;

    private RectTransform canvasRect;
    private Vector2 parallaxTarget;
    private Vector2 parallaxCurrent;

    private void Awake()
    {
        SetupCanvasGroups();
        playButton.onClick.AddListener(OnPlayClicked);
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        Input.gyro.enabled = true;
    }

    private void Start()
    {
        PlayIntroAnimation();
        if (fruitSprites != null && fruitSprites.Length > 0 && fruitContainer != null)
            StartCoroutine(SpawnFruitsLoop());
    }

    private void Update()
    {
        UpdateParallax();
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
        if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
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
            .Join(titleImage.DOAnchorPosY(titleImage.anchoredPosition.y - 150f, introDuration).SetEase(Ease.OutBack))
            .Join(titleImage.DOScale(1f, introDuration).SetEase(Ease.OutBack))
            .AppendInterval(0.1f)
            .Append(textGroup.DOFade(1f, introDuration))
            .Join(titleText.DOAnchorPosY(titleText.anchoredPosition.y + 120f, introDuration).SetEase(Ease.OutBack))
            .Join(titleText.DOScale(1f, introDuration).SetEase(Ease.OutBack))
            .OnComplete(() =>
            {
                StartIdleAnimation();
                StartButtonPulse();
            });
    }

    // =======================
    // Idle Animation
    // =======================

    private void StartIdleAnimation()
    {
        idleSequence = DOTween.Sequence();
        idleSequence
            .Append(titleImage.DOScale(idleScale, idleDuration).SetEase(Ease.InOutSine))
            .Join(titleText.DOScale(idleScale, idleDuration).SetEase(Ease.InOutSine))
            .Append(titleImage.DOScale(1f, idleDuration).SetEase(Ease.InOutSine))
            .Join(titleText.DOScale(1f, idleDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1);
    }

    // =======================
    // Play Button Pulse
    // =======================

    private void StartButtonPulse()
    {
        var btnRect = playButton.GetComponent<RectTransform>();
        pulseSequence = DOTween.Sequence();
        pulseSequence
            .Append(btnRect.DOScale(pulseScale, pulseDuration).SetEase(Ease.InOutSine))
            .Append(btnRect.DOScale(1f, pulseDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1);
    }

    // =======================
    // Floating Fruits
    // =======================

    private IEnumerator SpawnFruitsLoop()
    {
        // Stagger lần đầu để không spawn cùng lúc
        yield return new WaitForSeconds(0.3f);
        while (!isTransitioning)
        {
            SpawnFruit();
            yield return new WaitForSeconds(fruitSpawnInterval);
        }
    }

    private void SpawnFruit()
    {
        var sprite = fruitSprites[Random.Range(0, fruitSprites.Length)];

        var go = new GameObject("FloatFruit", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(fruitContainer, false);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;

        var rect = go.GetComponent<RectTransform>();
        float size = Random.Range(fruitSizeMin, fruitSizeMax);
        rect.sizeDelta = new Vector2(size, size);

        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;
        float startX = Random.Range(-halfW * 0.8f, halfW * 0.8f);
        float driftX = Random.Range(-80f, 80f);

        rect.anchoredPosition = new Vector2(startX, -halfH - size);

        float duration = Random.Range(fruitFloatDuration * 0.8f, fruitFloatDuration * 1.3f);
        float rotation = Random.Range(-270f, 270f);
        float targetY = halfH + size + 50f;

        var cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        var seq = DOTween.Sequence();
        // Bay lên + drift ngang nhẹ + xoay
        seq.Append(rect.DOAnchorPos(new Vector2(startX + driftX, targetY), duration).SetEase(Ease.Linear));
        seq.Join(rect.DORotate(new Vector3(0f, 0f, rotation), duration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        // Fade in nhanh, fade out ở 70% cuối
        seq.Join(cg.DOFade(fruitMaxAlpha, duration * 0.15f));
        seq.Insert(duration * 0.7f, cg.DOFade(0f, duration * 0.3f));
        seq.OnComplete(() => Destroy(go));
    }

    // =======================
    // Parallax (Gyro / Tilt)
    // =======================

    private void UpdateParallax()
    {
        if (parallaxLayer == null) return;

        // Input.acceleration = gia tốc kế (hoạt động cả khi không có gyro riêng)
        Vector2 tilt = new Vector2(Input.acceleration.x, Input.acceleration.y);
        tilt = Vector2.ClampMagnitude(tilt, 1f);

        parallaxTarget = tilt * parallaxStrength;
        parallaxCurrent = Vector2.Lerp(parallaxCurrent, parallaxTarget, Time.deltaTime * parallaxSmoothing);
        parallaxLayer.anchoredPosition = parallaxCurrent;
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
            .OnComplete(() => SceneManager.LoadScene("SelectScene"));
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
        pulseSequence?.Kill();
    }
}
