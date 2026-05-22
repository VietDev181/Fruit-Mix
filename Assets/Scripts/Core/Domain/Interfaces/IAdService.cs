using System;

/// <summary>
/// App-wide ads abstraction so gameplay code never talks to the AdMob SDK directly (mirrors
/// <see cref="IAudioService"/> / <see cref="ISceneService"/>). The concrete implementation lives in
/// Infrastructure (<c>AdMobService</c>); swap it for a stub in tests or when ads are disabled.
/// </summary>
public interface IAdService
{
    /// <summary>Boot the SDK and pre-load the first ads. Call once at startup.</summary>
    void Initialize();

    // --- Interstitial (full-screen between rounds / on game over) ---------------------------------

    /// <summary>True when an interstitial is loaded and allowed to show (respects the cooldown).</summary>
    bool IsInterstitialReady { get; }

    /// <summary>Pre-load an interstitial so it's ready to show later. Called automatically after each show.</summary>
    void LoadInterstitial();

    /// <summary>Show the interstitial if ready; <paramref name="onClosed"/> always fires (even if no ad
    /// was available) so the caller can resume the game uniformly.</summary>
    void ShowInterstitial(Action onClosed = null);

    // --- App Open (shown when the player returns to the app) --------------------------------------

    /// <summary>Pre-load an App Open ad.</summary>
    void LoadAppOpen();

    /// <summary>Show the App Open ad if one is loaded and nothing else is on screen.</summary>
    void ShowAppOpenIfReady();

    // --- Banner (small persistent ad, e.g. on menu screens) --------------------------------------

    /// <summary>Create + show a bottom banner (loads it if needed). Safe to call repeatedly.</summary>
    void ShowBanner();

    /// <summary>Hide the banner (e.g. when entering gameplay).</summary>
    void HideBanner();

    // --- Rewarded (watch a full ad to earn a reward) ---------------------------------------------

    /// <summary>True when a rewarded ad is loaded and ready to show.</summary>
    bool IsRewardedReady { get; }

    /// <summary>Pre-load a rewarded ad.</summary>
    void LoadRewarded();

    /// <summary>
    /// Show a rewarded ad. <paramref name="onRewardEarned"/> fires ONLY if the player watches enough to
    /// earn the reward; <paramref name="onClosed"/> always fires when the ad is dismissed (or none was
    /// available) so the UI can re-enable itself.
    /// </summary>
    void ShowRewarded(Action onRewardEarned, Action onClosed = null);
}
