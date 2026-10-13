using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Companion
{
    /// <summary>
    /// Le présentateur du panneau de debug : il lit les machines de la scène et écrit dans
    /// le document UI Toolkit. Le markup vit dans un <c>.uxml</c>, l'habillage dans
    /// <c>StateMachineHud.uss</c>.
    ///
    /// Ce script ne fabrique et ne clone aucun élément d'interface : les panneaux existent
    /// dans le document parce que les machines existent dans la scène. Il se contente
    /// d'interroger le document (<c>Q</c>) et de poser du texte. C'est la règle en vrai
    /// projet : l'UI est un asset, pas du code.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StateMachineHud : MonoBehaviour
    {
        [Header("Le texte de la scène")]
        [SerializeField] private string sceneTitle = "Machine à états";
        [SerializeField, TextArea(2, 4)] private string controls = "";

        [Header("La machine principale, affichée en grand")]
        [SerializeField] private MonoBehaviour mainMachine;

        [Header("Les machines imbriquées, un panneau chacune")]
        [SerializeField] private NestedMachine[] cards = Array.Empty<NestedMachine>();

        /// <summary>Une machine imbriquée et le panneau du document qui l'attend.</summary>
        [Serializable]
        private class NestedMachine
        {
            public MonoBehaviour machine;

            [Tooltip("Le nom du panneau dans le document : card-player, card-ai…")]
            public string element;
        }

        private IStateMachineProbe _main;
        private Label _stateName, _stateDetails;
        private readonly List<Card> _cards = new List<Card>();

        /// <summary>Un panneau du document, et la sonde qu'il montre.</summary>
        private readonly struct Card
        {
            public readonly IStateMachineProbe Probe;
            public readonly Label State, Detail;

            public Card(IStateMachineProbe probe, Label state, Label detail)
            {
                Probe = probe;
                State = state;
                Detail = detail;
            }
        }

        /// <summary>
        /// En <c>Start</c> et pas en <c>OnEnable</c> : le document a fini de construire son
        /// arbre, donc les éléments sont là.
        /// </summary>
        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            _stateName = root.Q<Label>("state-name");
            _stateDetails = root.Q<Label>("state-details");

            Write(root.Q<Label>("context"), sceneTitle);
            Write(root.Q<Label>("controls"), controls);

            _main = Probe(mainMachine);
            BindCards(root);
            Refresh();
        }

        private void Update() => Refresh();

        /// <summary>L'état courant de chaque machine, une fois par image.</summary>
        private void Refresh()
        {
            if (_main != null)
            {
                Set(_stateName, StateNames.Short(_main.CurrentState));
                Write(_stateDetails, _main.Details);
            }

            foreach (Card card in _cards)
            {
                Set(card.State, StateNames.Short(card.Probe.CurrentState));
                Write(card.Detail, card.Probe.Details);
            }
        }

        /// <summary>Chaque machine imbriquée est liée au panneau nommé dans l'Inspector.</summary>
        private void BindCards(VisualElement root)
        {
            _cards.Clear();
            if (cards == null) return;

            foreach (NestedMachine nested in cards)
            {
                IStateMachineProbe probe = Probe(nested.machine);
                VisualElement card = root.Q<VisualElement>(nested.element);

                if (probe == null || card == null)
                {
                    Debug.LogWarning($"Panneau « {nested.element} » : rien à lier.", this);
                    continue;
                }

                Write(card.Q<Label>("card-name"), probe.MachineName);
                _cards.Add(new Card(probe, card.Q<Label>("card-state"), card.Q<Label>("card-detail")));
            }
        }

        private IStateMachineProbe Probe(MonoBehaviour behaviour)
        {
            if (behaviour == null) return null;

            if (behaviour is IStateMachineProbe probe) return probe;

            Debug.LogWarning($"{behaviour.GetType().Name} n'implémente pas IStateMachineProbe.", this);
            return null;
        }

        /// <summary>Pose un texte, et cache l'élément quand il n'y a rien à dire.</summary>
        private static void Write(Label label, string text)
        {
            if (label == null) return;

            Set(label, text);
            label.EnableInClassList("hidden", string.IsNullOrEmpty(text));
        }

        /// <summary>N'écrit que si la valeur a changé : pas de travail inutile par image.</summary>
        private static void Set(Label label, string text)
        {
            if (label == null || label.text == text) return;

            label.text = text;
        }
    }
}
