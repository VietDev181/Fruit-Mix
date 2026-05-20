using UnityEngine;

/// <summary>
/// Swipe across the cup to stir / shake it: the swipe speed feeds the liquid's sloshing spring and
/// nudges every topping horizontally, so the drink visibly swirls. Active during the Stir phase
/// (toggle with <see cref="SetActive"/>). Uses the shared drag lock so it never fights the bottles
/// or toppings for the finger.
/// </summary>
public class StirController : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private CupController cup;
    [Tooltip("Collider covering the cup area where stirring is detected.")]
    [SerializeField] private Collider2D stirZone;
    [SerializeField] private BrewingAudio brewAudio;

    [Header("Strength")]
    [Tooltip("Swipe-velocity → liquid lean force.")]
    [SerializeField] private float liquidForceFactor = 0.6f;
    [Tooltip("Swipe-velocity → per-topping horizontal impulse.")]
    [SerializeField] private float toppingForceFactor = 0.35f;
    [Tooltip("Min swipe speed (units/sec) before a stir sound plays.")]
    [SerializeField] private float stirSoundThreshold = 3f;

    private bool active = true;
    private bool stirring;
    private float lastX;
    private float soundCooldown;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    public void SetActive(bool value)
    {
        active = value;
        if (!value && stirring) StopStir();
    }

    private void Update()
    {
        soundCooldown -= Time.deltaTime;
        if (!active) return;

        if (!stirring && DragInput2D.PressedThisFrame && !DragInput2D.HasOwner)
        {
            Vector3 p = DragInput2D.WorldPosition(cam);
            if (stirZone != null && stirZone.OverlapPoint(p) && DragInput2D.TryClaim(this))
            {
                stirring = true;
                lastX = p.x;
            }
        }

        if (!stirring) return;

        if (DragInput2D.ReleasedThisFrame || !DragInput2D.IsOwner(this))
        {
            StopStir();
            return;
        }

        StirTick();
    }

    private void StirTick()
    {
        float x = DragInput2D.WorldPosition(cam).x;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float velocity = (x - lastX) / dt; // signed swipe speed in units/sec
        lastX = x;

        cup.Wobble?.AddDirectionalForce(Mathf.Clamp(velocity, -8f, 8f) * liquidForceFactor * 0.02f);

        if (cup.ToppingContainer != null)
        {
            float impulse = Mathf.Clamp(velocity, -8f, 8f) * toppingForceFactor;
            var toppings = cup.ToppingContainer.GetComponentsInChildren<ToppingBuoyancy>();
            foreach (var t in toppings)
                t.AddStirImpulse(new Vector2(impulse, Mathf.Abs(impulse) * 0.3f));
        }

        if (Mathf.Abs(velocity) > stirSoundThreshold && soundCooldown <= 0f)
        {
            brewAudio?.PlayStir();
            soundCooldown = 0.25f;
        }
    }

    private void StopStir()
    {
        stirring = false;
        DragInput2D.Release(this);
    }

    private void OnDestroy() => DragInput2D.Release(this);
}
