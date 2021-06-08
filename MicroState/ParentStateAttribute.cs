using System;
using System.Collections.Generic;
using System.Text;

namespace MicroState
{
    /// <summary>
    /// Used to define the state hierarchy, by defining a parent state for a state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ParentStateAttribute : Attribute
    {
        internal Type Parent { get; }

        /// <summary>
        /// Constructs a new <see cref="ParentStateAttribute"/> instance.
        /// </summary>
        /// <param name="parent">The parent state type</param>
        public ParentStateAttribute(Type parent)
        {
            Parent = parent;
        }
    }
}
