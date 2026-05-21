using UnityEngine;

/// <summary>
/// Visual + audio of a pouring stream. A thin sprite (TOP pivot) is stretched from the bottle
/// pour origin down to the liquid surface; splash particles fire where it lands and bubbles rise
/// inside the cup. Everything is tinted to the ingredient colour. Driven by <see cref="ButtonPour"/>.
/// </summary>
public class PourStream : MonoBehaviour
{
    [Header("Stream")]
    [Tooltip("1-unit tall sprite with a TOP pivot, so scaling Y stretches it downward from the spout.")]
    [SerializeField] private SpriteRenderer streamSprite;
    [SerializeField] private float streamWidth = 0.12f;
    [Tooltip("Slight horizontal wobble of the stream for life.")]
    [SerializeField] private float streamWaggle = 0.05f;
    [SerializeField] private float streamWaggleSpeed = 18f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem splash;   // at the landing point
    [SerializeField] private ParticleSystem bubbles;  // rising inside the cup

    private bool active;

    private void Awake() => SetVisible(false);

    public void Begin(Color color)
    {
        active = true;
        SetVisible(true);
        ApplyColor(color);
        if (splash != null) { var e = splash.emission; e.enabled = true; if (!splash.isPlaying) splash.Play(); }
        if (bubbles != null) { var e = bubbles.emission; e.enabled = true; if (!bubbles.isPlaying) bubbles.Play(); }
    }

    /// <summary>Call every frame while pouring to keep the stream pinned spout→surface.</summary>
    public void UpdatePositions(Vector3 spoutWorld, Vector3 surfaceWorld)
    {
        if (!active || streamSprite == null) return;

        float length = Mathf.Max(0.01f, spoutWorld.y - surfaceWorld.y);
        float waggle = Mathf.Sin(Time.time * streamWaggleSpeed) * streamWaggle;

        Transform t = streamSprite.transform;
        t.position = new Vector3(spoutWorld.x + waggle * 0.5f, spoutWorld.y, spoutWorld.z);
        t.localScale = new Vector3(streamWidth, length, 1f);

        if (splash != null)
            splash.transform.position = new Vector3(surfaceWorld.x, surfaceWorld.y, surfaceWorld.z);
        if (bubbles != null)
            bubbles.transform.position = new Vector3(surfaceWorld.x, surfaceWorld.y, surfaceWorld.z);
    }

    public void End()
    {
        active = false;
        SetVisible(false);
        if (splash != null) { var e = splash.emission; e.enabled = false; }
        if (bubbles != null) { var e = bubbles.emission; e.enabled = false; }
    }

    private void ApplyColor(Color color)
    {
        if (streamSprite != null)
        {
            Color c = color; c.a = 0.9f;
            streamSprite.color = c;
        }
        TintParticles(splash, color);
        TintParticles(bubbles, Color.Lerp(color, Color.white, 0.5f));
    }

    private static void TintParticles(ParticleSystem ps, Color color)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = color;
    }

    private void SetVisible(bool v)
    {
        if (streamSprite != null) streamSprite.enabled = v;
    }
}
