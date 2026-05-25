using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1 slot duy nhất hiện nguyên liệu cần thêm tiếp theo.
/// Khi người chơi thêm đúng nguyên liệu → slot animate out → đổi nội dung → animate in.
/// </summary>
public class RecipeIngredientUI : MonoBehaviour
{
    [Header("Single slot")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image icon;

    [Header("Anim")]
    [SerializeField] private float animDuration = 0.25f;

    private struct Entry { public string id; public Sprite sprite; public Color color; }

    private readonly List<Entry> entries = new List<Entry>();
    private readonly HashSet<string> done = new HashSet<string>();

    // ------------------------------------------------------------------

    public void SetRecipe(RecipeManager.RecipeDefinition recipe)
    {
        entries.Clear();
        done.Clear();

        if (recipe.toppings != null)
            foreach (var dt in recipe.toppings)
                if (dt != null && !string.IsNullOrEmpty(dt.Id))
                    entries.Add(new Entry { id = dt.Id, sprite = dt.Icon, color = Color.white });

        if (recipe.ingredients != null)
            foreach (var bp in recipe.ingredients)
                if (bp != null && !string.IsNullOrEmpty(bp.IngredientId))
                    entries.Add(new Entry { id = bp.IngredientId, sprite = null, color = bp.LiquidColor });

        Debug.Log($"[RecipeIngredientUI] SetRecipe: {entries.Count} entries");
        foreach (var e in entries) Debug.Log($"  → id='{e.id}'");

        if (root != null) { root.transform.DOKill(); root.transform.localScale = Vector3.zero; }
        ShowNext(animate: false);
    }

    public void NotifyIngredientAdded(string id) => TryAdvance(id);
    public void NotifyToppingAdded(string id)    => TryAdvance(id);

    // ------------------------------------------------------------------

    private void TryAdvance(string id)
    {
        Debug.Log($"[RecipeIngredientUI] TryAdvance id='{id}' entries={entries.Count} done={done.Count}");
        if (string.IsNullOrEmpty(id)) return;
        if (!ContainsId(id) || done.Contains(id)) return;

        done.Add(id);

        root?.transform.DOKill();
        root?.transform.DOScale(0f, animDuration).SetEase(Ease.InBack)
            .OnComplete(() => ShowNext(animate: true));
    }

    // Find first entry not yet done and show it.
    private void ShowNext(bool animate)
    {
        Entry? next = null;
        foreach (var e in entries)
            if (!done.Contains(e.id)) { next = e; break; }

        if (next == null)
        {
            if (root != null) root.SetActive(false);
            return;
        }

        if (icon != null)
        {
            icon.sprite = next.Value.sprite;
            icon.color  = next.Value.sprite != null ? Color.white : next.Value.color;
        }

        if (root != null)
        {
            root.SetActive(true);
            root.transform.DOKill();
            if (animate)
                root.transform.DOScale(1f, animDuration).SetEase(Ease.OutBack);
            else
                root.transform.localScale = Vector3.one;
        }
    }

    private bool ContainsId(string id)
    {
        foreach (var e in entries) if (e.id == id) return true;
        return false;
    }
}
