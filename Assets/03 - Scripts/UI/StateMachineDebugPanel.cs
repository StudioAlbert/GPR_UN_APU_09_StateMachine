using System.Collections.Generic;
using UnityEngine;

namespace Companion
{
    /// <summary>
    /// Panneau de debug commun aux quatre démos : liste chaque machine de la scène
    /// et son état courant. En IMGUI pour qu'une scène n'ait rien à câbler.
    /// </summary>
    public class StateMachineDebugPanel : MonoBehaviour
    {
        [SerializeField] private string sceneTitle = "Machine à états";
        [SerializeField] private string controls = "";

        private readonly List<IStateMachineProbe> _probes = new List<IStateMachineProbe>();
        private GUIStyle _title, _label, _state, _hint;

        private void Start()
        {
            _probes.Clear();
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour is IStateMachineProbe probe) _probes.Add(probe);
        }

        private void OnGUI()
        {
            BuildStyles();

            GUILayout.BeginArea(new Rect(16, 16, 440, 120 + 96 * _probes.Count));
            GUILayout.Label(sceneTitle, _title);

            foreach (IStateMachineProbe probe in _probes)
            {
                GUILayout.Space(8);
                GUILayout.Label(probe.MachineName, _label);
                GUILayout.Label(StateNames.Short(probe.CurrentState), _state);
                if (!string.IsNullOrEmpty(probe.Details)) GUILayout.Label(probe.Details, _hint);
            }

            if (!string.IsNullOrEmpty(controls))
            {
                GUILayout.Space(12);
                GUILayout.Label(controls, _hint);
            }

            GUILayout.EndArea();
        }

        private void BuildStyles()
        {
            if (_title != null) return;

            _title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            _state = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            _hint = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };

            _title.normal.textColor = Color.white;
            _label.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
            _state.normal.textColor = new Color(1f, 0.25f, 0.2f);
            _hint.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
        }
    }
}
