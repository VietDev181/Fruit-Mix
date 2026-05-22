using UnityEngine;

/// <summary>
/// Shows the bottom banner ad while this scene is active, and hides it when the scene unloads. Drop it
/// on any object in a menu scene (Start / Select). Relies on a persistent <see cref="AdMobService"/>
/// (booted from the Start scene). Without the AdMob SDK / USE_ADMOB it's a harmless no-op.
/// </summary>
public class BannerAd : MonoBehaviour
{
    [Tooltip("Hide the banner again when leaving this scene (e.g. before gameplay).")]
    [SerializeField] private bool hideOnDisable = true;

    private IAdService Ads => AdMobService.Instance != null
        ? AdMobService.Instance
        : FindObjectOfType<AdMobService>();

    private void Start()
    {
        var ads = Ads;
        if (ads == null) return;
        ads.Initialize();  // idempotent — boots the SDK if entering this scene directly
        ads.ShowBanner();
    }

    private void OnDisable()
    {
        if (hideOnDisable) Ads?.HideBanner();
    }
}
