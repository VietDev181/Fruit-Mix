using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Watch an ad for a reward" button. Tapping it shows a rewarded ad; the gold is granted ONLY if the
/// player watches it to the end (the SDK's reward callback). The button is disabled while the ad is on
/// screen and (optionally) hidden when no ad is available, so the player can't farm a broken button.
///
/// Setup: put on (or assign) a UI Button in the Select scene, set the reward amount, and make sure a
/// persistent <see cref="AdMobService"/> exists (booted from the Start scene).
/// </summary>
public class RewardedAdButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [Tooltip("Gold granted when the player finishes watching the ad.")]
    [SerializeField] private int rewardGold = 20;
    [Tooltip("Optional coin-fly burst played when the reward lands.")]
    [SerializeField] private CoinFlyEffect coinFly;
    [Tooltip("Hide the button while no rewarded ad is loaded.")]
    [SerializeField] private bool hideWhenNotReady = true;

    private IAdService Ads => AdMobService.Instance != null
        ? AdMobService.Instance
        : FindObjectOfType<AdMobService>();

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Watch);
    }

    private void Update()
    {
        if (!hideWhenNotReady || button == null) return;
        bool ready = Ads != null && Ads.IsRewardedReady;
        if (button.gameObject.activeSelf != ready)
            button.gameObject.SetActive(ready);
    }

    public void Watch()
    {
        var ads = Ads;
        if (ads == null) return;

        if (button != null) button.interactable = false;

        ads.ShowRewarded(
            onRewardEarned: () =>
            {
                PlayerProgress.AddGold(rewardGold); // only fires after a full watch
                if (coinFly != null) coinFly.Play();
            },
            onClosed: () =>
            {
                if (button != null) button.interactable = true;
            });
    }
}
