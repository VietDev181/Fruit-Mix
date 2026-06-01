using System;
using UnityEngine;

public static class DailyRewardSystem
{
    private const string LastClaimKey = "daily_last_claim";
    private const string DayIndexKey  = "daily_day_index";

    /// <summary>Gold awarded each day of a 7-day cycle.</summary>
    public static readonly int[] DayRewards = { 50, 60, 75, 100, 125, 150, 200 };

    /// <summary>Current day slot (0-6) shown in the UI.</summary>
    public static int DayIndex => PlayerPrefs.GetInt(DayIndexKey, 0);

    public static bool CanClaim
    {
        get
        {
            string raw = PlayerPrefs.GetString(LastClaimKey, "");
            if (string.IsNullOrEmpty(raw)) return true;
            return DateTime.TryParse(raw, out DateTime last) && DateTime.UtcNow.Date > last.Date;
        }
    }

    /// <summary>Gold amount the player will receive when claiming today.</summary>
    public static int TodayReward
    {
        get
        {
            int idx = DayIndex;
            // Preview reset if streak is broken
            string raw = PlayerPrefs.GetString(LastClaimKey, "");
            if (!string.IsNullOrEmpty(raw) && DateTime.TryParse(raw, out DateTime last))
            {
                if ((DateTime.UtcNow.Date - last.Date).TotalDays > 1)
                    idx = 0;
            }
            return DayRewards[idx % DayRewards.Length];
        }
    }

    /// <summary>Attempt to claim today's reward. Sets <paramref name="reward"/> to the gold earned.</summary>
    public static bool TryClaim(out int reward)
    {
        reward = 0;
        if (!CanClaim) return false;

        int dayIdx = DayIndex;

        string raw = PlayerPrefs.GetString(LastClaimKey, "");
        if (!string.IsNullOrEmpty(raw) && DateTime.TryParse(raw, out DateTime last))
        {
            if ((DateTime.UtcNow.Date - last.Date).TotalDays > 1)
                dayIdx = 0; // streak broken, restart
        }

        reward = DayRewards[dayIdx % DayRewards.Length];
        PlayerProgress.AddGold(reward);

        PlayerPrefs.SetInt(DayIndexKey, (dayIdx + 1) % DayRewards.Length);
        PlayerPrefs.SetString(LastClaimKey, DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
        return true;
    }
}
