using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Carousel-style "choose a drink" scene: one drink is shown at a time, with left/right arrows to
/// flip between drinks and a confirm button ("DRINK!") to pick the one on screen. Confirming records
/// its recipe id in <see cref="RecipeSelection"/> and loads the GameScene, where
/// <see cref="RecipeManager"/> makes exactly that drink.
///
/// Setup in Unity:
///   1. Add one Choice entry per drink: recipeId (must match a RecipeManager recipe id) + the art
///      Sprite shown in the carousel.
///   2. Assign the drink SpriteRenderer (drinkSprite), the prev/next arrow buttons and the confirm
///      button.
///   3. Put both this scene and the GameScene in Build Settings ▸ Scenes In Build.
/// </summary>
public class RecipeSelectController : MonoBehaviour
{
    [Serializable]
    public class Choice
    {
        [Tooltip("Recipe id to make — must match a RecipeManager.RecipeDefinition.id in the GameScene.")]
        public string recipeId;
        [Tooltip("Big drink art shown in the carousel.")]
        public Sprite art;
        [Tooltip("Decorative sticker shown on top of this drink (one fixed sticker per drink).")]
        public Sprite sticker;
        [Tooltip("Title image shown for this drink (e.g. the drink's name art).")]
        public Sprite title;
        [Tooltip("Gold needed to unlock this drink. 0 = free / always unlocked (e.g. the first drink).")]
        public int unlockCost = 10;
    }

    [Header("Choices")]
    [SerializeField] private Choice[] choices;
    [SerializeField] private int startIndex = 0;
    [Tooltip("Wrap from the last drink back to the first (and vice-versa).")]
    [SerializeField] private bool wrap = true;

    [Header("UI")]
    [Tooltip("SpriteRenderer that shows the current drink's art.")]
    [SerializeField] private SpriteRenderer drinkSprite;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [Tooltip("Confirm button (the \"DRINK!\" button) — picks the drink on screen and starts the game.")]
    [SerializeField] private Button confirmButton;
    [Tooltip("Back-to-home button.")]
    [SerializeField] private Button homeButton;

    [Header("Sticker")]
    [Tooltip("Image that shows the current drink's sticker (set per Choice).")]
    [SerializeField] private Image stickerImage;
    [Tooltip("How far above its resting spot the sticker starts before dropping in.")]
    [SerializeField] private float stickerDropHeight = 600f;
    [Tooltip("Duration of the drop-in animation (seconds).")]
    [SerializeField] private float stickerDropDuration = 0.5f;

    [Header("Title")]
    [Tooltip("Image that shows the current drink's title (set per Choice).")]
    [SerializeField] private Image titleImage;

    [Header("Gold & unlocking")]
    [Tooltip("Text showing the player's gold total.")]
    [SerializeField] private TMP_Text goldText;
    [Tooltip("Object shown over a LOCKED drink (e.g. a lock icon / dim overlay).")]
    [SerializeField] private GameObject lockOverlay;
    [Tooltip("Text on the confirm button: shows the unlock cost while locked, then the drink label once unlocked.")]
    [SerializeField] private TMP_Text unlockCostText;
    [Tooltip("Text shown once the drink is unlocked.")]
    [SerializeField] private string drinkLabel = "DRINK!";
    [Tooltip("Text shown while locked. {0} = unlock cost, e.g. \"Mở khoá ({0})\".")]
    [SerializeField] private string unlockLabel = "{0}";

    [Header("Flow")]
    [Tooltip("Scene loaded after a drink is chosen.")]
    [SerializeField] private string gameSceneName = "GameScene";
    [Tooltip("Scene loaded by the home button.")]
    [SerializeField] private string homeSceneName = "StartScene";

    [Header("Audio (optional)")]
    [SerializeField] private AudioService audioService;

    [Header("VFX")]
    [Tooltip("VFX GameObject/ParticleSystem played when a drink is successfully unlocked. " +
             "It will be activated then deactivated automatically after the effect finishes.")]
    [SerializeField] private ParticleSystem unlockVFX;

    private int index;
    private bool transitioning;
    private Vector2 stickerRestPos;
    private Vector2 titleRestPos;
    private bool stickerRestCaptured;
    private bool titleRestCaptured;

