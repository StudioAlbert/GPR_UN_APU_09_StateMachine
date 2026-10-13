using UnityEngine;

namespace Companion.TurnBased
{
    /// <summary>
    /// Scène 04 — deux machines emboîtées, comme dans le schéma du cours.
    /// En haut, la partie : PlayerTurn, AiTurn, GameOver. En bas, chaque tourelle :
    /// Waiting, Active, Empty. Entrer dans un tour active la tourelle de ce camp
    /// (<c>OnEnter</c>), et la transition du haut lit l'état du bas.
    ///
    /// Deux tourelles se tirent dessus. Le joueur passe la main avec le bouton
    /// « Tour suivant » ; l'IA passe quand elle est à court de munitions.
    /// </summary>
    public class TurnBasedStateMachine : MonoBehaviour, IStateMachineProbe
    {
        [SerializeField] private Turret playerTurret;
        [SerializeField] private Turret aiTurret;
        [SerializeField] private float aiShotInterval = 0.7f;

        private readonly Core.StateMachine _machine = new Core.StateMachine();

        private PlayerTurnState _playerTurn;
        private AiTurnState _aiTurn;

        private bool _nextPressed;

        public string MachineName => "Partie";
        public Core.IState CurrentState => _machine.CurrentState;

        public string Details =>
            $"joueur {playerTurret.Ammo} munitions / {playerTurret.Hull} PV · " +
            $"IA {aiTurret.Ammo} munitions / {aiTurret.Hull} PV";

        private void Awake()
        {
            _playerTurn = new PlayerTurnState(playerTurret, aiTurret);
            _aiTurn = new AiTurnState(aiTurret, playerTurret, aiShotInterval);
            var gameOver = new GameOverState();

            _machine.ChangeState(_playerTurn);

            // Déclarées du plus urgent au moins urgent : la fin de partie passe avant tout.
            _machine.AddAnyTransition(gameOver, () => playerTurret.Destroyed || aiTurret.Destroyed);
            _machine.AddTransition(_playerTurn, _aiTurn, ConsumeNext);
            _machine.AddTransition(_aiTurn, _playerTurn, () => _aiTurn.TurnFinished);
        }

        private void Update() => _machine.Tick(Time.deltaTime);

        private bool ConsumeNext()
        {
            if (!_nextPressed) return false;

            _nextPressed = false;
            return true;
        }

        /// <summary>Câblé sur le bouton « Tour suivant ».</summary>
        public void PressNext() => _nextPressed = true;

        /// <summary>Câblé sur le bouton « Tirer » : n'a d'effet que pendant le tour du joueur.</summary>
        public void PressFire()
        {
            if (_machine.CurrentState == _playerTurn) playerTurret.Fire(aiTurret);
        }
    }

    /// <summary>Le tour du joueur : on active sa tourelle en entrant, on la rend en sortant.</summary>
    public class PlayerTurnState : Core.IState
    {
        private readonly Turret _player, _ai;

        public PlayerTurnState(Turret player, Turret ai)
        {
            _player = player;
            _ai = ai;
        }

        public void OnEnter()
        {
            _player.Activate();
            _ai.Deactivate();
        }

        public void OnExit() => _player.Deactivate();

        public void Tick(float deltaTime) { }
    }

    /// <summary>Le tour de l'IA : elle tire à intervalle régulier, jusqu'à être à sec.</summary>
    public class AiTurnState : Core.IState
    {
        private readonly Turret _ai, _target;
        private readonly float _interval;
        private float _sinceLastShot;

        public AiTurnState(Turret ai, Turret target, float interval)
        {
            _ai = ai;
            _target = target;
            _interval = interval;
        }

        /// <summary>Condition de la transition du haut : elle lit l'état de la tourelle.</summary>
        public bool TurnFinished => _ai.OutOfAmmo;

        public void OnEnter()
        {
            _sinceLastShot = 0f;
            _ai.Activate();
            _target.Deactivate();
        }

        public void OnExit() => _ai.Deactivate();

        public void Tick(float deltaTime)
        {
            _sinceLastShot += deltaTime;
            if (_sinceLastShot < _interval) return;

            _sinceLastShot = 0f;
            _ai.Fire(_target);
        }
    }

    public class GameOverState : Core.IState
    {
        public void OnEnter() { }
        public void OnExit() { }
        public void Tick(float deltaTime) { }
    }
}
