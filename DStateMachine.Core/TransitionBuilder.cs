using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DStateMachine
{
    /// <summary>
    /// Fluent builder for configuring transitions for a given trigger.
    /// </summary>
    public class TransitionBuilder<TTrigger, TState>
    {
        private readonly TState _sourceState;
        private readonly TTrigger _trigger;
        private readonly DStateMachine<TTrigger, TState> _machine;
        // Each pending transition is stored along with a flag indicating whether it is internal.
        private readonly List<(Func<bool> Guard, Func<Task<TState>> DestinationSelector, bool IsInternal)> _pendingTransitions = new List<(Func<bool> Guard, Func<Task<TState>> DestinationSelector, bool IsInternal)>();

        internal TransitionBuilder(TState sourceState, TTrigger trigger, DStateMachine<TTrigger, TState> machine)
        {
            _sourceState = sourceState;
            _trigger = trigger;
            _machine = machine;
        }

        /// <summary>
        /// Gets the underlying state machine.
        /// </summary>
        public DStateMachine<TTrigger, TState> Machine => _machine;

        /// <summary>
        /// Specifies a fixed transition to the given destination state.
        /// </summary>
        public TransitionBuilder<TTrigger, TState> ChangeState(TState destination)
        {
            _pendingTransitions.Add((() => true, () => Task.FromResult(destination), false));
            return this;
        }

        /// <summary>
        /// Specifies a synchronous dynamic transition. The destination is computed at firing time.
        /// </summary>
        public TransitionBuilder<TTrigger, TState> ChangeState(Func<TState> destinationSelector)
        {
            _pendingTransitions.Add((() => true, () => Task.FromResult(destinationSelector()), false));
            return this;
        }

        /// <summary>
        /// Specifies an asynchronous dynamic transition. The destination is computed at firing time.
        /// </summary>
        public TransitionBuilder<TTrigger, TState> ChangeStateAsync(Func<Task<TState>> destinationSelector)
        {
            _pendingTransitions.Add((() => true, destinationSelector, false));
            return this;
        }

        /// <summary>
        /// Applies a guard condition to the most recent transition.
        /// </summary>
        public TransitionBuilder<TTrigger, TState> If(Func<bool> guard)
        {
            if (_pendingTransitions.Count == 0)
                throw new InvalidOperationException("No transition available to apply a guard. Call ChangeState or ExecuteAction first.");

            var last = _pendingTransitions[^1];
            _pendingTransitions[_pendingTransitions.Count - 1] = (guard, last.DestinationSelector, last.IsInternal);
            return this;
        }

        /// <summary>
        /// Adds an alternative internal transition.
        /// Internal transitions execute their delegate for side effects only.
        /// </summary>
        public TransitionBuilder<TTrigger, TState> ExecuteAction(Action action = null)
        {
            _pendingTransitions.Add((() => true, () => { action?.Invoke(); return Task.FromResult(_sourceState); }, true));
            return this;
        }

        /// <summary>
        /// Finalizes and registers all pending transitions.
        /// </summary>
        internal void Build()
        {
            foreach (var (guard, destinationSelector, isInternal) in _pendingTransitions)
            {
                Func<Task<bool>> asyncGuard = () => Task.FromResult(guard());
                _machine.AddTransition(_sourceState, _trigger, asyncGuard, destinationSelector, isInternal);
            }
        }
    }
}
