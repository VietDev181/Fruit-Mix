using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the available cup shapes and applies the chosen one to the <see cref="CupController"/>.
/// Hook the public methods to UI buttons (a row of cup thumbnails) or to swipe gestures.
/// Selecting a cup resets its contents — meant to be used at the start of a drink.
/// </summary>
public class CupSelector : MonoBehaviour
{
    [SerializeField] private CupController cup;
    [SerializeField] private List<CupDefinition> cups = new List<CupDefinition>();
    [SerializeField] private int startIndex = 0;

    public int CurrentIndex { get; private set; }
    public CupDefinition Current =>
        (cups.Count > 0) ? cups[Mathf.Clamp(CurrentIndex, 0, cups.Count - 1)] : null;

    /// <summary>Raised after a cup is applied. Argument is the new index.</summary>
    public event Action<int> OnCupSelected;

    private void Start()
    {
        if (cups.Count > 0)
            Select(startIndex);
    }

    public void Next() => Select(CurrentIndex + 1);
    public void Prev() => Select(CurrentIndex - 1);

    public void Select(int index)
    {
        if (cups.Count == 0) return;
        CurrentIndex = (index % cups.Count + cups.Count) % cups.Count; // wrap both directions
        cup.SetCup(cups[CurrentIndex]);
        OnCupSelected?.Invoke(CurrentIndex);
    }
}
