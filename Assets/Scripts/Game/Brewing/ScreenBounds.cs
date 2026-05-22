using UnityEngine;

/// <summary>
/// Builds a U-shaped <see cref="EdgeCollider2D"/> (left wall + floor + right wall, OPEN at the top)
/// that follows the camera view, so dropped toppings settle inside the screen instead of falling out
/// the bottom or sides. The cup-less replacement for the old CupCavityCollider.
///
/// Setup in Unity: drop this on an empty GameObject, assign the Camera (or leave empty for
/// Camera.main). Make sure dropped toppings are on a layer that collides with this object's layer.
/// </summary>
[DefaultExecutionOrder(-10)] // build before toppings can be dropped
[RequireComponent(typeof(EdgeCollider2D))]
public class ScreenBounds : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [Tooltip("Push the walls this far OUTSIDE the screen edges, so toppings rest just off-frame at " +
             "the sides and the floor sits a touch below the bottom.")]
    [SerializeField] private float margin = 0.3f;
    [Tooltip("How far above the top edge the side walls extend, so fast toppings can't pop over them.")]
    [SerializeField] private float wallExtraHeight = 3f;
    [Tooltip("Re-fit every frame (handles rotation / resize). Off = fit once at start.")]
    [SerializeField] private bool refitContinuously = false;

    private EdgeCollider2D edge;

    private void Awake()
    {
        edge = GetComponent<EdgeCollider2D>();
        if (cam == null) cam = Camera.main;
    }

    private void Start() => Fit();

    private void LateUpdate()
    {
        if (refitContinuously) Fit();
    }

    /// <summary>Shape the collider into a U that hugs the screen edges. Public for manual cam changes.</summary>
    public void Fit()
    {
        if (edge == null || cam == null || !cam.orthographic) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;

        float left = c.x - halfW - margin;
        float right = c.x + halfW + margin;
        float bottom = c.y - halfH - margin;
        float top = c.y + halfH + wallExtraHeight;

        // Points are in this transform's local space; convert from world.
        edge.points = new[]
        {
            ToLocal(left, top),     // top of left wall (open above)
            ToLocal(left, bottom),  // down the left wall
            ToLocal(right, bottom), // across the floor
            ToLocal(right, top),    // up the right wall
        };
    }

    private Vector2 ToLocal(float worldX, float worldY)
    {
        Vector3 local = transform.InverseTransformPoint(new Vector3(worldX, worldY, 0f));
        return new Vector2(local.x, local.y);
    }
}
