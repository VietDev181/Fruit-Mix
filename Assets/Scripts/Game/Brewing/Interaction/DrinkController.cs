using System;
using UnityEngine;

/// <summary>
/// Cup-less drinking: TILT THE PHONE to drink. The further the device is tilted past a dead-zone, the
/// faster the screen-fill liquid drains, the liquid leans toward the tilt, and a slurp/straw ASMR loop
/// plays. Empties to <see cref="OnEmptied"/>. Active during the Drink phase only.
///
/// On desktop / in the editor (no usable accelerometer) hold <see cref="editorDrinkKey"/> to drain so
/// the flow stays testable.
/// </summary>
public class DrinkController : MonoBehaviour
{
    [SerializeField] private DrinkContainer container;
    [SerializeField] private BrewingAudio brewAudio;

    [Header("Tilt to drink")]
    [Tooltip("Tilt magnitude (0..1, |Input.acceleration.x|) below which nothing drains — the resting " +
             "dead-zone so holding the phone normally doesn't drink.")]
    [SerializeField] private float tiltDeadZone = 0.25f;
    [Tooltip("Tilt magnitude at which the liquid drains at full speed.")]
    [SerializeField] private float tiltForFullDrain = 0.7f;
    [Tooltip("Max fraction of the drink drained per second at full tilt.")]
    [SerializeField] private float drainRatePerSecond = 0.5f;

    [Header("Editor fallback")]
    [Tooltip("Hold this key to drink when there is no accelerometer (editor / desktop).")]
    [SerializeField] private KeyCode editorDrinkKey = KeyCode.D;

    public event Action OnEmptied;

    private bool active;
    private bool sipping;
    private bool emptied;

    private LiquidController Liquid => container != null ? container.Liquid : null;

    public void SetActive(bool value)
    {
        active = value;
        if (!active) StopSip();
    }

    /// <summary>Kept for API compatibility with BrewingManager; no-op in the screen-fill flow.</summary>
    public void CaptureHome() { }

    private void Update()
    {
        if (!active || emptied) return;

        float tilt = CurrentTilt();
        float t = Mathf.Clamp01((tilt - tiltDeadZone) / Mathf.Max(0.01f, tiltForFullDrain - tiltDeadZone));

        var l = Liquid;
        bool shouldSip = t > 0f && l != null && !l.IsEmpty;
        if (shouldSip)
        {
            l.DrainLiquid(drainRatePerSecond * t * Time.deltaTime);
            container.Wobble?.AddImpulse(0.03f * t);
            if (!sipping) { brewAudio?.StartSipLoop(); sipping = true; }

            if (l.IsEmpty)
            {
                emptied = true;
                StopSip();
                brewAudio?.PlayFinalGulp();
                OnEmptied?.Invoke();
            }
        }
        else if (sipping)
        {
            StopSip();
        }
    }

    /// <summary>Tilt magnitude in 0..1 from the device side-tilt, or the hold key in the editor.</summary>
    private float CurrentTilt()
    {
        if (Input.GetKey(editorDrinkKey)) return 1f;
        return Mathf.Clamp01(Mathf.Abs(Input.acceleration.x));
    }

    private void StopSip()
    {
        if (!sipping) return;
        sipping = false;
        brewAudio?.StopSipLoop();
    }

    public void ResetDrink()
    {
        emptied = false;
        StopSip();
    }
}
