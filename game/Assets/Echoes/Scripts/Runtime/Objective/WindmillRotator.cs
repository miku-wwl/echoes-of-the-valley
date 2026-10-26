using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Turns the windmill's sails about their axle. It speeds up and slows down smoothly, runs in game time (a paused game
    /// stands still) and keeps its total angle so tests can measure the rotation. A stopped windmill rests at a crooked angle.
    /// </summary>
    public class WindmillRotator : MonoBehaviour
    {
        [SerializeField] Transform sails;
        [SerializeField] float degreesPerSecond = 40f;
        [SerializeField] float rampSeconds = 2.5f;
        [SerializeField] float restAngle = 18f;

        float speed;

        public Transform Sails => sails;
        public bool Spinning { get; private set; }
        public float CurrentSpeed => speed;
        public float FullSpeed => degreesPerSecond;

        /// <summary>Degrees turned since the scene loaded; it only ever grows.</summary>
        public float TotalDegrees { get; private set; }

        /// <summary>Starts or stops the sails. <paramref name="instant"/> skips the ramp (loading a game that is already restored).</summary>
        public void SetSpinning(bool on, bool instant)
        {
            Spinning = on;
            if (instant) speed = on ? degreesPerSecond : 0f;
        }

        void Awake() => Apply();

        void Update()
        {
            float target = Spinning ? degreesPerSecond : 0f;
            speed = rampSeconds <= 0f ? target : Mathf.MoveTowards(speed, target, degreesPerSecond / rampSeconds * Time.deltaTime);
            if (speed <= 0f) return;

            TotalDegrees += speed * Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (sails != null) sails.localRotation = Quaternion.Euler(0f, 0f, restAngle - TotalDegrees);
        }
    }
}
