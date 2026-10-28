using UnityEngine;

namespace Echoes
{
    /// <summary>Visual-only 2.5D villager: loops the idle frames of a front-facing illustrated sheet. Collider, dialogue and idle logic stay on the NPC.</summary>
    [DisallowMultipleComponent]
    public sealed class NpcVisual2D5D : MonoBehaviour
    {
        [SerializeField] SpriteSheet25D sheet;
        [SerializeField] Sprite25DQuad card;
        [SerializeField] float phaseSeconds;
        [SerializeField] float scale = 1f;
        float time;

        public SpriteSheet25D Sheet => sheet;
        public int FrameIndex { get; private set; }

        void LateUpdate()
        {
            if (sheet == null || card == null) return;
            SpriteSheet25D.Direction front = sheet.Get("S");
            if (front == null || front.idle.Length == 0) return;
            time += Time.deltaTime;
            FrameIndex = Mathf.FloorToInt((time + phaseSeconds) * sheet.idleFps) % front.idle.Length;
            card.Show(front.idle[FrameIndex], front.pivotPx, sheet.pixelsPerMeter, false, scale);
        }
    }
}
