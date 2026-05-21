using UnityEngine;

/// <summary>
/// ASMR sound layer for the brewing gameplay. Kept separate from the app-wide IAudioService (which
/// handles BGM + UI clicks) so it can own dedicated looping sources for the pour and sip streams.
///
/// Looping sounds (pour, sip) use two reserved AudioSources whose volume fades in/out for smooth
/// starts and stops. One-shots (plop, stir, gulp, bubble) play with slight random pitch so repeated
/// taps never sound mechanical. Route every source through the SFX mixer group so the settings
/// slider still controls them.
/// </summary>
public class BrewingAudio : MonoBehaviour
{
    [Header("Looping sources")]
    [SerializeField] private AudioSource pourSource;
    [SerializeField] private AudioSource sipSource;
    [SerializeField] private float loopFade = 0.12f;

    [Header("One-shot source")]
    [SerializeField] private AudioSource oneShotSource;

    [Header("Loop clips")]
    [SerializeField] private AudioClip pourLoop;
    [SerializeField] private AudioClip sipLoop;

    [Header("One-shot clips")]
    [SerializeField] private AudioClip plop;
    [SerializeField] private AudioClip stir;
    [SerializeField] private AudioClip finalGulp;
    [SerializeField] private AudioClip bubble;

    [Header("Feel")]
    [SerializeField] private Vector2 pitchJitter = new Vector2(0.92f, 1.08f);

    [Tooltip("Fill any empty clip slot above with a procedurally synthesised sound (see " +
             "ProceduralBrewingSfx). A clip assigned by hand always takes priority.")]
    [SerializeField] private bool synthesizeMissingClips = true;

    private float pourTargetVol, sipTargetVol;

    private void Awake()
    {
        if (synthesizeMissingClips) SynthesizeMissingClips();
        ConfigureLoop(pourSource, pourLoop);
        ConfigureLoop(sipSource, sipLoop);
    }

    /// <summary>Generate stand-in clips for any slot left empty, so the game has full ASMR audio
    /// even before real recordings are dropped in.</summary>
    private void SynthesizeMissingClips()
    {
        if (pourLoop == null) pourLoop = ProceduralBrewingSfx.PourLoop();
        if (sipLoop == null) sipLoop = ProceduralBrewingSfx.SipLoop();
        if (plop == null) plop = ProceduralBrewingSfx.Plop();
        if (stir == null) stir = ProceduralBrewingSfx.Stir();
        if (finalGulp == null) finalGulp = ProceduralBrewingSfx.FinalGulp();
        if (bubble == null) bubble = ProceduralBrewingSfx.Bubble();
    }

    private void Update()
    {
        FadeSource(pourSource, pourTargetVol);
        FadeSource(sipSource, sipTargetVol);
    }

    // --- Pour loop --------------------------------------------------------------------------------
    public void StartPourLoop()
    {
        if (pourSource == null) return;
        if (!pourSource.isPlaying) pourSource.Play();
        pourTargetVol = 1f;
    }

    public void StopPourLoop() => pourTargetVol = 0f;

    // --- Sip loop ---------------------------------------------------------------------------------
    public void StartSipLoop()
    {
        if (sipSource == null) return;
        if (!sipSource.isPlaying) sipSource.Play();
        sipTargetVol = 1f;
    }

    public void StopSipLoop() => sipTargetVol = 0f;

    // --- One-shots --------------------------------------------------------------------------------
    public void PlayPlop() => PlayOneShot(plop);
    public void PlayStir() => PlayOneShot(stir);
    public void PlayFinalGulp() => PlayOneShot(finalGulp);
    public void PlayBubble() => PlayOneShot(bubble);

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || oneShotSource == null) return;
        oneShotSource.pitch = Random.Range(pitchJitter.x, pitchJitter.y);
        oneShotSource.PlayOneShot(clip);
    }

    private static void ConfigureLoop(AudioSource src, AudioClip clip)
    {
        if (src == null) return;
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;
    }

    private void FadeSource(AudioSource src, float target)
    {
        if (src == null) return;
        float speed = Time.unscaledDeltaTime / Mathf.Max(0.01f, loopFade);
        src.volume = Mathf.MoveTowards(src.volume, target, speed);
        if (src.volume <= 0.001f && target == 0f && src.isPlaying)
            src.Stop();
    }
}
