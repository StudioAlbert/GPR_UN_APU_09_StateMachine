using UnityEngine;

namespace Companion.TurnBased
{
    /// <summary>
    /// La machine du bas : Waiting, Active, Empty. Une tourelle ne sait rien du
    /// déroulé de la partie — elle est activée par l'état de tour, et c'est tout.
    /// </summary>
    public class Turret : MonoBehaviour, IStateMachineProbe
    {
        [SerializeField] private string label = "Tourelle";
        [SerializeField] private int ammoPerTurn = 3;
        [SerializeField] private int hull = 4;

        private int ammo;

        private readonly Core.StateMachine _machine = new Core.StateMachine();

        private WaitingState _waiting;
        private ActiveState _active;
        private bool _activated;

        public int Ammo => ammo;
        public int Hull => hull;
        public bool OutOfAmmo => ammo <= 0;
        public bool Destroyed => hull <= 0;

        public string MachineName => label;
        public Core.IState CurrentState => _machine.CurrentState;
        public string Details => "";

        private void Awake()
        {
            ammo = ammoPerTurn;

            _waiting = new WaitingState(this);
            _active = new ActiveState(this);
            var empty = new EmptyState(this);

            _machine.ChangeState(_waiting);
            // La plus urgente d'abord : à sec ou détruite, la tourelle ne joue plus.
            _machine.AddAnyTransition(empty, () => OutOfAmmo || Destroyed);
            _machine.AddTransition(_waiting, _active, () => _activated);
            _machine.AddTransition(_active, _waiting, () => !_activated);
            _machine.AddTransition(empty, _waiting, () => !OutOfAmmo && !Destroyed);
        }

        private void Update() => _machine.Tick(Time.deltaTime);

        /// <summary>Un tour commence : la tourelle est rechargée et reprend la main.</summary>
        public void Activate()
        {
            if (!Destroyed) ammo = ammoPerTurn;
            _activated = true;
        }

        /// <summary>Le tour se termine : la tourelle rend la main et se recharge.</summary>
        public void Deactivate()
        {
            if (!Destroyed) ammo = ammoPerTurn;
            _activated = false;
        }

        public void Fire(Turret target)
        {
            if (OutOfAmmo || Destroyed) return;

            ammo--;
            target.TakeHit();
        }

        private void TakeHit() => hull--;

        internal void SetColor(Color color)
        {
            if (TryGetComponent(out Renderer renderer)) renderer.material.color = color;
        }
    }

    public class WaitingState : Core.IState
    {
        private readonly Turret _turret;
        public WaitingState(Turret turret) => _turret = turret;

        public void OnEnter() => _turret.SetColor(new Color(0.55f, 0.58f, 0.62f));
        public void OnExit() { }
        public void Tick(float deltaTime) { }
    }

    public class ActiveState : Core.IState
    {
        private readonly Turret _turret;
        public ActiveState(Turret turret) => _turret = turret;

        public void OnEnter() => _turret.SetColor(new Color(0.95f, 0.75f, 0.1f));
        public void OnExit() { }
        public void Tick(float deltaTime) { }
    }

    public class EmptyState : Core.IState
    {
        private readonly Turret _turret;
        public EmptyState(Turret turret) => _turret = turret;

        public void OnEnter() => _turret.SetColor(new Color(0.25f, 0.25f, 0.28f));
        public void OnExit() { }
        public void Tick(float deltaTime) { }
    }
}
