using System;
using System.Threading.Tasks;

namespace DStateMachine
{
    /// <summary>
    /// Fluent configuration for a state.
    /// </summary>
    public class StateConfiguration<TTrigger, TState>
    {
        private readonly TState _state;
        private readonly DStateMachine<TTrigger, TState> _machine;

        internal StateConfiguration(TState state, DStateMachine<TTrigger, TState> machine)
        {
            _state = state;
            _machine = machine;
        }

        /// <summary>
        /// Gets the underlying state machine.
        /// </summary>
        public DStateMachine<TTrigger, TState> Machine => _machine;

        /// <summary>
        /// Configures the state to ignore the global default entry actions.
        /// </summary>
        public StateConfiguration<TTrigger, TState> IgnoreDefaultEntry()
        {
            _machine.GetOrCreateStateActions(_state).IgnoreDefaultEntry = true;
            return this;
        }

        /// <summary>
        /// Configures the state to ignore the global default exit actions.
        /// </summary>
        public StateConfiguration<TTrigger, TState> IgnoreDefaultExit()
        {
            _machine.GetOrCreateStateActions(_state).IgnoreDefaultExit = true;
            return this;
        }
        
        /// <summary>
        /// Registers an asynchronous action to execute when entering the state.
        /// </summary>
        public StateConfiguration<TTrigger, TState> OnEntry(Action<DStateMachine<TTrigger, TState>> action)
        {
            _machine.GetOrCreateStateActions(_state)
                .EntryActions.Add(() =>
                {
                    action(_machine);
                    return Task.CompletedTask;
                });
            return this;
        }

        /// <summary>
        /// Registers a synchronous action to execute when entering the state.
        /// </summary>
        public StateConfiguration<TTrigger, TState> OnEntry(Func<Task> action)
        {
            _machine.GetOrCreateStateActions(_state).EntryActions.Add(action);
            return this;
        }

        /// <summary>
        /// Registers an asynchronous action to execute when exiting the state.
        /// </summary>
        public StateConfiguration<TTrigger, TState> OnExit(Action<DStateMachine<TTrigger, TState>> action)
        {
            _machine.GetOrCreateStateActions(_state)
                .ExitActions.Add(() =>
                {
                    action(_machine);
                    return Task.CompletedTask;
                });
            return this;
        }

        /// <summary>
        /// Registers a synchronous action to execute when exiting the state.
        /// </summary>
        public StateConfiguration<TTrigger, TState> OnExit(Func<Task> action)
        {
            _machine.GetOrCreateStateActions(_state).ExitActions.Add(action);
            return this;
        }

        /// <summary>
        /// Registers an asynchronous entry action that receives the state machine.
        /// </summary>
        public StateConfiguration<TTrigger, TState> OnEntry(Func<DStateMachine<TTrigger, TState>, Task> asyncAction)
        {
            _machine.GetOrCreateStateActions(_state)
                .EntryActions.Add(() => asyncAction(_machine));
            return this;
        }

        /// <summary>
        /// Registers an asynchronous exit action that receives the state machine.
        /// </summary>
        public StateConfiguration<TTrigger, TState> OnExit(Func<DStateMachine<TTrigger, TState>, Task> asyncAction)
        {
            _machine.GetOrCreateStateActions(_state)
                .ExitActions.Add(() => asyncAction(_machine));
            return this;
        }

        /// <summary>
        /// Begins configuration for transitions triggered by the specified trigger,
        /// using a configuration lambda.
        /// </summary>
        public StateConfiguration<TTrigger, TState> OnTrigger(TTrigger trigger, Action<TransitionBuilder<TTrigger, TState>> config)
        {
            var builder = new TransitionBuilder<TTrigger, TState>(_state, trigger, _machine);
            config(builder);
            builder.Build();
            return this;
        }
    }
}
