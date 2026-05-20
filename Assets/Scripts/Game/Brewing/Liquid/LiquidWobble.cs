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
    [Tooltip("Max tilt in degrees so the liquid never leans unrealistically far.")]
    [SerializeField] private float maxTilt = 7f;
    [Tooltip("World units of vertical bob per unit of angular speed.")]
    [SerializeField] private float bobFactor = 0.012f;

    [Header("Idle shimmer")]
    [Tooltip("Tiny constant sine wobble so the surface is never perfectly still.")]
    [SerializeField] private float idleAmplitude = 0.4f;
    [SerializeField] private float idleSpeed = 1.6f;

    [Header("Optional shader hook")]
    [SerializeField] private SpriteRenderer shaderTarget;
    [SerializeField] private string splashProperty = "_Splash";

    private float angle;          // current tilt (deg)
    private float angularVel;     // deg/sec
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

        // Spring integration toward angle = 0.
        angularVel += (-stiffness * angle - damping * angularVel) * dt;
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
