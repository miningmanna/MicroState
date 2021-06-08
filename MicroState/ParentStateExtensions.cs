using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MicroState
{
    public static class ParentStateExtensions
    {
        /// <summary>
        /// Gets a list representing the hierarchy of the state type from highest to lowest.
        /// </summary>
        /// <param name="o">The state object</param>
        /// <returns>A list representing the state hierarchy</returns>
        public static IList<Type> GetStateHierarchy(this object o)
        {
            if (o == null)
                throw new ArgumentNullException(nameof(o));
            return GetStateHierarchy(o.GetType());
        }

        /// <summary>
        /// Gets a list representing the hierarchy of the state type from highest to lowest.
        /// </summary>
        /// <param name="t">The state type</param>
        /// <returns>A list representing the state hierarchy</returns>
        public static IList<Type> GetStateHierarchy(this Type t)
        {
            var list = new List<Type>();
            GetStateHierarchy(t, list);
            list.Reverse();
            return list;
        }

        private static void GetStateHierarchy(Type t, IList<Type> s)
        {
            if (!typeof(State).IsAssignableFrom(t) || t == typeof(State) || t.BaseType == typeof(State))
                throw new ArgumentException("The type must be a subtype of State<CT>", nameof(t));

            s.Add(t);
            var at = (ParentStateAttribute) t.GetCustomAttributes().FirstOrDefault((a) => a is ParentStateAttribute);

            if (at == null)
                return;

            var parent = at.GetType();
            GetStateHierarchy(at.Parent, s);
        }

    }
}
