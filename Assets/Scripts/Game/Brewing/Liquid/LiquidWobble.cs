using UnityEngine;

/// <summary>
/// Damped-spring sloshing for the liquid. The body stays upright (no rotation) to avoid gaps on
/// screen-fill liquid. Only the surface/meniscus sprite tilts to show the water-surface angle.
/// Pour / plop / stir events feed impulses via <see cref="AddImpulse"/>.
/// </summary>
public class LiquidWobble : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The liquid body root — never rotated, so no gaps appear at screen edges.")]
    [SerializeField] private Transform liquidRoot;
    [Tooltip("The surface/meniscus sprite transform. Only this tilts to show the water-surface angle.")]
    [SerializeField] private Transform surfaceRoot;
    [Tooltip("LiquidController whose shader receives the tilt slope each frame.")]
    [SerializeField] private LiquidController liquidController;

    [Header("Spring")]
    [SerializeField] private float stiffness = 800f;
    [SerializeField] private float damping = 10f;
    [Tooltip("Max surface tilt in degrees.")]
    [SerializeField] private float maxTilt = 80f;
    [SerializeField] private float bobFactor = 0.012f;

    [Header("Idle shimmer")]
    [SerializeField] private float idleAmplitude = 0.3f;
    [SerializeField] private float idleSpeed = 1.6f;

    [Header("Device tilt sloshing")]
    [SerializeField] private bool enableTiltSlosh = true;
    [SerializeField] private float tiltLeanFactor = 1.3f;
    [SerializeField] private float tiltSloshKick = 20f;
    [Tooltip("Tick if the liquid leans the wrong way on your device.")]
    [SerializeField] private bool invertTilt = false;

    [Header("Editor fallback (PC testing)")]
    [Tooltip("Hold Left/Right arrow to simulate phone tilt in the editor.")]
    [SerializeField] private float editorTiltStrength = 1f;

    [Header("Optional shader hook")]
    [SerializeField] private SpriteRenderer shaderTarget;
    [SerializeField] private string splashProperty = "_Splash";

    private float angle;
    private float angularVel;
    private float lastDeviceTilt;
    private float energy;
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

    public void AddImpulse(float strength)
    {
        angularVel += strength * 60f * Mathf.Sign(Random.value - 0.5f);
        energy = Mathf.Clamp01(energy + strength);
    }

    public void AddDirectionalForce(float signedStrength)
    {
        angularVel += signedStrength * 180f * Time.deltaTime * 60f;
        energy = Mathf.Clamp01(energy + Mathf.Abs(signedStrength) * Time.deltaTime * 4f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float restAngle = 0f;
        if (enableTiltSlosh)
        {
            float tilt = Input.acceleration.x;
#if UNITY_EDITOR
            if (tilt == 0f)
            {
                if (Input.GetKey(KeyCode.LeftArrow))  tilt = -editorTiltStrength;
                if (Input.GetKey(KeyCode.RightArrow)) tilt =  editorTiltStrength;
            }
#endif
            float tiltAngle = Mathf.Asin(Mathf.Clamp(tilt, -1f, 1f)) * Mathf.Rad2Deg;
            float sign = invertTilt ? 1f : -1f;
            restAngle = Mathf.Clamp(sign * tiltAngle * tiltLeanFactor, -maxTilt, maxTilt);
            angularVel += (tilt - lastDeviceTilt) * tiltSloshKick;
            energy = Mathf.Clamp01(energy + Mathf.Abs(tilt - lastDeviceTilt) * 2f);
            lastDeviceTilt = tilt;
        }

        angularVel += (-stiffness * (angle - restAngle) - damping * angularVel) * dt;
        angle += angularVel * dt;
        angle = Mathf.Clamp(angle, -maxTilt, maxTilt);

        float idle = Mathf.Sin(Time.time * idleSpeed) * idleAmplitude;
        float displayAngle = angle + idle;

        // Body stays flat — shader handles the tilted fill line, no gaps.
        liquidRoot.localRotation = Quaternion.identity;
        liquidRoot.localPosition = baseLocalPos + new Vector3(0f, -Mathf.Abs(angularVel) * bobFactor, 0f);

        // Surface tilts to match the shader fill line visually.
        if (surfaceRoot != null)
            surfaceRoot.localRotation = Quaternion.Euler(0f, 0f, displayAngle);

        // Send tilt slope to the body shader so the fill line tilts per-pixel.
        // slope = tan(angle) * bodyWidth / cavityHeight  (converts world slope to UV space).
        if (liquidController != null)
        {
            float rad = displayAngle * Mathf.Deg2Rad;
            float slope = Mathf.Tan(rad) * liquidController.BodyWidth / Mathf.Max(liquidController.CavityHeight, 0.001f);
            liquidController.SetTiltSlope(slope);
        }

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
        if (surfaceRoot != null)
            surfaceRoot.localRotation = Quaternion.identity;
    }
}
