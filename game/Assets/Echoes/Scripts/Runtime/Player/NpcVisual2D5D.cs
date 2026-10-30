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
        public string Direction { get; private set; } = "S";
        public void UseSheet(SpriteSheet25D other) { if (other != null) sheet = other; }

        void LateUpdate()
        {
            if (sheet == null || card == null) return;
            if (sheet.Get("NE") != null && Camera.main != null)
            {
                Vector3 facing = transform.parent != null ? transform.parent.forward : transform.forward;
                var dialogue = FindFirstObjectByType<DialogueBox>();
                if (dialogue != null && dialogue.IsOpen && dialogue.Speaker == "Mira")
                {
                    var player = FindFirstObjectByType<PlayerController>();
                    if (player != null) facing = player.transform.position - transform.position;
                }
                facing = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
                Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
                float angle = Mathf.Atan2(Vector3.Dot(facing, right), Vector3.Dot(facing, forward)) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(CharacterVisual2D5D.Angle8(Direction), angle)) > 28.5f)
                    Direction = CharacterVisual2D5D.Sector8(angle);
            }
            else Direction = "S";
            SpriteSheet25D.Direction front = sheet.Get(Direction);
            if (front == null || front.idle.Length == 0) return;
            time += Time.deltaTime;
            FrameIndex = Mathf.FloorToInt((time + phaseSeconds) * sheet.idleFps) % front.idle.Length;
            card.Show(front.idle[FrameIndex], front.pivotPx, sheet.pixelsPerMeter, false, scale);
        }
    }
}
