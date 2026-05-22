using DG.Tweening;
using UnityEngine;

/// <summary>
/// Juicy "coins fly to the wallet" burst. Spawns a handful of coin images that scatter from a spawn
/// point then home in on a target point (e.g. the gold icon), with a little pop on arrival. Pure UI:
/// put this on an object under a Canvas and call <see cref="Play()"/> when a reward is earned.
///
/// Setup in Unity:
///   1. Make a coin Image prefab (a small RectTransform with the coin sprite).
///   2. Place a Spawn Point (where coins burst from, e.g. screen centre) and a Target (the gold icon)
///      as RectTransforms under the same Canvas.
///   3. Assign all three + tune the count/feel, then call Play() (RecipeManager does this on finish).
/// </summary>
public class CoinFlyEffect : MonoBehaviour
{
    [Header("Refs (all under the same Canvas)")]
    [SerializeField] private RectTransform coinPrefab;
    [Tooltip("Where the coins burst from. Defaults to this transform.")]
    [SerializeField] private RectTransform spawnPoint;
    [Tooltip("Where the coins fly to (e.g. the gold counter icon).")]
    [SerializeField] private RectTransform target;

    [Header("Feel")]
    [SerializeField] private int coinCount = 8;
    [Tooltip("How far coins scatter before homing in (screen units).")]
    [SerializeField] private float scatterRadius = 120f;
    [SerializeField] private float scatterDuration = 0.25f;
    [SerializeField] private float flyDuration = 0.55f;
    [Tooltip("Delay between each coin leaving, so they stream rather than move as one blob.")]
    [SerializeField] private float stagger = 0.05f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioService audioService;

    /// <summary>Total time the whole burst takes — wait this long before changing scene.</summary>
    public float TotalDuration => scatterDuration + flyDuration + coinCount * stagger;

    public void Play() => Play(coinCount);

    public void Play(int count)
    {
        if (coinPrefab == null || target == null) return;
        RectTransform from = spawnPoint != null ? spawnPoint : (RectTransform)transform;

        for (int i = 0; i < count; i++)
        {
            RectTransform coin = Instantiate(coinPrefab, transform);
            coin.gameObject.SetActive(true);
            coin.position = from.position;
            coin.localScale = Vector3.zero;

            Vector3 scatter = from.position + (Vector3)(Random.insideUnitCircle * scatterRadius);
            float delay = i * stagger;

            Sequence s = DOTween.Sequence().SetDelay(delay);
            s.Append(coin.DOScale(1f, scatterDuration * 0.6f).SetEase(Ease.OutBack));
            s.Join(coin.DOMove(scatter, scatterDuration).SetEase(Ease.OutQuad));
            s.Append(coin.DOMove(target.position, flyDuration).SetEase(Ease.InBack));
            s.Join(coin.DOScale(0.6f, flyDuration).SetEase(Ease.InQuad));
            s.OnComplete(() =>
            {
                Destroy(coin.gameObject);
                PunchTarget();
            });
        }

        audioService?.PlayClickSFX();
    }

    private void PunchTarget()
    {
        if (target == null) return;
        target.DOComplete();
        target.DOPunchScale(Vector3.one * 0.25f, 0.25f, 8, 0.6f);
    }
}
