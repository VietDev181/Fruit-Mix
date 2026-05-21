using UnityEngine;

public class StartBootstrapper : MonoBehaviour
{
    [SerializeField] private AudioService audioService;
    [SerializeField] private AdMobService adServiceMono; // optional: boot + pre-load ads from the start scene

    private void Awake()
    {
        audioService.PlayMainMenuBGM();

        // Boot the SDK here so ads are pre-loaded before the player reaches the game scene.
        // The service is a DontDestroyOnLoad singleton, so it carries over.
        IAdService ads = adServiceMono;
        ads?.Initialize();
    }
}