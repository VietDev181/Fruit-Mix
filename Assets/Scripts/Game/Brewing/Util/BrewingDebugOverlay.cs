using UnityEngine;

/// <summary>
/// TEMPORARY debugging aid. Drop this on ANY GameObject in the scene (no wiring needed — it finds
/// the systems automatically) and press Play.
///
///   SPACE = bơm nước thẳng vào ly (bỏ qua chai/kéo-thả) — để kiểm tra riêng hệ liquid + mask.
///   R     = reset nước.
///
/// Đọc bảng góc trái màn hình:
///   - Nếu bấm SPACE mà Fill tăng NHƯNG không thấy nước  -> lỗi sprite/mask/vị trí (không phải pour).
///   - Nếu bấm SPACE thấy nước hiện                       -> hệ liquid OK, lỗi nằm ở thao tác RÓT.
/// Xoá script này khi xong.
/// </summary>
public class BrewingDebugOverlay : MonoBehaviour
{
    [SerializeField] private Color testColor = new Color(0.85f, 0.3f, 0.35f, 1f);
    [SerializeField] private float addPerPress = 0.15f;

    private LiquidController liquid;
    private CupController cup;
    private BrewingManager manager;
    private IngredientBottle[] bottles;

    private void Awake()
    {
        liquid = FindObjectOfType<LiquidController>(true);
        cup = FindObjectOfType<CupController>(true);
        manager = FindObjectOfType<BrewingManager>(true);
        bottles = FindObjectsOfType<IngredientBottle>(true);
    }

    private void Update()
    {
        if (liquid == null) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            liquid.AddLiquid(addPerPress, testColor);
            Debug.Log($"[BrewingDebug] AddLiquid -> Fill now {liquid.Fill:0.00}, color {liquid.CurrentColor}");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            liquid.ResetLiquid();
            Debug.Log("[BrewingDebug] ResetLiquid");
        }
    }

    private void OnGUI()
    {
        GUI.skin.label.fontSize = 22;
        GUILayout.BeginArea(new Rect(12, 12, 620, 520), GUI.skin.box);
        GUILayout.Label("BREWING DEBUG  (SPACE = bơm nước, R = reset)");

        if (manager != null) GUILayout.Label($"Phase: {manager.Phase}");
        else GUILayout.Label("BrewingManager: KHÔNG TÌM THẤY");

        if (liquid != null)
        {
            GUILayout.Label($"Fill: {liquid.Fill:0.000}   Empty={liquid.IsEmpty}");
            GUILayout.Label($"LiquidColor: {liquid.CurrentColor}");
            GUILayout.Label($"Surface world Y: {liquid.SurfaceWorldPosition.y:0.00}");
            var body = liquid.GetComponentInChildren<SpriteRenderer>(true);
            if (body != null)
                GUILayout.Label($"Body sprite: {(body.sprite != null ? body.sprite.name : "NULL!")}  enabled={body.enabled}");
        }
        else GUILayout.Label("LiquidController: KHÔNG TÌM THẤY");

        var camMain = Camera.main;
        Vector3 ptr = DragInput2D.WorldPosition(camMain);
        GUILayout.Label($"Pointer world: ({ptr.x:0.0}, {ptr.y:0.0})  pressed={DragInput2D.IsPressed}  hasOwner={DragInput2D.HasOwner}");

        if (bottles != null)
        {
            foreach (var b in bottles)
            {
                if (b == null) continue;
                Vector3 c = b.ColliderWorldCenter;
                GUILayout.Label($"{b.name}: over={b.PointerInsideCollider} drag={b.Dragging} POUR={b.Pouring} center=({c.x:0.0},{c.y:0.0}) dist={b.SpoutDistanceToCup:0.0}/{b.PourRadius:0.0}");
            }
        }

        var cam = Camera.main;
        GUILayout.Label(cam != null ? $"Camera: ortho={cam.orthographic} size={cam.orthographicSize}" : "Camera.main: NULL");
        GUILayout.EndArea();
    }
}
