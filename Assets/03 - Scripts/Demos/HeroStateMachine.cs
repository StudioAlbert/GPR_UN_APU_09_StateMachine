using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Companion.Hero
{
    /// <summary>
    /// Scène 01 — Grounded, Jumping, Landing.
    /// Un cube saute à la barre d'espace, puis reste bloqué en Landing le temps
    /// d'une coroutine : la transition de retour attend un drapeau, pas un chrono
    /// recopié dans l'état.
    /// </summary>
    public class HeroStateMachine : MonoBehaviour, IStateMachineProbe
    {
        [SerializeField] private float jumpHeight = 2.5f;
        [SerializeField] private float jumpDuration = 0.6f;
        [SerializeField] private float landingCooldown = 0.8f;

        private readonly Core.StateMachine _machine = new Core.StateMachine();

        private Vector3 _restPosition;
        private bool _jumpFinished;
        private bool _landingFinished;

        public string MachineName => "Héros";
        public Core.IState CurrentState => _machine.CurrentState;
        public string Details => "Espace pour sauter — pas pendant Landing";

        private void Awake()
        {
            _restPosition = transform.position;

            var grounded = new GroundedState(this);
            var jumping = new JumpingState(this);
            var landing = new LandingState(this);

            _machine.ChangeState(grounded);
            _machine.AddTransition(grounded, jumping, JumpPressed);
            _machine.AddTransition(jumping, landing, () => _jumpFinished);
            _machine.AddTransition(landing, grounded, () => _landingFinished);
        }

        private void Update() => _machine.Tick(Time.deltaTime);

        private static bool JumpPressed() =>
            Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        // ------------------------------------------------------- services des états
        internal void BeginJump()
        {
            _jumpFinished = false;
            StartCoroutine(Jump());
        }

        private IEnumerator Jump()
        {
            for (float t = 0f; t < jumpDuration; t += Time.deltaTime)
            {
                float height = Mathf.Sin(Mathf.PI * t / jumpDuration) * jumpHeight;
                transform.position = _restPosition + Vector3.up * height;
                yield return null;
            }

            transform.position = _restPosition;
            _jumpFinished = true;
        }

        internal void BeginLanding()
        {
            _landingFinished = false;
            StartCoroutine(Land());
        }

        private IEnumerator Land()
        {
            transform.localScale = new Vector3(1.3f, 0.6f, 1.3f);
            yield return new WaitForSeconds(landingCooldown);
            transform.localScale = Vector3.one;
            _landingFinished = true;
        }
    }

    public class GroundedState : Core.IState
    {
        private readonly HeroStateMachine _hero;
        public GroundedState(HeroStateMachine hero) => _hero = hero;

        public void OnEnter() { }
        public void OnExit() { }
        public void Tick(float deltaTime) { }
    }

    public class JumpingState : Core.IState
    {
        private readonly HeroStateMachine _hero;
        public JumpingState(HeroStateMachine hero) => _hero = hero;

        public void OnEnter() => _hero.BeginJump();
        public void OnExit() { }
        public void Tick(float deltaTime) { }
    }

    public class LandingState : Core.IState
    {
        private readonly HeroStateMachine _hero;
        public LandingState(HeroStateMachine hero) => _hero = hero;

        public void OnEnter() => _hero.BeginLanding();
        public void OnExit() { }
        public void Tick(float deltaTime) { }
    }
}
