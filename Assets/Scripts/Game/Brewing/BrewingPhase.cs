/// <summary>
/// The drink-building flow (cup-less, screen-fill). The flow is permissive (sandbox /
/// satisfying-first): players may pour and add toppings in any order; phases mainly gate Stir and
/// Drink so the experience reads as a sequence without ever failing the player.
/// </summary>
public enum BrewingPhase
{
    Pour,      // tap pour buttons to fill the screen with liquid
    Topping,   // dropping toppings into the drink
    Stir,      // swipe to stir / shake
    Drink,     // tilt the phone to drink; liquid drains
    Done       // finished, ready to reset for a new drink
}
