using DG.Tweening;
using UnityEngine;

/// <summary>
/// Shakes the camera whenever the player physically shakes the phone. Reads the device
/// accelerometer (with an editor key fallback) and plays a DOTween position shake. Attach to the
/// "Main Camera" GameObject. Self-contained: no wiring to other systems is required.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Shake detection")]
    [Tooltip("Acceleration magnitude (in g) that counts as a phone shake.")]
    [SerializeField] private float shakeThreshold = 2.2f;
    [Tooltip("Min seconds between shakes so one gesture doesn't fire repeatedly.")]
    [SerializeField] private float shakeCooldown = 0.5f;
    [Tooltip("Editor/desktop fallback key to trigger a shake (no accelerometer).")]
    [SerializeField] private KeyCode editorShakeKey = KeyCode.S;

    [Header("Shake feel")]
    [Tooltip("How long a single camera shake lasts, in seconds.")]
    [SerializeField] private float duration = 0.4f;
    [Tooltip("Max camera offset (world units) at the start of the shake.")]
    [SerializeField] private float strength = 0.6f;
    [Tooltip("How many shakes happen over the duration. Higher = more frantic.")]
    [SerializeField] private int vibrato = 14;

    private float nextShakeTime;
    private Vector3 basePosition;
    private Tween shakeTween;

    private void Awake()
    {
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextShakeTime) return;

        bool shook = Input.GetKeyDown(editorShakeKey) ||
                     Input.acceleration.sqrMagnitude > shakeThreshold * shakeThreshold;
        if (!shook) return;

        nextShakeTime = Time.unscaledTime + shakeCooldown;
        Shake();
    }

    /// <summary>Play the camera shake. Public so other systems can trigger it too.</summary>
    public void Shake()
    {
        shakeTween?.Kill();
        transform.localPosition = basePosition;
        shakeTween = transform.DOShakePosition(duration, strength, vibrato, 90f, false, true)
            .SetUpdate(true)
            .OnComplete(() => transform.localPosition = basePosition);
    }

    private void OnDisable()
    {
        shakeTween?.Kill();
        transform.localPosition = basePosition;
    }
}
