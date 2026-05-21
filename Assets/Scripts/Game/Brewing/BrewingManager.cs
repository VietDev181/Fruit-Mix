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
    [SerializeField] private CupSelector cupSelector;
    [SerializeField] private CupController cup;
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

    [Header("Testing")]
    [Tooltip("Skip the SelectCup gate and open the mixing phase immediately on Play, so you can " +
             "pour/add toppings without wiring a Start button. Turn off for the real flow.")]
    [SerializeField] private bool autoStartMixing = false;

    public BrewingPhase Phase { get; private set; } = BrewingPhase.SelectCup;

    /// <summary>Raised whenever the phase changes — hook UI hints / button visibility here.</summary>
    public event Action<BrewingPhase> OnPhaseChanged;

    private IGameService gameService; // optional, for score

    private void Start()
    {
        if (drink != null) drink.OnEmptied += HandleEmptied;
        SetPhase(BrewingPhase.SelectCup);

        if (autoStartMixing)
            BeginMixing(); // testing shortcut: bottles + toppings + stir active right away
    }

    /// <summary>Optional injection from the bootstrapper so finishing a drink awards score.</summary>
    public void BindGameService(IGameService service) => gameService = service;

    // --- UI-facing transitions --------------------------------------------------------------------

    /// <summary>Confirm the chosen cup and open the free mixing phase (pour + topping + stir).</summary>
    public void BeginMixing()
    {
        if (drink != null) drink.CaptureHome();
        SetPhase(BrewingPhase.Pour);
    }

    /// <summary>Switch to the drinking phase. Only meaningful once there is liquid in the cup.</summary>
    public void BeginDrinking()
    {
        if (cup != null && cup.Liquid.IsEmpty) return; // nothing to drink yet
        SetPhase(BrewingPhase.Drink);
    }

    /// <summary>Reset everything and start a fresh cup.</summary>
    public void StartNewCup()
    {
        drink?.ResetDrink();
        cup?.ResetCup();
        SetPhase(BrewingPhase.SelectCup);
    }

    // --- Internals --------------------------------------------------------------------------------

    private void HandleEmptied()
    {
        gameService?.AddScore(scorePerDrink);
        SetPhase(BrewingPhase.Done);
    }

    private void SetPhase(BrewingPhase phase)
    {
        Phase = phase;
        ConfigureInteractions(phase);
        OnPhaseChanged?.Invoke(phase);
    }

    private void ConfigureInteractions(BrewingPhase phase)
    {
        bool canMix = phase == BrewingPhase.Pour ||
                      phase == BrewingPhase.Topping ||
                      phase == BrewingPhase.Stir;
        bool canDrink = phase == BrewingPhase.Drink;

        // NOTE: don't toggle cupSelector.enabled — disabling a component before its Start() runs
        // skips Start() entirely, which would skip the initial SetCup() (mask sprite + cavity).
        // Cup-swap buttons are gated by phase elsewhere if needed; selection still works via methods.

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
    }
}
