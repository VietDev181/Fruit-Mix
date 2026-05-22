using UnityEngine;

/// <summary>
/// "The screen IS the cup." Reshapes a <see cref="LiquidController"/>'s cavity to span the whole
/// camera view, so pouring fills the entire screen from the bottom up instead of a cup shape. Use
/// this in the cup-less GameScene: drop it on the liquid object (or any scene object), assign the
/// LiquidController, and the liquid body becomes a full-screen rectangle that rises with the fill.
///
/// Notes:
///   • Don't add a SpriteMask or LiquidShaderClip to the liquid in this mode — we want the plain
///     rectangle, not a cup-shaped clip.
///   • Place the liquid object at the camera's centre X (the body sprite is centred horizontally).
///   • For the liquid to reach the very top, set the LiquidController's Max Fill to 1.
///   • Works with an orthographic 2D camera (the project's setup).
/// </summary>
[DefaultExecutionOrder(-10)] // fit before pours start so the cavity is correct on the first frame
public class ScreenLiquidFitter : MonoBehaviour
{
    [SerializeField] private LiquidController liquid;
    [SerializeField] private Camera cam;
    [Tooltip("Extra world units added to each side so the liquid overflows past the screen edges " +
             "and never shows a gap at the sides.")]
    [SerializeField] private float widthPadding = 0.5f;
    [Tooltip("Re-fit every frame so it stays correct if the screen rotates / resizes. Off = fit once.")]
    [SerializeField] private bool refitContinuously = false;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Start() => FitToScreen();

    private void LateUpdate()
    {
        if (refitContinuously) FitToScreen();
    }

    /// <summary>Configure the liquid cavity to cover the full camera view. Public so you can call it
    /// after a manual camera change.</summary>
    public void FitToScreen()
    {
        if (liquid == null || cam == null || !cam.orthographic) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // World-space bottom-centre of the screen, expressed in the liquid transform's local space
        // (ConfigureCavity wants local units).
        Vector3 worldBottom = new Vector3(cam.transform.position.x, cam.transform.position.y - halfHeight, 0f);
        Vector3 localBottom = liquid.transform.InverseTransformPoint(worldBottom);

        float width = (halfWidth + widthPadding) * 2f;
        float height = halfHeight * 2f;
        liquid.ConfigureCavity(localBottom.y, height, width);
    }
}
