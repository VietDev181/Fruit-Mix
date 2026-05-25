using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
        [Tooltip("Image of the finished drink, shown on the result screen.")]
        public Sprite resultImage;
        [Tooltip("Required ingredients — drag the ButtonPour objects in. Order ignored.")]
        public ButtonPour[] ingredients;
        [Tooltip("Required toppings — drag the topping PREFABS in. Order & count ignored.")]
        public DraggableTopping[] toppings;
    }

    [Header("Data")]
    [SerializeField] private RecipeDefinition[] recipes;

    [Header("Refs")]
    [SerializeField] private BrewTracker tracker;
    [Tooltip("Screen-fill drink container; reset between attempts (liquid + toppings).")]
    [SerializeField] private DrinkContainer container;
    [Tooltip("Tilt-to-drink controller — enabled after a CORRECT result so the player drinks the cup empty.")]
    [SerializeField] private DrinkController drink;
    [Tooltip("Extra pause after the drink is emptied before advancing to the next recipe.")]
    [SerializeField] private float afterDrinkPause = 0.4f;
    [Tooltip("Gold awarded each time a drink is finished (correct + drunk empty).")]
    [SerializeField] private int goldPerDrink = 10;
    [Tooltip("Optional coin-fly burst played when a drink is finished.")]
    [SerializeField] private CoinFlyEffect coinFly;

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
    [Tooltip("Panel shown after the correct badge — tells the player to tilt and enjoy the drink. " +
             "Hide it by default; it is shown automatically when the enjoy phase starts.")]
    [SerializeField] private GameObject enjoyHintPanel;

    [Header("Shake to serve")]
    [Tooltip("Progress bar that fills as the player shakes; serving happens when it reaches the top.")]
    [SerializeField] private Slider shakeProgressBar;
    [Tooltip("Min shake intensity (per-frame change in acceleration) that counts as shaking.")]
    [SerializeField] private float shakeSensitivity = 0.4f;
    [Tooltip("How fast the bar fills while shaking hard (units of bar per second at strong shake).")]
    [SerializeField] private float fillRate = 1.2f;
    [Tooltip("How fast the bar drains per second when not shaking.")]
    [SerializeField] private float drainRate = 0.6f;

    [Header("Shake — topping jiggle")]
    [Tooltip("Per-frame impulse applied to each topping while shaking, so they bounce around.")]
    [SerializeField] private float toppingShakeForce = 0.6f;
    [Tooltip("Liquid slosh added while shaking.")]
    [SerializeField] private float wobbleShakeForce = 0.5f;

    [Tooltip("Editor/desktop fallback key: hold to shake (no accelerometer).")]
    [SerializeField] private KeyCode editorServeKey = KeyCode.Space;

    [Header("Flow")]
    [Tooltip("Scene to load after all recipes are completed.")]
    [SerializeField] private string startSceneName = "StartScene";
    [Tooltip("Scene to return to after the single chosen drink is finished (the select scene). " +
             "Leave empty to fall back to Start Scene.")]
    [SerializeField] private string selectSceneName = "";

    private int index;
    private bool evaluating;
    private float shakeProgress;       // 0..1, fills while shaking
    private Vector3 lastAcceleration;
    private Tween barPulse;            // heartbeat pulse when the bar is near full
    private bool singleRecipeMode; // true when launched from the select scene for one specific drink

    private void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("[RecipeManager] No recipes assigned.");
            return;
        }

        int startIndex = 0;
        if (RecipeSelection.HasSelection)
        {
            int found = IndexOfRecipe(RecipeSelection.SelectedId);
            if (found >= 0)
            {
                startIndex = found;
                singleRecipeMode = true; // make only the chosen drink, then return to the select scene
            }
            else
            {
                Debug.LogWarning($"[RecipeManager] Selected recipe id '{RecipeSelection.SelectedId}' " +
                                 "not found — falling back to the first recipe.");
            }
        }

        ShowRecipe(startIndex);
    }

    private int IndexOfRecipe(string id)
    {
        for (int i = 0; i < recipes.Length; i++)
            if (recipes[i] != null && recipes[i].id == id) return i;
        return -1;
    }

    private void Update()
    {
        if (evaluating || recipes == null || recipes.Length == 0) return;

        float shake = CurrentShake();
        float dt = Time.unscaledDeltaTime;

        if (shake >= shakeSensitivity)
        {
            if (IsRecipeReady())
                shakeProgress += fillRate * Mathf.Min(shake, 3f) * dt;
            JiggleToppings(shake);
        }
        else
        {
            shakeProgress -= drainRate * dt;
        }

        shakeProgress = Mathf.Clamp01(shakeProgress);
        UpdateShakeBar();

        if (shakeProgress >= 1f) Serve();
    }

    /// <summary>Show the bar only while there is shake progress; hidden (empty) otherwise.</summary>
    private void UpdateShakeBar()
    {
        if (shakeProgressBar == null) return;
        shakeProgressBar.value = shakeProgress;
        bool show = shakeProgress > 0.001f;
        if (shakeProgressBar.gameObject.activeSelf != show)
            shakeProgressBar.gameObject.SetActive(show);

        // Heartbeat pulse once the bar is almost full, to signal "keep shaking, almost there!".
        bool nearFull = shakeProgress >= 0.85f;
        if (nearFull && barPulse == null)
        {
            barPulse = shakeProgressBar.transform
                .DOScale(1.08f, 0.22f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }
        else if (!nearFull && barPulse != null)
        {
            barPulse.Kill();
            barPulse = null;
            shakeProgressBar.transform.localScale = Vector3.one;
        }
    }

    /// <summary>Pop the result badge in with a little bounce; the wrong badge also shakes.</summary>
    private void PopBadge(GameObject badge, bool ok)
    {
        if (badge == null) return;
        Transform t = badge.transform;
        t.DOKill();
        t.localScale = Vector3.zero;
        t.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        if (!ok)
            t.DOShakeRotation(0.5f, new Vector3(0f, 0f, 18f), 10, 90f).SetUpdate(true);
    }

    /// <summary>Shake intensity this frame: how much the device acceleration changed (or the key).</summary>
    private float CurrentShake()
    {
        if (Input.GetKey(editorServeKey)) return 3f; // editor: hold to shake at full strength

        Vector3 acc = Input.acceleration;
        float jerk = (acc - lastAcceleration).magnitude;
        lastAcceleration = acc;
        return jerk;
    }

    /// <summary>Bounce the liquid and every topping while shaking, so the drink visibly rattles.</summary>
    private void JiggleToppings(float shake)
    {
        if (container == null) return;
        container.Wobble?.AddImpulse(wobbleShakeForce * Mathf.Min(shake, 2f) * Time.unscaledDeltaTime * 10f);

        Transform parent = container.ToppingContainer;
        if (parent == null) return;
        float mag = toppingShakeForce * Mathf.Min(shake, 3f);
        foreach (var t in parent.GetComponentsInChildren<ToppingBuoyancy>())
        {
            Vector2 dir = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-0.3f, 1f));
            t.AddStirImpulse(dir.normalized * mag * Time.unscaledDeltaTime * 60f);
        }
    }

    private void ShowRecipe(int i)
    {
        index = i;
        evaluating = false;
        shakeProgress = 0f;
        if (barPulse != null) { barPulse.Kill(); barPulse = null; }
        if (shakeProgressBar != null)
        {
            shakeProgressBar.transform.localScale = Vector3.one;
            shakeProgressBar.value = 0f;
            shakeProgressBar.gameObject.SetActive(false);
        }

        tracker?.ResetRound();
        container?.ResetContainer(); // clear liquid + toppings for a fresh attempt

        if (resultPanel != null) resultPanel.SetActive(false);
        if (recipeImageUI != null) recipeImageUI.sprite = recipes[i].recipeImage;
    }

    /// <summary>Serve the current drink for scoring. Hooked to shake; can also be a UI button.</summary>
    public void Serve()
    {
        if (evaluating) return;
        evaluating = true;
        shakeProgress = 0f;
        if (barPulse != null) { barPulse.Kill(); barPulse = null; }
        if (shakeProgressBar != null)
        {
            shakeProgressBar.transform.localScale = Vector3.one;
            shakeProgressBar.value = 0f;
            shakeProgressBar.gameObject.SetActive(false);
        }
        bool ok = Evaluate(recipes[index]);
        StartCoroutine(ResultRoutine(ok));
    }

    /// <summary>True when all required ingredients and toppings for the current recipe are present.</summary>
    private bool IsRecipeReady()
    {
        if (tracker == null || index >= recipes.Length) return false;
        var r = recipes[index];

        if (r.ingredients != null)
        {
            var have = new HashSet<string>(tracker.Ingredients);
            foreach (var b in r.ingredients)
                if (b != null && !string.IsNullOrEmpty(b.IngredientId) && !have.Contains(b.IngredientId))
                    return false;
        }

        if (r.toppings != null && r.toppings.Length > 0)
        {
            var haveToppings = tracker.ToppingIds();
            foreach (var t in r.toppings)
                if (t != null && !string.IsNullOrEmpty(t.Id) && !haveToppings.Contains(t.Id))
                    return false;
        }

        return true;
    }

    private bool Evaluate(RecipeDefinition r)
    {
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
        PopBadge(ok ? correctBadge : wrongBadge, ok);

        if (!ok)
        {
            yield return new WaitForSeconds(resultDuration);
            ShowRecipe(index); // wrong → redo the same recipe
            yield break;
        }

        // Correct → show the enjoy hint, then let the player tilt to drain the drink.
        yield return new WaitForSeconds(resultDuration);
        if (correctBadge != null) correctBadge.SetActive(false);
        if (enjoyHintPanel != null) enjoyHintPanel.SetActive(true);
        yield return StartCoroutine(EnjoyRoutine());
        if (enjoyHintPanel != null) enjoyHintPanel.SetActive(false);

        // Reward the finished drink with gold (used to unlock more drinks in the select scene).
        PlayerProgress.AddGold(goldPerDrink);

        // Celebrate: fly coins to the wallet before leaving the scene.
        if (coinFly != null)
        {
            coinFly.Play();
            yield return new WaitForSeconds(coinFly.TotalDuration);
        }

        if (singleRecipeMode)
            Finish(); // the chosen drink is done → back to the select scene
        else if (index + 1 < recipes.Length)
            ShowRecipe(index + 1); // next recipe
        else
            Finish(); // all recipes done
    }

    /// <summary>Enable tilt-to-drink and wait until the drink is emptied.</summary>
    private IEnumerator EnjoyRoutine()
    {
        if (drink != null)
        {
            drink.ResetDrink();
            drink.SetActive(true);
        }

        // Wait until the liquid is drained (or there was nothing to drink).
        while (container != null && container.Liquid != null && !container.Liquid.IsEmpty)
            yield return null;

        yield return new WaitForSeconds(afterDrinkPause);
        if (drink != null) drink.SetActive(false);
    }

    private void Finish()
    {
        Time.timeScale = 1f;
        RecipeSelection.Clear();

        // Single-drink runs (launched from the select scene) return there; otherwise go to Start.
        string scene = singleRecipeMode
            ? (!string.IsNullOrEmpty(selectSceneName) ? selectSceneName : "SelectScene")
            : startSceneName;
        SceneManager.LoadScene(scene);
    }
}
