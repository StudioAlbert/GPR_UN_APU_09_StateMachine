using UnityEngine;
using UnityEngine.InputSystem;

namespace Companion.Guard
{
    /// <summary>
    /// Scène 02 — Idle, Patrolling, OnAlarm.
    /// Le cas d'usage de <c>AddAnyTransition</c> : l'alarme interrompt le garde
    /// quoi qu'il soit en train de faire, et aucune des deux autres transitions
    /// n'a besoin de la connaître.
    /// </summary>
    public class GuardStateMachine : MonoBehaviour, IStateMachineProbe
    {
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float pause = 1.2f;

        private readonly Core.StateMachine _machine = new Core.StateMachine();

        private IdleState _idle;
        private PatrollingState _patrolling;
        private bool _alarmRaised;

        public string MachineName => "Garde";
        public Core.IState CurrentState => _machine.CurrentState;
        public string Details => _alarmRaised
            ? "R pour annuler l'alarme"
            : "A pour lever l'alarme (AddAnyTransition)";

        private void Awake()
        {
            _idle = new IdleState(this, pause);
            _patrolling = new PatrollingState(this, speed);
            var alarm = new OnAlarmState(this);

            _machine.ChangeState(_idle);
            _machine.AddTransition(_idle, _patrolling, () => _idle.PauseElapsed);
            _machine.AddTransition(_patrolling, _idle, () => _patrolling.TargetReached);
            _machine.AddAnyTransition(alarm, () => _alarmRaised);
            _machine.AddTransition(alarm, _idle, () => !_alarmRaised);
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.wasPressedThisFrame) _alarmRaised = true;
                if (Keyboard.current.rKey.wasPressedThisFrame) _alarmRaised = false;
            }

            _machine.Tick(Time.deltaTime);
        }

        // ------------------------------------------------------- services des états
        internal Transform NextWaypoint { get; private set; }

        internal void SwapWaypoint() => NextWaypoint = NextWaypoint == pointA ? pointB : pointA;

        internal bool StepTowardsWaypoint(float step)
        {
            if (NextWaypoint == null) NextWaypoint = pointB;

            transform.position = Vector3.MoveTowards(transform.position, NextWaypoint.position, step);
            return Vector3.Distance(transform.position, NextWaypoint.position) < 0.05f;
        }

        internal void SetColor(Color color)
        {
            if (TryGetComponent(out Renderer renderer)) renderer.material.color = color;
        }
    }

    public class IdleState : Core.IState
    {
        private readonly GuardStateMachine _guard;
        private readonly float _pause;
        private float _elapsed;

        public IdleState(GuardStateMachine guard, float pause)
        {
            _guard = guard;
            _pause = pause;
        }

        public bool PauseElapsed => _elapsed >= _pause;

        public void OnEnter()
        {
            _elapsed = 0f;
            _guard.SwapWaypoint();
            _guard.SetColor(new Color(0.6f, 0.65f, 0.7f));
        }

        public void OnExit() { }

        public void Tick(float deltaTime) => _elapsed += deltaTime;
    }

    public class PatrollingState : Core.IState
    {
        private readonly GuardStateMachine _guard;
        private readonly float _speed;

        public PatrollingState(GuardStateMachine guard, float speed)
        {
            _guard = guard;
            _speed = speed;
        }

        public bool TargetReached { get; private set; }

        public void OnEnter()
        {
            TargetReached = false;
            _guard.SetColor(new Color(0.35f, 0.6f, 0.9f));
        }

        public void OnExit() { }

        public void Tick(float deltaTime) =>
            TargetReached = _guard.StepTowardsWaypoint(_speed * deltaTime);
    }

    public class OnAlarmState : Core.IState
    {
        private readonly GuardStateMachine _guard;
        private float _elapsed;

        public OnAlarmState(GuardStateMachine guard) => _guard = guard;

        public void OnEnter()
        {
            _elapsed = 0f;
            _guard.SetColor(new Color(0.9f, 0.15f, 0.1f));
        }

        public void OnExit() { }

        public void Tick(float deltaTime)
        {
            // Sur place, et il tourne : on voit que l'état a bien la main.
            _elapsed += deltaTime;
            _guard.transform.Rotate(Vector3.up, 180f * deltaTime);
        }
    }
}
