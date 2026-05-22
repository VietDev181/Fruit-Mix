using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Collects what the player has put into the current drink so <see cref="RecipeManager"/> can score
/// it: the set of ingredient ids poured, and the set of topping ids dropped in. Ingredients are
/// recorded as bottles start pouring; toppings are read live at evaluation time. Call
/// <see cref="ResetRound"/> when a new recipe starts.
/// </summary>
public class BrewTracker : MonoBehaviour
{
    [SerializeField] private DrinkContainer container;

    private readonly HashSet<string> pouredIngredients = new HashSet<string>();

    private void Awake()
    {
        // Auto-subscribe to every pour button in the scene so pours get recorded.
        foreach (var button in FindObjectsOfType<ButtonPour>(true))
            button.OnPourStarted += RecordIngredient;
    }

    private void RecordIngredient(string id)
    {
        if (!string.IsNullOrEmpty(id)) pouredIngredients.Add(id);
    }

    /// <summary>Distinct ingredient ids poured so far this round.</summary>
    public IReadOnlyCollection<string> Ingredients => pouredIngredients;

    /// <summary>Distinct topping ids currently in the drink.</summary>
    public HashSet<string> ToppingIds()
    {
        var set = new HashSet<string>();
        Transform c = container != null ? container.ToppingContainer : null;
        if (c != null)
        {
            for (int i = 0; i < c.childCount; i++)
            {
                var t = c.GetChild(i).GetComponent<DraggableTopping>();
                if (t != null && !string.IsNullOrEmpty(t.Id)) set.Add(t.Id);
            }
        }
        return set;
    }

    /// <summary>Clear the poured-ingredient record for a fresh recipe.</summary>
    public void ResetRound() => pouredIngredients.Clear();
}
