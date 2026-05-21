using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the recipe loop: show an order (recipe image), let the player build the drink, then the
/// player SHAKES the phone to serve. The drink is scored by type (right cup + right set of
/// ingredients + right set of toppings, ignoring amounts). A result image of the finished cup is
/// shown with a correct/wrong badge. Correct → next recipe; wrong → redo the same recipe. When all
/// recipes are done, returns to the StartScene.
/// </summary>
public class RecipeManager : MonoBehaviour
{
    [Serializable]
    public class RecipeDefinition
    {
        public string id;
        [Tooltip("The order card shown to the player while making this drink.")]
        public Sprite recipeImage;
        [Tooltip("Image of the finished cup, shown on the result screen.")]
        public Sprite resultImage;
        [Tooltip("Required cup id (must match CupDefinition.id). Leave empty to skip cup check.")]
        public string cupId;
        [Tooltip("Required ingredients — drag the ButtonPour objects in. Order ignored.")]
        public ButtonPour[] ingredients;
        [Tooltip("Required toppings — drag the topping PREFABS in. Order & count ignored.")]
        public DraggableTopping[] toppings;
    }

    [Header("Data")]
    [SerializeField] private RecipeDefinition[] recipes;

    [Header("Refs")]
    [SerializeField] private BrewTracker tracker;
    [SerializeField] private CupController cup;

    [Header("UI — order")]
    [Tooltip("Image showing the current recipe/order card.")]
    [SerializeField] private Image recipeImageUI;

    [Header("UI — result")]
    [SerializeField] private GameObject resultPanel;
    [Tooltip("Image of the finished cup shown on the result screen.")]
    [SerializeField] private Image resultImageUI;
    [SerializeField] private GameObject correctBadge;
    [SerializeField] private GameObject wrongBadge;
    [Tooltip("Seconds the result screen stays up before advancing / retrying.")]
    [SerializeField] private float resultDuration = 2f;

    [Header("Shake to serve")]
    [Tooltip("Acceleration magnitude (in g) that counts as a shake on device.")]
    [SerializeField] private float shakeThreshold = 2.2f;
    [Tooltip("Min seconds between shakes so one gesture doesn't fire twice.")]
    [SerializeField] private float shakeCooldown = 1f;
    [Tooltip("Editor/desktop fallback key to serve (no accelerometer).")]
    [SerializeField] private KeyCode editorServeKey = KeyCode.Space;

    [Header("Flow")]
    [Tooltip("Scene to load after all recipes are completed.")]
    [SerializeField] private string startSceneName = "StartScene";

    private int index;
    private bool evaluating;
    private float nextShakeTime;

    private void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("[RecipeManager] No recipes assigned.");
            return;
        }
        ShowRecipe(0);
    }

    private void Update()
    {
        if (evaluating || recipes == null || recipes.Length == 0) return;
        if (ShakeDetected()) Serve();
    }

    private bool ShakeDetected()
    {
        if (Time.unscaledTime < nextShakeTime) return false;

        bool shook = Input.GetKeyDown(editorServeKey) ||
                     Input.acceleration.sqrMagnitude > shakeThreshold * shakeThreshold;
        if (shook) nextShakeTime = Time.unscaledTime + shakeCooldown;
        return shook;
    }

    private void ShowRecipe(int i)
    {
        index = i;
        evaluating = false;

        tracker?.ResetRound();
        cup?.ResetCup();

        if (resultPanel != null) resultPanel.SetActive(false);
        if (recipeImageUI != null) recipeImageUI.sprite = recipes[i].recipeImage;
    }

    /// <summary>Serve the current drink for scoring. Hooked to shake; can also be a UI button.</summary>
    public void Serve()
    {
        if (evaluating) return;
        evaluating = true;
        bool ok = Evaluate(recipes[index]);
        StartCoroutine(ResultRoutine(ok));
    }

    private bool Evaluate(RecipeDefinition r)
    {
        // Cup must match (if specified).
        if (!string.IsNullOrEmpty(r.cupId) && tracker.CupId != r.cupId) return false;

        // Ingredients: exact set of types (no missing, no extra), amounts ignored.
        var needIngredients = new HashSet<string>();
        if (r.ingredients != null)
            foreach (var b in r.ingredients)
                if (b != null && !string.IsNullOrEmpty(b.IngredientId)) needIngredients.Add(b.IngredientId);
        if (!SetMatches(needIngredients, tracker.Ingredients)) return false;

        // Toppings: exact set of types, counts ignored.
        var needToppings = new HashSet<string>();
        if (r.toppings != null)
            foreach (var t in r.toppings)
                if (t != null && !string.IsNullOrEmpty(t.Id)) needToppings.Add(t.Id);
        if (!SetMatches(needToppings, tracker.ToppingIds())) return false;

        return true;
    }

    private static bool SetMatches(HashSet<string> need, IReadOnlyCollection<string> have)
    {
        if (need.Count != have.Count) return false;
        foreach (var s in have) if (!need.Contains(s)) return false;
        return true;
    }

    private IEnumerator ResultRoutine(bool ok)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultImageUI != null) resultImageUI.sprite = recipes[index].resultImage;
        if (correctBadge != null) correctBadge.SetActive(ok);
        if (wrongBadge != null) wrongBadge.SetActive(!ok);

        yield return new WaitForSeconds(resultDuration);

        if (!ok)
        {
            ShowRecipe(index); // wrong → redo the same recipe
        }
        else if (index + 1 < recipes.Length)
        {
            ShowRecipe(index + 1); // correct → next recipe
        }
        else
        {
            Finish(); // all recipes done
        }
    }

    private void Finish()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }
}
