using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core
{
    public class StateMachine
    {
        private IState _currentState;

        private Dictionary<Type, List<StateTransition>> _transitions = new Dictionary<Type, List<StateTransition>>();
        private List<StateTransition> _anyTransitions = new List<StateTransition>();

        public IState CurrentState => _currentState;

        public void Tick(float deltaTime)
        {
            IState newState = CheckTransition(_currentState);
            if (newState != _currentState) ChangeState(newState);

            _currentState.Tick(deltaTime);
        }
        
        private IState CheckTransition(IState currenState)
        {
            if (currenState == null) return null;
            
            // Are there any transitions available ?
            if (_anyTransitions.Count > 0)
            {
                IState anyStateAvailable = _anyTransitions.FirstOrDefault(t => t.Condition())?.NextState;
                if (anyStateAvailable != null) return anyStateAvailable;
            }
            
            // No any transitions check specific transitions
            // Check that Transitions exist, even so return same state 
            if (_transitions.Count <= 0)
            {
                Debug.LogWarning("no transitions set");
                return currenState;
            }
            // Try to find some transitions available
            if (_transitions.TryGetValue(currenState.GetType(), out List<StateTransition> stateTransitions))
            {
                IState stateAvailable = stateTransitions.FirstOrDefault(t => t.Condition())?.NextState;
                if (stateAvailable != null) return stateAvailable;
            }
            // No transitions, so return same state
            // Debug.LogWarning($"no transitions for that state {currenState.GetType()}");
            return currenState;

        }

        public void ChangeState(IState newStateTypes)
        {
            _currentState?.OnExit();
            _currentState = newStateTypes;
            _currentState?.OnEnter();
        }

        public void AddTransition(IState fromState, IState toState, Func<bool> condition)
        {
            if (fromState == null || toState == null && condition == null)
                return;

            var key = fromState.GetType();
            if (!_transitions.ContainsKey(key))
                _transitions.Add(key, new List<StateTransition>());

            _transitions[key].Add(new StateTransition(toState, condition));

        }
        public void AddAnyTransition(IState toState, Func<bool> condition)
        {
            _anyTransitions.Add(new StateTransition(toState, condition));
        }

    }

}
