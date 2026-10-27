using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Publishes the point the foliage shader keeps visible (the player's chest) as the global _EchoesFocus. The Echoes/Foliage
    /// shader dissolves leaves that sit between the camera and this point and leaves right next to the camera, so the free camera can
    /// move under and through canopies without a card ever filling the screen. Presentation only: no gameplay or camera change.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class FoliageFadeDriver : MonoBehaviour
    {
        static readonly int FocusId = Shader.PropertyToID("_EchoesFocus");
        [SerializeField] float focusHeight = 1.05f;
        PlayerController player;

        public Vector4 Focus { get; private set; }

        void LateUpdate()
        {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            Vector3 p = player.transform.position + Vector3.up * focusHeight;
            Focus = new Vector4(p.x, p.y, p.z, 1f);
            Shader.SetGlobalVector(FocusId, Focus);
        }

        void OnDisable()
        {
            Focus = Vector4.zero;
            Shader.SetGlobalVector(FocusId, Vector4.zero);
        }
    }
}