    private void Start()
    {
        if (unlockVFX != null) unlockVFX.gameObject.SetActive(false);

        if (stickerImage != null)
        {
            stickerRestPos = stickerImage.rectTransform.anchoredPosition;
            stickerRestCaptured = true;
        }
        if (titleImage != null)
        {
            titleRestPos = titleImage.rectTransform.anchoredPosition;
            titleRestCaptured = true;
        }

        if (prevButton != null) prevButton.onClick.AddListener(Prev);
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (homeButton != null) homeButton.onClick.AddListener(GoHome);

        if (choices == null || choices.Length == 0)
        {
            Debug.LogWarning("[RecipeSelectController] No choices assigned.");
            return;
        }

        PlayerProgress.OnGoldChanged += RefreshGold;
        RefreshGold(PlayerProgress.Gold);

        index = Mathf.Clamp(startIndex, 0, choices.Length - 1);
        Show(index, animate: false);
    }

    private void OnDestroy() => PlayerProgress.OnGoldChanged -= RefreshGold;

    private bool goldShown;

    private void RefreshGold(int gold)
    {
        if (goldText == null) return;
        goldText.text = gold.ToString();

        // Bump the number when it changes (but not on the very first display at load).
        if (goldShown)
        {
            goldText.transform.DOKill();
            goldText.transform.localScale = Vector3.one;
            goldText.transform.DOPunchScale(Vector3.one * 0.35f, 0.35f, 8, 0.6f);
        }
        goldShown = true;
    }

    /// <summary>A drink is unlocked if it's free (cost ≤ 0) or has been bought before.</summary>
    private bool IsUnlocked(Choice c) =>
        c.unlockCost <= 0 || PlayerProgress.IsUnlocked(c.recipeId);

    [Header("Idle animation")]
    [SerializeField] private float idleFloatAmount = 20f;
    [SerializeField] private float idleFloatDuration = 2f;
    [Tooltip("Float distance specifically for the drink sprite (world units). Smaller = subtler.")]
    [SerializeField] private float drinkFloatAmount = 8f;

    private void UpdateLockUI(Choice c)
    {
        bool locked = !IsUnlocked(c);
        if (lockOverlay != null) lockOverlay.SetActive(locked);
        if (unlockCostText != null)
            unlockCostText.text = locked ? string.Format(unlockLabel, c.unlockCost) : drinkLabel;

        var gray = new Color(0.4f, 0.4f, 0.4f, 1f);

        if (drinkSprite != null)
            drinkSprite.color = locked ? gray : Color.white;
        if (stickerImage != null)
            stickerImage.color = locked ? gray : Color.white;
        if (titleImage != null)
            titleImage.color = locked ? gray : Color.white;

        if (locked) StopIdleAnimations();
    }

