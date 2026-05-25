using System;
using UnityEngine;

/// <summary>
/// Orchestrates the drink-building flow and enables/disables each interaction set per phase. The
/// flow is deliberately permissive (satisfying-first): once a cup is chosen the player can pour,
/// add toppings and stir freely; a "Drink" action switches to the drinking phase, and emptying the
/// cup finishes the round. UI buttons drive the transitions via the public methods.
///
/// Self-wiring: drop this on a scene object and assign the references in the inspector. Optionally
/// hand it the app's IGameService (from GameBootstrapper) to award score when a drink is finished.
/// </summary>
public class BrewingManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private DrinkContainer container;
    [SerializeField] private ButtonPour[] pourButtons;
    [SerializeField] private ToppingSpawner[] toppingSpawners;
    [Tooltip("New tap-to-drop topping menu (replaces the drag-from-tray spawners).")]
    [SerializeField] private ToppingMenu toppingMenu;
    [SerializeField] private StirController stir;
    [SerializeField] private DrinkController drink;
    [SerializeField] private BrewingAudio brewAudio;

    [Header("Scoring (optional)")]
    [Tooltip("Score granted to the app GameService each time a drink is finished.")]
    [SerializeField] private int scorePerDrink = 10;

    [Header("Shake hint")]
    [Tooltip("UI object (e.g. an animated 'shake to mix' label) shown once enough liquid is in the drink.")]
    [SerializeField] private GameObject shakeHintUI;
    [Tooltip("Liquid fill fraction (0..1) that triggers the shake hint.")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float minFillToShowHint = 0.15f;

    public BrewingPhase Phase { get; private set; } = BrewingPhase.Pour;

    /// <summary>Raised whenever the phase changes — hook UI hints / button visibility here.</summary>
    public event Action<BrewingPhase> OnPhaseChanged;

    private IGameService gameService; // optional, for score

    private void Start()
    {
        if (drink != null) drink.OnEmptied += HandleEmptied;
        if (container?.Liquid != null) container.Liquid.OnFillChanged += HandleFillChanged;
        if (shakeHintUI != null) shakeHintUI.SetActive(false);
        // No cup to choose — open the free mixing phase right away (pour + topping + stir).
        BeginMixing();
    }

    /// <summary>Optional injection from the bootstrapper so finishing a drink awards score.</summary>
    public void BindGameService(IGameService service) => gameService = service;

    // --- UI-facing transitions --------------------------------------------------------------------

    /// <summary>Open the free mixing phase (pour + topping + stir).</summary>
    public void BeginMixing()
    {
        if (drink != null) drink.CaptureHome();
        SetPhase(BrewingPhase.Pour);
    }

    /// <summary>Switch to the drinking phase. Only meaningful once there is liquid in the drink.</summary>
    public void BeginDrinking()
    {
        if (container != null && container.Liquid != null && container.Liquid.IsEmpty) return; // nothing to drink yet
        SetPhase(BrewingPhase.Drink);
    }

    /// <summary>Reset everything and start a fresh drink.</summary>
    public void StartNewCup()
    {
        drink?.ResetDrink();
        container?.ResetContainer();
        SetPhase(BrewingPhase.Pour);
    }

    // --- Internals --------------------------------------------------------------------------------

    private void HandleEmptied()
    {
        gameService?.AddScore(scorePerDrink);
        SetPhase(BrewingPhase.Done);
    }

    private void HandleFillChanged(float fill) => RefreshShakeHint();

    /// <summary>Call this after a topping is dropped so the hint re-evaluates.</summary>
    public void NotifyToppingAdded() => RefreshShakeHint();

    private void RefreshShakeHint()
    {
        if (shakeHintUI == null) return;
        bool canShow = Phase != BrewingPhase.Drink && Phase != BrewingPhase.Done;
        bool hasLiquid = container?.Liquid != null && container.Liquid.Fill >= minFillToShowHint;
        bool hasTopping = container?.ToppingContainer != null && container.ToppingContainer.childCount > 0;
        shakeHintUI.SetActive(canShow && hasLiquid && hasTopping);
    }

    private void SetPhase(BrewingPhase phase)
    {
        Phase = phase;
        ConfigureInteractions(phase);
        OnPhaseChanged?.Invoke(phase);
        // Hide shake hint once the player moves to drinking/done.
        if (shakeHintUI != null && (phase == BrewingPhase.Drink || phase == BrewingPhase.Done))
            shakeHintUI.SetActive(false);
    }

    private void ConfigureInteractions(BrewingPhase phase)
    {
        bool canMix = phase == BrewingPhase.Pour ||
                      phase == BrewingPhase.Topping ||
                      phase == BrewingPhase.Stir;
        bool canDrink = phase == BrewingPhase.Drink;

        if (pourButtons != null)
            foreach (var b in pourButtons) if (b != null) b.SetInteractable(canMix);

        if (toppingSpawners != null)
            foreach (var s in toppingSpawners) if (s != null) s.SetInteractable(canMix);

        toppingMenu?.SetInteractable(canMix);

        stir?.SetActive(canMix);
        drink?.SetActive(canDrink);
    }

    private void OnDestroy()
    {
        if (drink != null) drink.OnEmptied -= HandleEmptied;
        if (container?.Liquid != null) container.Liquid.OnFillChanged -= HandleFillChanged;
    }
}
