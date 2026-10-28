using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// The four-vertex card that the Echoes/Sprite25D shader turns into a camera-facing frame around this object's origin (the ground point).
    /// Holds the per-renderer frame data in a MaterialPropertyBlock.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class Sprite25DQuad : MonoBehaviour
    {
        static readonly int FrameUV = Shader.PropertyToID("_FrameUV");
        static readonly int FrameRect = Shader.PropertyToID("_FrameRect");
        static readonly int Flip = Shader.PropertyToID("_Flip");
        static Mesh shared;
        MaterialPropertyBlock block;
        MeshRenderer meshRenderer;

        public Vector4 Uv { get; private set; }
        public Vector4 Rect { get; private set; }
        public bool Mirrored { get; private set; }
        public MeshRenderer Renderer => meshRenderer != null ? meshRenderer : (meshRenderer = GetComponent<MeshRenderer>());

        /// <summary>A unit card with generous bounds: the shader moves the corners in view space, up to about 2.5 m from the origin.</summary>
        public static Mesh CardMesh()
        {
            if (shared != null) return shared;
            shared = new Mesh { name = "Sprite25DCard" };
            shared.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, new Vector3(1, 1, 0) };
            shared.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            shared.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            shared.bounds = new Bounds(new Vector3(0f, 1.2f, 0f), new Vector3(5f, 5f, 5f));
            shared.hideFlags = HideFlags.DontSave;
            return shared;
        }

        void Awake()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh == null) filter.sharedMesh = CardMesh();
        }

        /// <summary>Shows one frame: its atlas rect, its size in metres and where its ground pivot is (from the frame's bottom-left).</summary>
        public void Show(SpriteSheet25D.Frame frame, Vector2 pivotPx, float pixelsPerMeter, bool mirrored, float scale = 1f)
        {
            if (block == null) block = new MaterialPropertyBlock();
            float k = scale / Mathf.Max(1f, pixelsPerMeter);
            float width = frame.sizePx.x * k, height = frame.sizePx.y * k;
            float left = -(mirrored ? frame.sizePx.x - pivotPx.x : pivotPx.x) * k;
            Uv = frame.uv;
            Rect = new Vector4(left, -pivotPx.y * k, width, height);
            Mirrored = mirrored;
            block.SetVector(FrameUV, Uv);
            block.SetVector(FrameRect, Rect);
            block.SetFloat(Flip, mirrored ? 1f : 0f);
            Renderer.SetPropertyBlock(block);
        }
    }
}