    private void StartIdleAnimations()
    {
        // Drink sprite: breathe (scale pulse) + slow float
        if (drinkSprite != null)
        {
            var t = drinkSprite.transform;
            t.DOKill();
            Vector3 base3 = t.localPosition;
            t.DOLocalMoveY(base3.y + drinkFloatAmount, idleFloatDuration)
             .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            t.DOScale(Vector3.one * 1.05f, idleFloatDuration * 0.8f)
             .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        // Sticker: pendulum swing + counter-float (opposite phase)
        if (stickerImage != null && stickerImage.enabled)
        {
            var rt = stickerImage.rectTransform;
            rt.DOKill();
            rt.DOAnchorPosY(stickerRestPos.y - idleFloatAmount * 0.6f, idleFloatDuration * 1.1f)
              .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            rt.DOLocalRotate(new Vector3(0f, 0f, 8f), idleFloatDuration * 0.9f)
              .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        // Title: horizontal sway + subtle scale pulse (different speed for variety)
        if (titleImage != null && titleImage.enabled)
        {
            var rt = titleImage.rectTransform;
            rt.DOKill();
            rt.DOAnchorPosX(titleRestPos.x + idleFloatAmount * 0.5f, idleFloatDuration * 1.3f)
              .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            rt.DOScale(Vector3.one * 1.04f, idleFloatDuration * 1.5f)
              .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void StopIdleAnimations()
    {
        if (drinkSprite != null)
        {
            drinkSprite.transform.DOKill();
            drinkSprite.transform.localScale = Vector3.one;
            drinkSprite.transform.localRotation = Quaternion.identity;
        }
        if (stickerImage != null)
        {
            stickerImage.rectTransform.DOKill();
            stickerImage.rectTransform.localRotation = Quaternion.identity;
            stickerImage.rectTransform.localScale = Vector3.one;
            if (stickerRestCaptured)
                stickerImage.rectTransform.anchoredPosition = stickerRestPos;
        }
        if (titleImage != null)
        {
            titleImage.rectTransform.DOKill();
            titleImage.rectTransform.localScale = Vector3.one;
            if (titleRestCaptured)
                titleImage.rectTransform.anchoredPosition = titleRestPos;
        }
    }

    public void Next() => Step(+1);
    public void Prev() => Step(-1);

    private void Step(int dir)
    {
        if (choices == null || choices.Length == 0) return;

        int next = index + dir;
        if (wrap)
            next = (next % choices.Length + choices.Length) % choices.Length;
        else
            next = Mathf.Clamp(next, 0, choices.Length - 1);

        if (next == index) return;
        index = next;
        audioService?.PlayClickSFX();
        Show(index, animate: true);
    }

    private void Show(int i, bool animate)
    {
        var choice = choices[i];

        // Stop any running idle before applying new state.
        StopIdleAnimations();

        if (drinkSprite != null)
        {
            drinkSprite.sprite = choice.art;
            drinkSprite.enabled = choice.art != null;
            drinkSprite.transform.localRotation = Quaternion.identity;
            if (animate)
            {
                drinkSprite.transform.DOKill();
                drinkSprite.transform.localScale = Vector3.one * 0.9f;
                drinkSprite.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack)
                    .OnComplete(() => { if (IsUnlocked(choice)) StartIdleAnimations(); });
            }
        }

        DropInImage(stickerImage, choice.sticker, animate, ref stickerRestPos, ref stickerRestCaptured);
        DropInImage(titleImage, choice.title, animate, ref titleRestPos, ref titleRestCaptured);
        UpdateLockUI(choice);

        // If not animating, start idle immediately (animate=true waits for tween OnComplete).
        if (!animate && IsUnlocked(choice)) StartIdleAnimations();

        if (!wrap)
        {
            if (prevButton != null) prevButton.interactable = i > 0;
            if (nextButton != null) nextButton.interactable = i < choices.Length - 1;
        }
    }

    /// <summary>
    /// Show <paramref name="sprite"/> on <paramref name="img"/> (hidden if null). When animating, the
    /// image drops in from above its resting spot down to where it sits.
    /// </summary>
    private void DropInImage(Image img, Sprite sprite, bool animate, ref Vector2 restPos, ref bool restCaptured)
    {
        if (img == null) return;

        img.sprite = sprite;
        img.enabled = sprite != null;
        if (sprite == null) return;

        var rt = img.rectTransform;
        rt.DOKill();

        if (!restCaptured)
        {
            restPos = rt.anchoredPosition;
            restCaptured = true;
        }

        if (animate)
        {
            rt.anchoredPosition = restPos + Vector2.up * stickerDropHeight;
            rt.DOAnchorPos(restPos, stickerDropDuration).SetEase(Ease.OutBounce);
        }
        else
        {
            rt.anchoredPosition = restPos;
        }
    }

    private void GoHome()
    {
        if (transitioning) return;
        transitioning = true;

        audioService?.PlayClickSFX();
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    private void Confirm()
    {
        if (transitioning || choices == null || choices.Length == 0) return;

        var choice = choices[index];

        // Locked drink: try to buy it with gold. Not enough → nudge and bail (stay on the screen).
        if (!IsUnlocked(choice))
        {
            if (!PlayerProgress.TrySpendGold(choice.unlockCost))
            {
                audioService?.PlayClickSFX();
                if (unlockCostText != null)
                    unlockCostText.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 8, 0.6f);
                return; // can't afford it yet
            }

            PlayerProgress.Unlock(choice.recipeId);
            UpdateLockUI(choice); // reveal it's now unlocked
            audioService?.PlayClickSFX();
            if (unlockVFX != null)
            {
                unlockVFX.gameObject.SetActive(true);
                unlockVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                unlockVFX.Play();
                StartCoroutine(HideVFXWhenDone(unlockVFX));
            }
            return; // first tap unlocks; tap again to play it
        }

        transitioning = true;
        audioService?.PlayClickSFX();
        RecipeSelection.SelectedId = choice.recipeId;

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    private System.Collections.IEnumerator HideVFXWhenDone(ParticleSystem vfx)
    {
        yield return new WaitUntil(() => vfx == null || !vfx.IsAlive(true));
        if (vfx != null) vfx.gameObject.SetActive(false);
    }
}
