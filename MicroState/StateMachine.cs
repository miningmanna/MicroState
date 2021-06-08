using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroState
{
    /// <summary>
    /// Base class for the StateMachin<ST, CT> class. Used internally only.
    /// </summary>
    public abstract class StateMachine
    {
        internal virtual void SetState(Type start, Type dest)
        { }
    }

    /// <summary>
    /// Encapsulates a state machine allowing orthogonal and sub states.
    /// </summary>
    /// <typeparam name="ST">The base class of the state types</typeparam>
    /// <typeparam name="CT">The context's type of the states</typeparam>
    public class StateMachine<ST, CT> : StateMachine
        where ST : State<CT>
    {
        private StateTreeNode<ST, CT> _tree;
        
        /// <summary>
        /// Context for the states.
        /// </summary>
        public CT Context { get; private set; }
        
        /// <summary>
        /// Indicates if the state machine has any active states.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Creates a new StateMachine instance with the given context.
        /// </summary>
        /// <param name="context">Context of the states</param>
        public StateMachine(CT context)
        {
            if (typeof(ST) == typeof(State<CT>))
                throw new ArgumentException($"The state type must be a subtype of State<{typeof(CT).Name}>", nameof(ST));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            Context = context;
            _tree = new StateTreeNode<ST, CT>();

            Handle = StateHandleWrapper.Create(this);
        }

        /// <summary>
        /// Handle for the state machine. Calls to the event methods of the states via this handle.
        /// </summary>
        public ST Handle { get; private set; }

        internal override void SetState(Type start, Type dest)
        {
            if(!typeof(ST).IsAssignableFrom(start) || start == typeof(ST))
                throw new ArgumentException($"The type of the state must be a subtype of {typeof(ST).Name}>", nameof(start));
            if (!typeof(ST).IsAssignableFrom(dest) || dest == typeof(ST))
                throw new ArgumentException($"The type of the state must be a subtype of {typeof(ST).Name}>", nameof(dest));

            var startHierarchy = start.GetStateHierarchy();
            var destHierarchy = dest.GetStateHierarchy();
            var commonHierarchy = startHierarchy.Zip(destHierarchy, (a, b) => a == b ? a : null).Where((a) => a != null).ToList();

            var startLevel = startHierarchy.Count;
            var destLevel = destHierarchy.Count;
            var commonLevel = commonHierarchy.Count;

            // Enter
            var baseNode = _tree.GetNode(commonHierarchy);

            if(destLevel < startLevel)
            {
                // Exit
                var startTreeType = startHierarchy[commonHierarchy.Count];
                RemoveSubtree(baseNode, startTreeType);

                // Enter new state
                var newNode = new StateTreeNode<ST, CT>(CreateNewState(dest));
                baseNode.Leafs[dest] = newNode;
                newNode.State.OnEnter();

            }
            else if(destLevel > startLevel)
            {
                // Enter
                if (destLevel - startLevel != 1)
                    throw new ArgumentException("State type must be direct subtype of the current state", nameof(dest));
                if (commonHierarchy.Count != startLevel)
                    throw new ArgumentException("State type is not part of this state hierarchy", nameof(dest));

                var startNode = _tree.GetNode(startHierarchy);

                // Enter new state
                var newNode = new StateTreeNode<ST, CT>(CreateNewState(dest));
                startNode.Leafs[dest] = newNode;
                newNode.State.OnEnter();
            }
            else
            {
                // Switch
                if (commonHierarchy.Count != destLevel - 1 || commonHierarchy.Count != startLevel - 1)
                    throw new ArgumentException("State type is not on the same level or in the same tree", nameof(dest));

                RemoveSubtree(baseNode, start);

                // Enter new state
                var newNode = new StateTreeNode<ST, CT>(CreateNewState(dest));
                baseNode.Leafs[dest] = newNode;
                newNode.State.OnEnter();
            }

        }

        /// <summary>
        /// Start the state machine in the state given by STT.
        /// </summary>
        /// <typeparam name="STT">The state to start in</typeparam>
        public void Start<STT>()
            where STT : ST, new()
        {
            var sttType = typeof(STT);
            if (!typeof(ST).IsAssignableFrom(sttType) || sttType == typeof(ST))
                throw new ArgumentException($"The type of the state must be a subtype of {typeof(ST).Name}>", nameof(STT));
            if (Running)
                throw new InvalidOperationException("The state machine is still running");

            var state = CreateNewState(typeof(STT));

            _tree.Leafs[sttType] = new StateTreeNode<ST, CT>(state);
            state.OnEnter();
        }

        /// <summary>
        /// Force the state machine to exit all states.
        /// </summary>
        public void Exit() => ExitTo(null);

        private void ExitTo(Type t)
        {
            var queue = new Queue<StateTreeNode<ST, CT>>();
            _tree.GetQueueLeafsFirst(queue);

            while (queue.Count != 0)
            {
                var node = queue.Dequeue();
                node.State?.OnExit();
                node.Leafs.Clear();

                if (node.State.GetType() == t)
                    break;
            }
        }

        internal void DoLeafsFirst(Action<ST> action)
        {
            var queue = new Queue<StateTreeNode<ST, CT>>();
            _tree.GetQueueLeafsFirst(queue);

            while (queue.Count != 0)
            {
                var node = queue.Dequeue();
                if(node.State != null)
                    action(node.State);
            }
        }

        private ST CreateNewState(Type t)
        {
            var constructor = t.GetConstructor(new Type[] { });
            var newState = (ST)constructor.Invoke(new object[] { });
            newState._context = Context;
            newState._sm = this;
            return newState;
        }

        private void RemoveSubtree(StateTreeNode<ST, CT> baseNode, Type subtree)
        {
            // Deconstruct state subtree
            var startNode = baseNode.Leafs[subtree];
            var queue = new Queue<StateTreeNode<ST, CT>>();
            startNode.GetQueueLeafsFirst(queue);
            while (queue.Count != 0)
            {
                var node = queue.Dequeue();
                node.State?.OnExit();
                node.Leafs.Clear();
            }
            baseNode.Leafs.Remove(subtree);
        }

    }
}
