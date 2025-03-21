using System;

namespace DStateMachine
{
    /// <summary>
    /// Exception thrown when a transition cannot be performed.
    /// </summary>
    public class DStateMachineTransitionException : Exception
    {
        public DStateMachineTransitionException(string message)
            : base(message)
        {
        }
    }
}