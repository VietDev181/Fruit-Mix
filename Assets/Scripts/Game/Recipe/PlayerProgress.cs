using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent player progress: gold earned and which drinks (cups) have been unlocked. Backed by
/// PlayerPrefs so it survives app restarts. Finishing a drink awards gold (see
/// <see cref="RecipeManager"/>); the select scene spends gold to unlock the next drink (see
/// <see cref="RecipeSelectController"/>).
/// </summary>
public static class PlayerProgress
{
    private const string GoldKey = "player_gold";
    private const string UnlockedKey = "unlocked_drinks"; // comma-separated recipe ids

    /// <summary>Raised whenever the gold total changes, with the new amount.</summary>
    public static event Action<int> OnGoldChanged;

    public static int Gold
    {
        get => PlayerPrefs.GetInt(GoldKey, 0);
        private set
        {
            PlayerPrefs.SetInt(GoldKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
            OnGoldChanged?.Invoke(Gold);
        }
    }

    public static void AddGold(int amount)
    {
        if (amount == 0) return;
        Gold += amount;
    }

    /// <summary>Spend gold if there is enough. Returns true on success.</summary>
    public static bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (Gold < amount) return false;
        Gold -= amount;
        return true;
    }

    // --- Unlocks --------------------------------------------------------------------------------

    private static HashSet<string> LoadUnlocked()
    {
        var set = new HashSet<string>();
        string raw = PlayerPrefs.GetString(UnlockedKey, "");
        foreach (var id in raw.Split(','))
            if (!string.IsNullOrEmpty(id)) set.Add(id);
        return set;
    }

    private static void SaveUnlocked(HashSet<string> set)
    {
        PlayerPrefs.SetString(UnlockedKey, string.Join(",", set));
        PlayerPrefs.Save();
    }

    public static bool IsUnlocked(string id) =>
        !string.IsNullOrEmpty(id) && LoadUnlocked().Contains(id);

    public static void Unlock(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var set = LoadUnlocked();
        if (set.Add(id)) SaveUnlocked(set);
    }

#if UNITY_EDITOR
    /// <summary>Editor helper: wipe all progress (gold + unlocks).</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(GoldKey);
        PlayerPrefs.DeleteKey(UnlockedKey);
        PlayerPrefs.Save();
        OnGoldChanged?.Invoke(0);
    }
#endif
}
