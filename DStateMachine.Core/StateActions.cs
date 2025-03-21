using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DStateMachine
{
    /// <summary>
    /// Holds the entry and exit actions for a state.
    /// </summary>
    internal class StateActions
    {
        public List<Func<Task>> EntryActions { get; } = new List<Func<Task>>();
        public List<Func<Task>> ExitActions { get; } = new List<Func<Task>>();
    }
}