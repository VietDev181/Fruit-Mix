using UnityEngine;

public class StartBootstrapper : MonoBehaviour
{
    [SerializeField] private AudioService audioService;

    private void Awake()
    {
        audioService.PlayMainMenuBGM();
    }
}