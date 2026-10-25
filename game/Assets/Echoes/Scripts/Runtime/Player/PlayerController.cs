using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Third-person character movement on a CharacterController: camera-relative WASD, smoothed
    /// acceleration and turning, gravity with ground detection, and a respawn safety net.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField] CharacterAnimator characterAnimator;
        [SerializeField] Transform respawnPoint;
        [SerializeField] float moveSpeed = 4.5f;
        [SerializeField] float acceleration = 28f;
        [SerializeField] float turnSharpness = 14f;
        [SerializeField] float gravity = -22f;
        [SerializeField] float respawnBelowY = -25f;

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;

        /// <summary>Replaces keyboard input (automated tests and the scripted smoke run).</summary>
        public Vector2? InputOverride { get; set; }

        public Transform CameraTransform => cameraTransform;
        public CharacterAnimator Animator => characterAnimator;
        public Transform RespawnPoint => respawnPoint;
        public float MoveSpeed => moveSpeed;
        public float Speed => horizontalVelocity.magnitude;
        public bool IsGrounded => Controller.isGrounded;

        CharacterController Controller
        {
            get
            {
                if (controller == null) controller = GetComponent<CharacterController>();
                return controller;
            }
        }

        void Update()
        {
            Vector2 input = GameFlow.GameplayInputAllowed ? (InputOverride ?? GameInput.ReadMove()) : Vector2.zero;
            Simulate(input, Time.deltaTime);
        }

        /// <summary>One movement step. Public so tests can drive it without a keyboard.</summary>
        public void Simulate(Vector2 input, float dt)
        {
            if (dt <= 0f) return;

            Vector3 wish = ToWorldDirection(input);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wish * moveSpeed, acceleration * dt);

            if (wish.sqrMagnitude > 0.0001f)
            {
                Quaternion facing = Quaternion.LookRotation(wish, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, facing, 1f - Mathf.Exp(-turnSharpness * dt));
            }

            if (Controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * dt;

            Controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);

            if (transform.position.y < respawnBelowY) Respawn();
            if (characterAnimator != null) characterAnimator.Speed01 = horizontalVelocity.magnitude / moveSpeed;
        }

        /// <summary>Moves the character instantly (spawn, respawn, tests).</summary>
        public void Teleport(Vector3 position)
        {
            Controller.enabled = false;
            transform.position = position;
            Controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        public void Respawn()
        {
            Teleport(respawnPoint != null ? respawnPoint.position : new Vector3(0f, 2f, 0f));
        }

        Vector3 ToWorldDirection(Vector2 input)
        {
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }

            Vector3 direction = forward * input.y + right * input.x;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }
    }
}
