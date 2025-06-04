using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DStateMachine
{
    /// <summary>
    /// Fluent configuration for multiple states at once.
    /// Allows applying the same entry, exit and trigger configuration
    /// to a collection of states.
    /// </summary>
    public class MultiStateConfiguration<TTrigger, TState>
    {
        private readonly IReadOnlyCollection<TState> _states;
        private readonly DStateMachine<TTrigger, TState> _machine;

        internal MultiStateConfiguration(IEnumerable<TState> states, DStateMachine<TTrigger, TState> machine)
        {
            _states = states.ToArray();
            _machine = machine;
        }

        /// <summary>
        /// Gets the underlying state machine.
        /// </summary>
        public DStateMachine<TTrigger, TState> Machine => _machine;

        public MultiStateConfiguration<TTrigger, TState> IgnoreDefaultEntry()
        {
            foreach (var state in _states)
                _machine.GetOrCreateStateActions(state).IgnoreDefaultEntry = true;
            return this;
        }

        public MultiStateConfiguration<TTrigger, TState> IgnoreDefaultExit()
        {
            foreach (var state in _states)
                _machine.GetOrCreateStateActions(state).IgnoreDefaultExit = true;
            return this;
        }

        public MultiStateConfiguration<TTrigger, TState> OnEntry(Action<DStateMachine<TTrigger, TState>> action)
        {
            foreach (var state in _states)
                _machine.ForState(state).OnEntry(action);
            return this;
        }

        public MultiStateConfiguration<TTrigger, TState> OnEntry(Func<Task> action)
        {
            foreach (var state in _states)
                _machine.ForState(state).OnEntry(action);
            return this;
        }

        public MultiStateConfiguration<TTrigger, TState> OnEntry(Func<DStateMachine<TTrigger, TState>, Task> asyncAction)
        {
            foreach (var state in _states)
                _machine.ForState(state).OnEntry(asyncAction);
            return this;
        }

        public MultiStateConfiguration<TTrigger, TState> OnExit(Action<DStateMachine<TTrigger, TState>> action)
        {
            foreach (var state in _states)
                _machine.ForState(state).OnExit(action);
            return this;
        }

        public MultiStateConfiguration<TTrigger, TState> OnExit(Func<Task> action)
        {
            foreach (var state in _states)
                _machine.ForState(state).OnExit(action);
            return this;
        }

        public MultiStateConfiguration<TTrigger, TState> OnExit(Func<DStateMachine<TTrigger, TState>, Task> asyncAction)
        {
            foreach (var state in _states)
                _machine.ForState(state).OnExit(asyncAction);
            return this;
        }

        public MultiStateConfiguration<TTrigger, TState> OnTrigger(TTrigger trigger, Action<TransitionBuilder<TTrigger, TState>> config)
        {
            foreach (var state in _states)
            {
                var builder = new TransitionBuilder<TTrigger, TState>(state, trigger, _machine);
                config(builder);
                builder.Build();
            }
            return this;
        }
    }
}
