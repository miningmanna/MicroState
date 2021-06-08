using System;
using System.Collections.Generic;
using System.Text;

namespace MicroState
{
    /// <summary>
    /// Base class for the State<CT> class. Used only internally.
    /// </summary>
    public abstract class State
    { }

    /// <summary>
    /// Base class for states classes used with <see cref="StateMachine{ST, CT}"/>.
    /// </summary>
    /// <typeparam name="CT">The context class</typeparam>
    public abstract class State<CT> : State
    {
        internal StateMachine _sm;
        internal CT _context;

        /// <summary>
        /// Context to manipulate in the state.
        /// </summary>
        protected CT Context { get => _context; }

        /// <summary>
        /// OnEnter method. Called when the state is entered.
        /// </summary>
        public virtual void OnEnter() { }

        /// <summary>
        /// OnExit method. Called when the state is exited.
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// Transitions from this state to the state given by NST.
        /// If the new state is a substate of the current state, this method can be called to create multiple (orthogonal) substates.
        /// If the new state is a state higher in the hierarchy, the state machine exits the necesarry states and enters the state given by NST.
        /// </summary>
        /// <typeparam name="NST">The state class to enter</typeparam>
        protected void SetState<NST>()
            where NST : State<CT>, new()
        {
            _sm.SetState(GetType(), typeof(NST));
        }

        /// <summary>
        /// Exit all states.
        /// </summary>
        protected void Exit()
        {
            _sm.SetState(GetType(), null);
        }
    }
}
