using UnityEngine;

/// <summary>
/// Damped-spring sloshing for the liquid. Instead of a real fluid sim we tilt + bob the liquid
/// root with a critically-ish damped spring; the cup's SpriteMask clips the leaning corners so it
/// reads as sloshing. Pour / plop / stir events feed impulses via <see cref="AddImpulse"/>.
///
/// If a material with the optional LiquidWobble shader is assigned, the same energy drives the
/// shader's "_Splash" ripple amount for extra surface detail — but the effect works fully without it.
/// </summary>
public class LiquidWobble : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform tilted/bobbed to fake sloshing (usually the liquid root holding body + surface).")]
    [SerializeField] private Transform liquidRoot;

    [Header("Spring")]
    [SerializeField] private float stiffness = 120f;
    [SerializeField] private float damping = 6f;
    [Tooltip("Max tilt in degrees. Set to ~80 to allow a full cup-drinking lean.")]
    [SerializeField] private float maxTilt = 80f;
    [Tooltip("World units of vertical bob per unit of angular speed.")]
    [SerializeField] private float bobFactor = 0.012f;

    [Header("Idle shimmer")]
    [Tooltip("Tiny constant sine wobble so the surface is never perfectly still.")]
    [SerializeField] private float idleAmplitude = 0.4f;
    [SerializeField] private float idleSpeed = 1.6f;

    [Header("Device tilt sloshing")]
    [Tooltip("Slosh the liquid when the phone is tilted (reads the accelerometer). Off on desktop.")]
    [SerializeField] private bool enableTiltSlosh = true;
    [Tooltip("Multiplier on the actual device tilt angle (arcsin mapping). 1.0 = physically accurate " +
             "(water stays level relative to gravity); >1 exaggerates the lean.")]
    [SerializeField] private float tiltLeanFactor = 1f;
    [Tooltip("How fast tilt changes kick the slosh (sudden tilts splash more than slow ones).")]
    [SerializeField] private float tiltSloshKick = 40f;

    [Header("Optional shader hook")]
    [SerializeField] private SpriteRenderer shaderTarget;
    [SerializeField] private string splashProperty = "_Splash";

    private float angle;          // current tilt (deg)
    private float angularVel;     // deg/sec
    private float lastDeviceTilt; // last frame's side tilt, for slosh kicks
    private float energy;         // 0..1 normalised sloshing energy for the shader
    private Vector3 baseLocalPos;
    private MaterialPropertyBlock mpb;
    private int splashId;

    private void Awake()
    {
        if (liquidRoot == null) liquidRoot = transform;
        baseLocalPos = liquidRoot.localPosition;
        if (shaderTarget != null)
        {
            mpb = new MaterialPropertyBlock();
            splashId = Shader.PropertyToID(splashProperty);
        }
    }

    /// <summary>Kick the spring. <paramref name="strength"/> ~0.2 (gentle) .. 1.5 (hard shake).</summary>
    public void AddImpulse(float strength)
    {
        angularVel += strength * 60f * Mathf.Sign(Random.value - 0.5f);
        energy = Mathf.Clamp01(energy + strength);
    }

    /// <summary>Continuous push, e.g. while a stir swipe is dragging. Sign sets the lean direction.</summary>
    public void AddDirectionalForce(float signedStrength)
    {
        angularVel += signedStrength * 180f * Time.deltaTime * 60f;
        energy = Mathf.Clamp01(energy + Mathf.Abs(signedStrength) * Time.deltaTime * 4f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // When the phone is tilted, the liquid leans toward the tilt (rest angle) and sudden tilt
        // changes kick the spring so it sloshes back and forth before settling.
        float restAngle = 0f;
        if (enableTiltSlosh)
        {
            float tilt = Input.acceleration.x;            // ~ -1..1 g, side tilt
            // arcsin converts raw gravity component → actual device tilt angle in degrees,
            // so the liquid lean matches the real phone angle (not just a linear approximation).
            float tiltAngle = Mathf.Asin(Mathf.Clamp(tilt, -1f, 1f)) * Mathf.Rad2Deg;
            restAngle = Mathf.Clamp(-tiltAngle * tiltLeanFactor, -maxTilt, maxTilt);
            angularVel += (tilt - lastDeviceTilt) * tiltSloshKick;
            energy = Mathf.Clamp01(energy + Mathf.Abs(tilt - lastDeviceTilt) * 2f);
            lastDeviceTilt = tilt;
        }

        // Spring integration toward the (tilt-driven) rest angle.
        angularVel += (-stiffness * (angle - restAngle) - damping * angularVel) * dt;
        angle += angularVel * dt;
        angle = Mathf.Clamp(angle, -maxTilt, maxTilt);

        // Subtle idle shimmer layered on top of the spring.
        float idle = Mathf.Sin(Time.time * idleSpeed) * idleAmplitude;
        float displayAngle = angle + idle;

        liquidRoot.localRotation = Quaternion.Euler(0f, 0f, displayAngle);
        liquidRoot.localPosition = baseLocalPos + new Vector3(0f, -Mathf.Abs(angularVel) * bobFactor, 0f);

        // Decay shader energy.
        energy = Mathf.MoveTowards(energy, 0f, dt * 0.8f);
        if (shaderTarget != null)
        {
            shaderTarget.GetPropertyBlock(mpb);
            mpb.SetFloat(splashId, energy);
            shaderTarget.SetPropertyBlock(mpb);
        }
    }

    public void ResetWobble()
    {
        angle = 0f;
        angularVel = 0f;
        energy = 0f;
        if (liquidRoot != null)
        {
            liquidRoot.localRotation = Quaternion.identity;
            liquidRoot.localPosition = baseLocalPos;
        }
    }
}
