using UnityEngine;

/// <summary>
/// Procedurally synthesises the brewing ASMR clips at runtime — no audio files needed. Each method
/// returns a mono <see cref="AudioClip"/> built with <see cref="AudioClip.Create"/> and a hand-rolled
/// little synth (sine sweeps, filtered noise, envelopes). <see cref="BrewingAudio"/> calls these to
/// fill any clip slot left empty in the Inspector, so a real recording always wins if assigned.
///
/// Loop clips (pour, sip) are written with NO fade at the ends so they tile seamlessly when looped.
/// One-shots (plop, bubble, stir, gulp) carry their own attack/decay envelope.
/// </summary>
public static class ProceduralBrewingSfx
{
    private const int SampleRate = 44100;

    // --- One-shots --------------------------------------------------------------------------------

    /// <summary>"Tõm" — a topping dropping in: a quick downward pitch blip with a watery tail.</summary>
    public static AudioClip Plop()
    {
        int len = Sec(0.18f);
        var data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;                       // 0..1 over the clip
            float env = Mathf.Exp(-9f * t);                 // fast decay
            float freq = Mathf.Lerp(720f, 240f, Mathf.Sqrt(t)); // pitch drops as it sinks
            data[i] = Mathf.Sin(Phase(i, freq)) * env * 0.7f;
        }
        return Make("sfx_plop", data);
    }

    /// <summary>"Bloop" — a single rising bubble pop.</summary>
    public static AudioClip Bubble()
    {
        int len = Sec(0.13f);
        var data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-7f * t);
            float freq = Mathf.Lerp(280f, 760f, t);         // pitch rises (air escaping)
            data[i] = Mathf.Sin(Phase(i, freq)) * env * 0.55f;
        }
        return Make("sfx_bubble", data);
    }

    /// <summary>Spoon circling the cup: swelling band of filtered noise with a slow circular wobble.</summary>
    public static AudioClip Stir()
    {
        int len = Sec(0.45f);
        var data = new float[len];
        var rng = new System.Random(12345);
        float lp = 0f;
        const float alpha = 0.18f;                          // low-pass cutoff coefficient
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float n = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += alpha * (n - lp);                          // one-pole low-pass → soft "shhh"
            float swell = Mathf.Sin(t * Mathf.PI);           // 0→1→0 across the clip
            float wobble = 0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 6f); // spoon going round
            data[i] = lp * swell * wobble * 0.6f;
        }
        return Make("sfx_stir", data);
    }

    /// <summary>The satisfying final "ực ực" — three descending glugs.</summary>
    public static AudioClip FinalGulp()
    {
        int len = Sec(0.6f);
        var data = new float[len];
        var rng = new System.Random(777);
        float lp = 0f;
        float[] glugStart = { 0.0f, 0.2f, 0.4f };           // when each glug begins (in seconds)
        float[] glugFreq = { 220f, 180f, 150f };            // descending pitch
        for (int i = 0; i < len; i++)
        {
            float sec = (float)i / SampleRate;
            float s = 0f;
            for (int g = 0; g < glugStart.Length; g++)
            {
                float dt = sec - glugStart[g];
                if (dt < 0f || dt > 0.18f) continue;
                float env = Mathf.Exp(-14f * dt) * Mathf.Min(1f, dt * 60f); // pluck-ish attack+decay
                s += Mathf.Sin(2f * Mathf.PI * glugFreq[g] * dt) * env;
            }
            float n = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += 0.25f * (n - lp);
            data[i] = Mathf.Clamp(s * 0.6f + lp * 0.1f, -1f, 1f);
        }
        return Make("sfx_final_gulp", data);
    }

    // --- Loops (seamless, no end fade) ------------------------------------------------------------

    /// <summary>Looping water stream: low-passed noise + sparse bubbles.</summary>
    public static AudioClip PourLoop()
    {
        int len = Sec(1.0f);
        var data = new float[len];
        var rng = new System.Random(2024);
        float lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float n = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += 0.12f * (n - lp);                          // body of the stream (low rumble hiss)
            data[i] = lp * 0.5f;
        }
        // Sprinkle a few bubble blips across the loop for life.
        for (int b = 0; b < 7; b++)
        {
            int start = rng.Next(0, len);
            float freq = 320f + (float)rng.NextDouble() * 500f;
            int blen = Sec(0.05f);
            for (int j = 0; j < blen; j++)
            {
                int idx = (start + j) % len;                 // wrap so the loop stays seamless
                float env = Mathf.Exp(-10f * j / blen);
                data[idx] += Mathf.Sin(Phase(j, freq)) * env * 0.18f;
            }
        }
        Normalise(data, 0.8f);
        return Make("sfx_pour_loop", data, loop: true);
    }

    /// <summary>Looping sip/suck: higher, breathier band of filtered noise with a gentle flutter.</summary>
    public static AudioClip SipLoop()
    {
        int len = Sec(1.0f);
        var data = new float[len];
        var rng = new System.Random(99);
        float lp = 0f, hp = 0f, prev = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float n = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += 0.4f * (n - lp);                            // low-pass …
            hp = lp - prev; prev = lp;                        // … then high-pass → band-passed "ssss"
            float flutter = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 8f);
            data[i] = hp * flutter * 1.2f;
        }
        Normalise(data, 0.55f);
        return Make("sfx_sip_loop", data, loop: true);
    }

    // --- Helpers ----------------------------------------------------------------------------------

    private static int Sec(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));

    private static float Phase(int sample, float freq) => 2f * Mathf.PI * freq * sample / SampleRate;

    private static void Normalise(float[] data, float peak)
    {
        float max = 0f;
        for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
        if (max < 1e-4f) return;
        float g = peak / max;
        for (int i = 0; i < data.Length; i++) data[i] *= g;
    }

    private static AudioClip Make(string name, float[] data, bool loop = false)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
