using UnityEngine;

/// <summary>
/// Detects device shake (accelerometer) and triggers a swirl effect: liquid gets slosh impulses and
/// toppings orbit in a circle. On PC/Editor hold <see cref="editorShakeKey"/> (default: Space) to
/// simulate a shake.
/// </summary>
public class ShakeController : MonoBehaviour
{
    [SerializeField] private DrinkContainer container;
    [SerializeField] private BrewingAudio brewAudio;

    [Header("Shake detection")]
    [Tooltip("Accelerometer magnitude delta that counts as a shake.")]
    [SerializeField] private float shakeThreshold = 1.8f;
    [Tooltip("Seconds of cooldown between two shake triggers.")]
    [SerializeField] private float shakeCooldown = 0.25f;

    [Header("Swirl")]
    [Tooltip("How many wobble impulses are sent per shake.")]
    [SerializeField] private float wobbleImpulse = 1.2f;
    [Tooltip("Tangential speed applied to each topping to spin them in a circle (units/sec).")]
    [SerializeField] private float swirlToppingSpeed = 5f;
    [Tooltip("Duration the swirl angular velocity is kept alive after each shake.")]
    [SerializeField] private float swirlDecay = 1.5f;

    [Header("Editor fallback")]
    [Tooltip("Hold this key to simulate a shake in the editor / on PC.")]
    [SerializeField] private KeyCode editorShakeKey = KeyCode.Space;

    private float cooldownTimer;
    private Vector3 lastAccel;
    private float swirlVelocity; // current swirl angular speed (rad/sec), decays over time
    private bool active = true;

    public void SetActive(bool value) => active = value;

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (swirlVelocity != 0f)
        {
            ApplySwirlToToppings(swirlVelocity * Time.deltaTime);
            swirlVelocity = Mathf.MoveTowards(swirlVelocity, 0f, Time.deltaTime / Mathf.Max(swirlDecay, 0.01f) * Mathf.Abs(swirlVelocity));
        }

        if (!active) return;

        if (DetectShake()) TriggerSwirl();
    }

    private bool DetectShake()
    {
        if (cooldownTimer > 0f) return false;

#if UNITY_EDITOR
        if (Input.GetKeyDown(editorShakeKey)) return true;
#endif

        Vector3 accel = Input.acceleration;
        float delta = (accel - lastAccel).magnitude / Mathf.Max(Time.deltaTime, 0.001f);
        lastAccel = accel;
        return delta > shakeThreshold;
    }

    private void TriggerSwirl()
    {
        cooldownTimer = shakeCooldown;

        // Slosh the liquid.
        container?.Wobble?.AddImpulse(wobbleImpulse);

        // Kick swirl; alternate direction each shake for a natural feel.
        float dir = (Random.value > 0.5f) ? 1f : -1f;
        swirlVelocity += dir * swirlToppingSpeed;

        brewAudio?.PlayStir();
    }

    // Rotate every topping around the liquid centre by deltaAngle radians.
    private void ApplySwirlToToppings(float deltaAngle)
    {
        if (container == null || container.ToppingContainer == null) return;

        Vector2 centre = container.Liquid != null
            ? (Vector2)container.Liquid.SurfaceWorldPosition
            : (Vector2)container.transform.position;

        var toppings = container.ToppingContainer.GetComponentsInChildren<ToppingBuoyancy>();
        foreach (var t in toppings)
        {
            var rb = t.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            Vector2 offset = (Vector2)t.transform.position - centre;
            // Tangential velocity perpendicular to radius (rotate offset by 90°).
            Vector2 tangent = new Vector2(-offset.y, offset.x).normalized;
            rb.AddForce(tangent * deltaAngle * 60f, ForceMode2D.Force);
        }
    }
}
