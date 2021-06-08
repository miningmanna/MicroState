using System;
using System.Collections.Generic;
using System.Text;

namespace MicroState
{
    internal class StateTreeNode<ST, CT>
        where ST : State<CT>
    {
        public ST State { get; private set; }
        public Dictionary<Type, StateTreeNode<ST, CT>> Leafs { get; private set; }

        public StateTreeNode(ST state = null)
        {
            Leafs = new Dictionary<Type, StateTreeNode<ST, CT>>();
            State = state;
        }

        public StateTreeNode<ST, CT> GetNode(IEnumerable<Type> keys)
        {
            var node = this;
            foreach(var key in keys)
            {
                if(!node.Leafs.ContainsKey(key))
                    break;

                node = node.Leafs[key];
            }
            return node;
        }

        public void GetQueueLeafsFirst(Queue<StateTreeNode<ST, CT>> queue)
        {
            foreach(var leaf in Leafs)
                leaf.Value.GetQueueLeafsFirst(queue);
            if(State != null)
                queue.Enqueue(this);
        }

    }
}
