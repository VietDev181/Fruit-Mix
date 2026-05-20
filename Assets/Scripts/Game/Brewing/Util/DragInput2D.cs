using UnityEngine;

/// <summary>
/// Lightweight pointer helper that works for both mouse (editor) and single touch (mobile).
/// Uses legacy Input (project has no Input System package). World position is computed on the
/// XY plane through the main camera so all 2D gameplay can share one source of truth.
/// </summary>
public static class DragInput2D
{
    public static bool IsPressed
    {
        get
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).phase != TouchPhase.Ended &&
                       Input.GetTouch(0).phase != TouchPhase.Canceled;
            return Input.GetMouseButton(0);
        }
    }

    public static bool PressedThisFrame
    {
        get
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).phase == TouchPhase.Began;
            return Input.GetMouseButtonDown(0);
        }
    }

    public static bool ReleasedThisFrame
    {
        get
        {
            if (Input.touchCount > 0)
            {
                var phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            }
            return Input.GetMouseButtonUp(0);
        }
    }

    public static Vector2 ScreenPosition
    {
        get
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }
    }

    public static Vector3 WorldPosition(Camera cam)
    {
        if (cam == null) cam = Camera.main;
        Vector3 sp = ScreenPosition;
        sp.z = Mathf.Abs(cam.transform.position.z); // distance to z=0 plane for an orthographic 2D cam
        return cam.ScreenToWorldPoint(sp);
    }

    /// <summary>Returns the topmost Collider2D under the pointer, or null.</summary>
    public static Collider2D PickCollider(Camera cam, LayerMask mask)
    {
        Vector3 world = WorldPosition(cam);
        return Physics2D.OverlapPoint(world, mask);
    }

    // --- Single-pointer drag ownership ---------------------------------------------------------
    // Ensures only one object follows the finger at a time (the pointer can overlap several
    // colliders). Whoever claims first owns the gesture until it releases.
    private static object _owner;

    public static bool HasOwner => _owner != null;

    public static bool TryClaim(object claimant)
    {
        if (_owner == null) _owner = claimant;
        return ReferenceEquals(_owner, claimant);
    }

    public static bool IsOwner(object claimant) => ReferenceEquals(_owner, claimant);

    public static void Release(object claimant)
    {
        if (ReferenceEquals(_owner, claimant)) _owner = null;
    }
}
