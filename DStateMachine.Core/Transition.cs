using System;
using System.Threading.Tasks;

namespace DStateMachine
{
    /// <summary>
    /// Internal representation of a state transition.
    /// </summary>
    internal class Transition<TState>
    {
        public Func<Task<bool>> Guard { get; }
        public Func<Task<TState>> DestinationSelector { get; }
        public bool IsInternal { get; }

        public Transition(Func<Task<bool>> guard, Func<Task<TState>> destinationSelector, bool isInternal)
        {
            Guard = guard;
            DestinationSelector = destinationSelector;
            IsInternal = isInternal;
        }
    }
}