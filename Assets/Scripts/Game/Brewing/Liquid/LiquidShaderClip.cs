using UnityEngine;

/// <summary>
/// Drives the FruitMix/LiquidClipped shader so the liquid clips itself to a cup-shaped mask texture
/// — a robust replacement for SpriteMask (which can be finicky in URP 2D). Attach to the Cup/Liquid,
/// assign the liquid renderers, the mask transform (the object that overlays the cup interior) and
/// the CupMask sprite. It builds one material at runtime and feeds the mask's worldToLocal matrix +
/// bounds every frame, so clipping stays correct even as the cup tilts while drinking.
/// </summary>
[DefaultExecutionOrder(50)] // run after movers (drink tilt, wobble) so the matrix is up to date
public class LiquidShaderClip : MonoBehaviour
{
    [Tooltip("LiquidBody + LiquidSurface. If empty, auto-finds SpriteRenderers in children.")]
    [SerializeField] private SpriteRenderer[] liquidRenderers;
    [Tooltip("Transform that overlays the cup interior (e.g. CavityMask). Defines the mask space.")]
    [SerializeField] private Transform maskTransform;
    [Tooltip("Solid cup-cavity silhouette sprite (CupMask).")]
    [SerializeField] private Sprite maskSprite;
    [Range(0f, 1f)]
    [SerializeField] private float maskCutoff = 0.5f;

    private Material mat;
    private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
    private static readonly int MaskMatrixId = Shader.PropertyToID("_MaskMatrix");
    private static readonly int MaskMinId = Shader.PropertyToID("_MaskMin");
    private static readonly int MaskSizeId = Shader.PropertyToID("_MaskSize");
    private static readonly int MaskCutoffId = Shader.PropertyToID("_MaskCutoff");

    private void Awake()
    {
        if (liquidRenderers == null || liquidRenderers.Length == 0)
            liquidRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        var shader = Shader.Find("FruitMix/LiquidClipped");
        if (shader == null)
        {
            Debug.LogError("[LiquidShaderClip] KHÔNG tìm thấy shader 'FruitMix/LiquidClipped' — " +
                           "shader chưa import/compile. Liquid giữ material cũ (không clip).");
            enabled = false;
            return;
        }
        mat = new Material(shader);

        int applied = 0;
        foreach (var r in liquidRenderers)
        {
            if (r == null) continue;
            r.sharedMaterial = mat;
            r.maskInteraction = SpriteMaskInteraction.None; // we clip in the shader now, not via SpriteMask
            applied++;
        }

        if (maskSprite != null) mat.SetTexture(MaskTexId, maskSprite.texture);
        mat.SetFloat(MaskCutoffId, maskCutoff);

        Debug.Log($"[LiquidShaderClip] OK: shader found, material applied to {applied} renderer(s); " +
                  $"maskSprite={(maskSprite ? maskSprite.name : "NULL")} " +
                  $"maskTex={(maskSprite && maskSprite.texture ? maskSprite.texture.name : "NULL")} " +
                  $"shaderIsSupported={mat.shader.isSupported}");
    }

    private void LateUpdate()
    {
        if (mat == null || maskSprite == null || maskTransform == null) return;

        Bounds b = maskSprite.bounds; // local-space (unscaled) bounds in world units
        mat.SetMatrix(MaskMatrixId, maskTransform.worldToLocalMatrix);
        mat.SetVector(MaskMinId, new Vector4(b.min.x, b.min.y, 0, 0));
        mat.SetVector(MaskSizeId, new Vector4(b.size.x, b.size.y, 0, 0));
    }

    private void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
