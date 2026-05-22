using System;
using UnityEngine;
#if USE_ADMOB
using GoogleMobileAds.Api;
#endif

/// <summary>
/// Google AdMob implementation of <see cref="IAdService"/> (Interstitial + App Open).
///
/// IMPORTANT — this file compiles even before the AdMob SDK is installed: every SDK call is wrapped in
/// <c>#if USE_ADMOB</c>, so without that scripting-define the service is a harmless no-op. Once you've
/// installed the Google Mobile Ads Unity plugin AND added <b>USE_ADMOB</b> to
/// Project Settings ▸ Player ▸ Scripting Define Symbols (Android + iOS), the real ads switch on.
///
/// Ad-unit IDs below are Google's official TEST units while <see cref="useTestAds"/> is on. Turn it
/// off and paste your real unit IDs before shipping. The App ID itself is NOT set here — it goes in
/// Assets ▸ Google Mobile Ads ▸ Settings (or the Android/iOS manifest the plugin generates).
/// </summary>
public class AdMobService : MonoBehaviour, IAdService
{
    [Header("Mode")]
    [Tooltip("Use Google's test ad units. MUST be on during development — real ads on a dev build can " +
             "get your AdMob account banned. Turn off only for release with your own unit IDs filled in.")]
    [SerializeField] private bool useTestAds = true;

    // The platform-specific IDs look "unused" to whichever platform isn't being compiled (the other
    // branch is stripped by #if), so silence CS0414 for these serialized fields.
#pragma warning disable 0414
    [Header("Real ad unit IDs (used only when 'Use Test Ads' is OFF)")]
    [SerializeField] private string androidInterstitialId = "";
    [SerializeField] private string iosInterstitialId = "";
    [SerializeField] private string androidAppOpenId = "";
    [SerializeField] private string iosAppOpenId = "";
#pragma warning restore 0414

    [Header("Cadence")]
    [Tooltip("Minimum seconds between two interstitials so we don't spam the player.")]
    [SerializeField] private float minSecondsBetweenInterstitials = 60f;

    [Header("Debug")]
    [Tooltip("Draw on-screen test buttons in Play mode to fire ads manually. Turn OFF for release.")]
    [SerializeField] private bool showDebugButtons = false;

    // Google's official test unit IDs.
    private const string TestInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";
    private const string TestInterstitialIOS = "ca-app-pub-3940256099942544/4411468910";
    private const string TestAppOpenAndroid = "ca-app-pub-3940256099942544/9257395921";
    private const string TestAppOpenIOS = "ca-app-pub-3940256099942544/5575463023";

    private float lastInterstitialTime = -9999f;

    private string InterstitialUnitId =>
#if UNITY_ANDROID
        useTestAds ? TestInterstitialAndroid : androidInterstitialId;
#elif UNITY_IOS
        useTestAds ? TestInterstitialIOS : iosInterstitialId;
#else
        useTestAds ? TestInterstitialAndroid : androidInterstitialId; // editor fallback
#endif

    private string AppOpenUnitId =>
#if UNITY_ANDROID
        useTestAds ? TestAppOpenAndroid : androidAppOpenId;
#elif UNITY_IOS
        useTestAds ? TestAppOpenIOS : iosAppOpenId;
#else
        useTestAds ? TestAppOpenAndroid : androidAppOpenId;
#endif

#if USE_ADMOB
    private InterstitialAd interstitial;
    private AppOpenAd appOpenAd;
    private bool isShowingFullScreenAd;
    private bool initialized;
#endif

    public bool IsInterstitialReady
    {
        get
        {
#if USE_ADMOB
            return initialized
                   && interstitial != null
                   && interstitial.CanShowAd()
                   && Time.unscaledTime - lastInterstitialTime >= minSecondsBetweenInterstitials;
#else
            return false;
#endif
        }
    }

    public void Initialize()
    {
#if USE_ADMOB
        if (initialized) return;
        MobileAds.Initialize(_ =>
        {
            initialized = true;
            LoadInterstitial();
            LoadAppOpen();
        });
#else
        Debug.Log("[AdMobService] USE_ADMOB not defined — ads are disabled (no-op). " +
                  "Install the AdMob SDK and add USE_ADMOB to Scripting Define Symbols to enable.");
#endif
    }

    // --- Interstitial -----------------------------------------------------------------------------

    public void LoadInterstitial()
    {
#if USE_ADMOB
        interstitial?.Destroy();
        interstitial = null;

        var request = new AdRequest();
        InterstitialAd.Load(InterstitialUnitId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMobService] Interstitial failed to load: {error}");
                return;
            }
            interstitial = ad;
        });
#endif
    }

    public void ShowInterstitial(Action onClosed = null)
    {
#if USE_ADMOB
        if (IsInterstitialReady)
        {
            interstitial.OnAdFullScreenContentClosed += () =>
            {
                isShowingFullScreenAd = false;
                onClosed?.Invoke();
                LoadInterstitial(); // pre-load the next one
            };
            interstitial.OnAdFullScreenContentFailed += _ =>
            {
                isShowingFullScreenAd = false;
                onClosed?.Invoke();
                LoadInterstitial();
            };
            isShowingFullScreenAd = true;
            lastInterstitialTime = Time.unscaledTime;
            interstitial.Show();
            return;
        }

        // Not ready: keep the game flowing and make sure one is loading for next time.
        onClosed?.Invoke();
        LoadInterstitial();
#else
        onClosed?.Invoke();
#endif
    }

    // --- App Open ---------------------------------------------------------------------------------

    public void LoadAppOpen()
    {
#if USE_ADMOB
        appOpenAd?.Destroy();
        appOpenAd = null;

        var request = new AdRequest();
        AppOpenAd.Load(AppOpenUnitId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMobService] App Open failed to load: {error}");
                return;
            }
            appOpenAd = ad;
        });
#endif
    }

    public void ShowAppOpenIfReady()
    {
#if USE_ADMOB
        if (!initialized || isShowingFullScreenAd) return;
        if (appOpenAd == null || !appOpenAd.CanShowAd()) return;

        appOpenAd.OnAdFullScreenContentClosed += () =>
        {
            isShowingFullScreenAd = false;
            LoadAppOpen(); // pre-load the next one
        };
        appOpenAd.OnAdFullScreenContentFailed += _ =>
        {
            isShowingFullScreenAd = false;
            LoadAppOpen();
        };
        isShowingFullScreenAd = true;
        appOpenAd.Show();
#endif
    }

    /// <summary>Show the App Open ad when the player returns to the app (foreground).</summary>
    private void OnApplicationPause(bool paused)
    {
        if (!paused) ShowAppOpenIfReady();
    }

    private void OnDestroy()
    {
#if USE_ADMOB
        interstitial?.Destroy();
        appOpenAd?.Destroy();
#endif
    }

    // --- Debug test buttons (Play mode only; toggle off for release) -------------------------------
    private void OnGUI()
    {
        if (!showDebugButtons) return;

        const float w = 280f, h = 80f, pad = 20f;
        GUIStyle style = new GUIStyle(GUI.skin.button) { fontSize = 28 };

        string interLabel = $"Show Interstitial\n(ready: {IsInterstitialReady})";
        if (GUI.Button(new Rect(pad, pad, w, h), interLabel, style))
            ShowInterstitial(() => Debug.Log("[AdMobService] Interstitial closed."));

        if (GUI.Button(new Rect(pad, pad * 2 + h, w, h), "Show App Open", style))
            ShowAppOpenIfReady();
    }
}
