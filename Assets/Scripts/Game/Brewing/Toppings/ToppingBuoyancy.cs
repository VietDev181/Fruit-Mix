using UnityEngine;

/// <summary>
/// Light fake-buoyancy for a topping inside the cup. When the topping is below the liquid surface we
/// push it up toward a rest depth and add extra drag, so pearls sink and settle low while ice/fruit
/// bob near the top. Not a real fluid sim — just enough Physics2D to feel alive. Enabled by
/// <see cref="DraggableTopping"/> once the topping is dropped in.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ToppingBuoyancy : MonoBehaviour
{
    [SerializeField] private CupController cup;

    [Header("Buoyancy")]
    [Tooltip("Upward stiffness toward the rest depth. Higher = floats up faster (use high for ice).")]
    [SerializeField] private float buoyancy = 12f;
    [Tooltip("How far below the surface this topping likes to rest. Big = sinks (pearls), ~0 = floats (ice).")]
    [SerializeField] private float restDepth = 0.6f;
    [Tooltip("Extra linear drag while submerged so it settles instead of bouncing forever.")]
    [SerializeField] private float submergedDrag = 3.5f;

    [Header("Life")]
    [Tooltip("Tiny continuous sway so settled toppings keep breathing.")]
    [SerializeField] private float swayForce = 0.4f;
    [SerializeField] private float swaySpeed = 1.3f;

    private Rigidbody2D rb;
    private float baseDrag;
    private float phase;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseDrag = rb.drag;
        phase = Random.value * Mathf.PI * 2f;
        if (cup == null) cup = GetComponentInParent<CupController>();
    }

    private void OnEnable()
    {
        if (cup == null) cup = GetComponentInParent<CupController>();
    }

    private void FixedUpdate()
    {
        if (cup == null || cup.Liquid == null) return;

        float surfaceY = cup.Liquid.SurfaceWorldPosition.y;
        float depth = surfaceY - transform.position.y; // >0 when submerged

        if (depth > 0f && !cup.Liquid.IsEmpty)
        {
            // Spring toward rest depth: push up when deeper than restDepth, let it sink when shallower.
            float displacement = depth - restDepth;
            rb.AddForce(Vector2.up * buoyancy * displacement, ForceMode2D.Force);

            // Gentle horizontal sway for ASMR life.
            float sway = Mathf.Sin(Time.time * swaySpeed + phase) * swayForce;
            rb.AddForce(Vector2.right * sway, ForceMode2D.Force);

            rb.drag = submergedDrag;
        }
        else
        {
            rb.drag = baseDrag;
        }
    }

    /// <summary>External kick, e.g. from stirring/shaking.</summary>
    public void AddStirImpulse(Vector2 impulse)
    {
        if (rb != null) rb.AddForce(impulse, ForceMode2D.Impulse);
    }
}
