/// <summary>
/// Carries the player's chosen recipe from the select scene into the GameScene. Static so it
/// survives the scene load without needing a DontDestroyOnLoad object. The select scene sets
/// <see cref="SelectedId"/>; <see cref="RecipeManager"/> reads it on start to know which drink to make.
/// </summary>
public static class RecipeSelection
{
    /// <summary>Id of the recipe the player tapped in the select scene, or null/empty if none.</summary>
    public static string SelectedId { get; set; }

    /// <summary>True when a recipe was picked in the select scene.</summary>
    public static bool HasSelection => !string.IsNullOrEmpty(SelectedId);

    /// <summary>Forget the current selection (call after the chosen drink is finished).</summary>
    public static void Clear() => SelectedId = null;
}
