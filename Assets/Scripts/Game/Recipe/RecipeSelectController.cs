using System;
using DG.Tweening;
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

    [Header("Flow")]
    [Tooltip("Scene loaded after a drink is chosen.")]
    [SerializeField] private string gameSceneName = "GameScene";
    [Tooltip("Scene loaded by the home button.")]
    [SerializeField] private string homeSceneName = "StartScene";

    [Header("Audio (optional)")]
    [SerializeField] private AudioService audioService;

    private int index;
    private bool transitioning;
    private Vector2 stickerRestPos;
    private Vector2 titleRestPos;
    private bool stickerRestCaptured;
    private bool titleRestCaptured;

    private void Start()
    {
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

        index = Mathf.Clamp(startIndex, 0, choices.Length - 1);
        Show(index, animate: false);
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

        if (drinkSprite != null)
        {
            drinkSprite.sprite = choice.art;
            drinkSprite.enabled = choice.art != null;
            if (animate)
            {
                drinkSprite.transform.DOKill();
                drinkSprite.transform.localScale = Vector3.one * 0.9f;
                drinkSprite.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }
        }

        DropInImage(stickerImage, choice.sticker, animate, ref stickerRestPos, ref stickerRestCaptured);
        DropInImage(titleImage, choice.title, animate, ref titleRestPos, ref titleRestCaptured);

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
        transitioning = true;

        audioService?.PlayClickSFX();
        RecipeSelection.SelectedId = choices[index].recipeId;

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }
}
