using System;

namespace Core
{
    public interface IState
    {
        void OnEnter();
        void OnExit();
        void Tick(float deltaTime);
    }

    public class StateTransition
    {

        public Func<bool> Condition { get; }
        public IState NextState { get; }

        public StateTransition(IState nextState, Func<bool> condition)
        {
            NextState = nextState;
            Condition = condition;
        }

    }
}
