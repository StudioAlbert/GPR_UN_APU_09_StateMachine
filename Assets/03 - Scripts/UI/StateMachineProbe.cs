using UnityEngine;

namespace Companion
{
    /// <summary>
    /// Ce qu'une démo expose pour que le panneau de debug puisse l'afficher.
    /// </summary>
    public interface IStateMachineProbe
    {
        /// <summary>Nom de la machine, affiché en titre.</summary>
        string MachineName { get; }

        /// <summary>L'état courant, ou null tant que la machine n'a pas démarré.</summary>
        Core.IState CurrentState { get; }

        /// <summary>Une ligne libre : ce qu'on veut voir en plus de l'état.</summary>
        string Details { get; }
    }

    public static class StateNames
    {
        /// <summary>« GuardPatrollingState » devient « Patrolling ».</summary>
        public static string Short(Core.IState state)
        {
            if (state == null) return "—";

            string name = state.GetType().Name;
            if (name.EndsWith("State") && name.Length > 5) name = name.Substring(0, name.Length - 5);
            return name;
        }
    }
}
