using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds an <see cref="EdgeCollider2D"/> that follows the cup-cavity silhouette so toppings collide
/// with the real cup walls + floor (instead of falling through). The collider is rebuilt from the
/// mask sprite whenever the cup changes, and the top is left OPEN (a gap at the rim) so toppings can
/// still drop in from above.
///
/// Requirements:
///   • The mask sprite must have "Generate Physics Shape" enabled in its import settings.
///   • Put this on the object that overlays the cup interior at the same transform/scale as the mask
///     sprite is drawn (e.g. CavityMask). CupController.SetCup calls <see cref="Rebuild"/>.
/// </summary>
[RequireComponent(typeof(EdgeCollider2D))]
public class CupCavityCollider : MonoBehaviour
{
    [SerializeField] private EdgeCollider2D edge;
    [Tooltip("Fraction of the cavity height left open at the top (the mouth) so toppings drop in.")]
    [Range(0f, 0.4f)]
    [SerializeField] private float topOpenFraction = 0.08f;

    private static readonly List<Vector2> shape = new List<Vector2>();

    private void Awake()
    {
        if (edge == null) edge = GetComponent<EdgeCollider2D>();
    }

    /// <summary>Regenerate the wall/floor collider from a cup mask sprite. Call on every cup change.</summary>
    public void Rebuild(Sprite maskSprite)
    {
        if (edge == null) edge = GetComponent<EdgeCollider2D>();
        if (edge == null) return;

        if (maskSprite == null || maskSprite.GetPhysicsShapeCount() == 0)
        {
            // No physics shape baked into the sprite — disable so we don't keep a stale collider.
            edge.enabled = false;
            if (maskSprite != null)
                Debug.LogWarning($"[CupCavityCollider] '{maskSprite.name}' has no physics shape. " +
                                 "Enable 'Generate Physics Shape' in its import settings.");
            return;
        }

        maskSprite.GetPhysicsShape(0, shape); // outline points, sprite-local units
        if (shape.Count < 3) { edge.enabled = false; return; }

        float maxY = float.MinValue, minY = float.MaxValue;
        foreach (var p in shape)
        {
            if (p.y > maxY) maxY = p.y;
            if (p.y < minY) minY = p.y;
        }
        float threshold = maxY - (maxY - minY) * topOpenFraction;

        int n = shape.Count;
        // Find where the kept (lower) run starts: a kept point whose predecessor was removed.
        int start = -1;
        for (int i = 0; i < n; i++)
        {
            bool keep = shape[i].y <= threshold;
            bool prevRemoved = shape[(i - 1 + n) % n].y > threshold;
            if (keep && prevRemoved) { start = i; break; }
        }

        var pts = new List<Vector2>(n);
        if (start < 0)
        {
            // Nothing trimmed (fully closed) — use the whole loop closed.
            pts.AddRange(shape);
            pts.Add(shape[0]);
        }
        else
        {
            for (int k = 0; k < n; k++)
            {
                int i = (start + k) % n;
                if (shape[i].y > threshold) break; // reached the open top → stop
                pts.Add(shape[i]);
            }
        }

        edge.enabled = true;
        edge.SetPoints(pts);
    }
}
