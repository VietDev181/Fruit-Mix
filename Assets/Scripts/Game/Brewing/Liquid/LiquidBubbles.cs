using UnityEngine;

/// <summary>
/// Lazy little air bubbles ("sủi tăm") rising inside the liquid. Drives a ParticleSystem so bubbles
/// spawn anywhere across the current liquid column (from the bottom up to the surface) and drift
/// upward, fading near the top — a constant fizzy/ASMR detail that lives as long as there is liquid.
///
/// Setup in Unity:
///   1. Create a ParticleSystem as a CHILD of the Liquid object (so it rides the wobble). Give it a
///      soft round bubble sprite (Renderer ▸ Material), looping, gentle upward Start Speed, small
///      Start Size, and Simulation Space = Local.
///   2. Set its Shape to Box (this script resizes/repositions that box to match the liquid).
///   3. Add this component (anywhere), assign the Liquid and the ParticleSystem, tune the rate.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class LiquidBubbles : MonoBehaviour
{
    [SerializeField] private LiquidController liquid;
    [Tooltip("The bubble ParticleSystem. Defaults to the one on this GameObject.")]
    [SerializeField] private ParticleSystem bubbles;

    [Header("Emission")]
    [Tooltip("Bubbles per second when the drink is full. Scales down with the fill level.")]
    [SerializeField] private float maxRate = 18f;
    [Tooltip("Keep the bubble band a little narrower than the liquid so they don't spawn at the very edge.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float widthFraction = 0.9f;
    [Tooltip("Tint bubbles toward white from the liquid colour (0 = liquid colour, 1 = white).")]
    [Range(0f, 1f)]
    [SerializeField] private float whiten = 0.6f;

    private void Awake()
    {
        if (bubbles == null) bubbles = GetComponent<ParticleSystem>();
        if (liquid == null) liquid = GetComponentInParent<LiquidController>();
    }

    private void LateUpdate()
    {
        if (bubbles == null || liquid == null) return;

        var emission = bubbles.emission;
        var main = bubbles.main;

        if (liquid.IsEmpty)
        {
            emission.rateOverTime = 0f;
            return;
        }

        // Resize the emission box to cover the current liquid column, in the liquid's local space.
        float height = Mathf.Max(0.05f, liquid.FilledHeightLocal);
        float width = Mathf.Max(0.05f, liquid.BodyWidth * widthFraction);
        float centerY = liquid.BottomLocalY + height * 0.5f;

        var shape = bubbles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0f, centerY, 0f);
        shape.scale = new Vector3(width, height, 0.1f);

        emission.rateOverTime = maxRate * liquid.Fill;
        main.startColor = Color.Lerp(liquid.CurrentColor, Color.white, whiten);
    }
}
