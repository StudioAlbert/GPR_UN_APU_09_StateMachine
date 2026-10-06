using UnityEngine;

namespace Companion.Qte
{
    /// <summary>
    /// Démo 3 — Invite, Confirm, Fail, Success, Closing.
    /// Un état = un panneau d'UI. <c>OnEnter</c> l'affiche, <c>OnExit</c> le cache :
    /// il n'y a aucun <c>SetActive(false)</c> ailleurs dans le code.
    /// Les transitions sont déclenchées par les boutons du Canvas.
    ///
    /// À remarquer : chaque état a sa propre classe, même quand deux états font la
    /// même chose. La machine indexe ses transitions par <c>GetType()</c>, donc deux
    /// états de la même classe partageraient leurs transitions.
    /// </summary>
    public class QteDemo : MonoBehaviour, IStateMachineProbe
    {
        [Header("Un panneau par état")]
        [SerializeField] private GameObject invitePanel;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private GameObject failPanel;
        [SerializeField] private GameObject successPanel;
        [SerializeField] private GameObject closingPanel;

        [Header("Durées")]
        [SerializeField] private float inviteTimeout = 3f;
        [SerializeField] private float resultDwell = 1.2f;
        [SerializeField] private float closingDelay = 0.8f;

        private readonly Core.StateMachine _machine = new Core.StateMachine();

        private InviteState _invite;
        private ConfirmState _confirm;
        private FailState _fail;
        private SuccessState _success;
        private ClosingState _closing;

        /// <summary>Armé par les boutons du Canvas, consommé par les conditions.</summary>
        private string _pressed;

        public string MachineName => "QTE";
        public Core.IState CurrentState => _machine.CurrentState;

        public string Details => _machine.CurrentState == _invite
            ? $"il reste {_invite.TimeLeft:0.0} s"
            : "les boutons du Canvas sont les transitions";

        private void Awake()
        {
            _invite = new InviteState(invitePanel, inviteTimeout);
            _confirm = new ConfirmState(confirmPanel);
            _fail = new FailState(failPanel, resultDwell);
            _success = new SuccessState(successPanel, resultDwell);
            _closing = new ClosingState(closingPanel, closingDelay);

            _machine.ChangeState(_invite);

            // Invite : le bouton mène à Confirm, le chrono mène à Fail.
            _machine.AddTransition(_invite, _confirm, () => Consume("confirm"));
            _machine.AddTransition(_invite, _fail, () => _invite.TimedOut);

            // Confirm : deux boutons, deux issues.
            _machine.AddTransition(_confirm, _success, () => Consume("ok"));
            _machine.AddTransition(_confirm, _fail, () => Consume("cancel"));

            // Les deux issues se referment, et Closing relance l'invite.
            _machine.AddTransition(_success, _closing, () => _success.DwellElapsed);
            _machine.AddTransition(_fail, _closing, () => _fail.DwellElapsed);
            _machine.AddTransition(_closing, _invite, () => _closing.DwellElapsed);
        }

        private void Update() => _machine.Tick(Time.deltaTime);

        /// <summary>Un bouton arme une intention ; la condition la consomme une seule fois.</summary>
        private bool Consume(string action)
        {
            if (_pressed != action) return false;

            _pressed = null;
            return true;
        }

        /// <summary>Câblé sur le OnClick des boutons : "confirm", "ok", "cancel".</summary>
        public void Press(string action) => _pressed = action;
    }

    /// <summary>Un état qui affiche son panneau pendant au moins <c>dwell</c> secondes.</summary>
    public abstract class PanelState : Core.IState
    {
        private readonly GameObject _panel;
        private readonly float _dwell;
        private float _elapsed;

        protected PanelState(GameObject panel, float dwell = 0f)
        {
            _panel = panel;
            _dwell = dwell;
            if (_panel != null) _panel.SetActive(false);
        }

        public bool DwellElapsed => _elapsed >= _dwell;

        public virtual void OnEnter()
        {
            _elapsed = 0f;
            if (_panel != null) _panel.SetActive(true);
        }

        public virtual void OnExit()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        public virtual void Tick(float deltaTime) => _elapsed += deltaTime;
    }

    public class InviteState : PanelState
    {
        private readonly float _timeout;

        public InviteState(GameObject panel, float timeout) : base(panel) => _timeout = timeout;

        public float TimeLeft { get; private set; }
        public bool TimedOut => TimeLeft <= 0f;

        public override void OnEnter()
        {
            base.OnEnter();
            TimeLeft = _timeout;
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            TimeLeft -= deltaTime;
        }
    }

    public class ConfirmState : PanelState
    {
        public ConfirmState(GameObject panel) : base(panel) { }
    }

    public class SuccessState : PanelState
    {
        public SuccessState(GameObject panel, float dwell) : base(panel, dwell) { }
    }

    public class FailState : PanelState
    {
        public FailState(GameObject panel, float dwell) : base(panel, dwell) { }
    }

    public class ClosingState : PanelState
    {
        public ClosingState(GameObject panel, float dwell) : base(panel, dwell) { }
    }
}
