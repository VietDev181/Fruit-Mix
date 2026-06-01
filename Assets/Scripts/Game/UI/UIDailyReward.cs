using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Panel điểm danh hàng ngày.
///
/// Cách setup trong Unity:
/// 1. panel      => RectTransform của panel chính.
/// 2. closeButton, claimButton => các nút tương ứng.
/// 3. dayText    => hiển thị "Ngày X/7".
/// 4. rewardText => hiển thị số vàng nhận được.
/// 5. statusText => "Nhận ngay!" hoặc "Đã nhận hôm nay".
/// 6. dayHighlights => mảng 7 GameObject đại diện cho từng ngày (index 0-6).
///    Ngày hiện tại sẽ được scale to hơn.
/// </summary>
public class UIDailyReward : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button closeButton;

    [Header("Claim")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;

    [Header("Info Texts")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Day Indicators (7 items)")]
    [Tooltip("Kéo 7 GameObject đại diện Ngày 1..7 vào đây theo thứ tự.")]
    [SerializeField] private GameObject[] dayHighlights;

    [Header("Reward Pop FX")]
    [SerializeField] private GameObject rewardPopFX; // optional particle/animation

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        claimButton.onClick.AddListener(OnClaim);
        if (panel != null) panel.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
        panel.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Close()
    {
        panel.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true)
             .OnComplete(() => gameObject.SetActive(false));
    }

    private void Refresh()
    {
        int dayIdx = DailyRewardSystem.DayIndex;
        int reward = DailyRewardSystem.TodayReward;
        bool canClaim = DailyRewardSystem.CanClaim;

        if (dayText != null) dayText.text = "Ngày " + (dayIdx + 1) + " / 7";
        if (rewardText != null) rewardText.text = "+" + reward + " vàng";
        if (statusText != null) statusText.text = canClaim ? "Nhận ngay!" : "Đã nhận hôm nay\nHẹn gặp lại ngày mai!";
        if (claimButtonText != null) claimButtonText.text = canClaim ? "NHẬN THƯỞNG" : "Đã nhận";

        claimButton.interactable = canClaim;

        // Highlight current day
        if (dayHighlights == null) return;
        for (int i = 0; i < dayHighlights.Length; i++)
        {
            if (dayHighlights[i] == null) continue;
            bool isToday = i == dayIdx;
            dayHighlights[i].transform.DOScale(isToday ? 1.2f : 1f, 0.25f).SetUpdate(true);
        }
    }

    private void OnClaim()
    {
        if (!DailyRewardSystem.TryClaim(out int reward)) return;

        if (rewardText != null)
        {
            rewardText.text = "+" + reward + " vàng";
            rewardText.transform
                .DOPunchScale(Vector3.one * 0.4f, 0.5f, 8, 0.5f)
                .SetUpdate(true);
        }

        if (rewardPopFX != null)
        {
            rewardPopFX.SetActive(false);
            rewardPopFX.SetActive(true);
        }

        Refresh();
    }
}
