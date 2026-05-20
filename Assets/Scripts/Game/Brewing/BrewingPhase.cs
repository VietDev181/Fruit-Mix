/// <summary>
/// The drink-building flow for a single cup. The flow is permissive (sandbox / satisfying-first):
/// players may pour and add toppings in any order; phases mainly gate Stir and Drink so the
/// experience reads as a sequence without ever failing the player.
/// </summary>
public enum BrewingPhase
{
    SelectCup, // empty cup on screen, player can swap cup sprites
    Pour,      // dragging bottles to fill liquid
    Topping,   // dragging toppings into the cup
    Stir,      // swipe to stir / shake
    Drink,     // drag the cup up to sip; liquid drains
    Done       // finished, ready to reset for a new cup
}
