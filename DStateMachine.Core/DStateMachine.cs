using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DStateMachine
{
    /// <summary>
    /// A generic, asynchronous state machine with support for guard conditions,
    /// dynamic transitions, entry/exit actions, and a concise lambda-based configuration API.
    /// </summary>
    public class DStateMachine<TTrigger, TState>
    {
        private TState _state;
        private readonly Dictionary<(TState, TTrigger), List<Transition<TState>>> _transitions = new Dictionary<(TState, TTrigger), List<Transition<TState>>>();
        private readonly Dictionary<TState, StateActions> _stateActions = new Dictionary<TState, StateActions>();

        /// <summary>
        /// Optional hook that is invoked when a trigger is fired but no valid transition is found.
        /// If set, the callback is awaited and no exception is thrown.
        /// </summary>
        private Func<TTrigger, DStateMachine<TTrigger, TState>, Task>? _onUnhandledTrigger;

        public DStateMachine(TState initialState)
        {
            _state = initialState;
        }

        /// <summary>
        /// Gets the current state.
        /// </summary>
        public TState CurrentState => _state;

        public void OnUnhandledTrigger(Func<TTrigger, DStateMachine<TTrigger, TState>, Task>? handler)
        {
            _onUnhandledTrigger = handler;
        }

        internal void AddTransition(
            TState source,
            TTrigger trigger,
            Func<Task<bool>> guard,
            Func<Task<TState>> destinationSelector,
            bool isInternal)
        {
            var key = (source, trigger);
            if (!_transitions.TryGetValue(key, out var list))
            {
                list = new List<Transition<TState>>();
                _transitions[key] = list;
            }
            list.Add(new Transition<TState>(guard, destinationSelector, isInternal));
        }

        /// <summary>
        /// Asynchronously fires the specified trigger.
        /// For a normal transition, exit actions are executed, state is updated, then entry actions are executed.
        /// For an internal transition, the associated delegate is executed without changing state or firing entry/exit actions.
        /// If no valid transition exists, OnUnhandledTrigger is invoked (if provided); otherwise, an exception is thrown.
        /// </summary>
        public async Task FireAsync(TTrigger trigger)
        {
            var key = (_state, trigger);
            if (!_transitions.TryGetValue(key, out var transitions))
            {
                if (_onUnhandledTrigger != null)
                {
                    await _onUnhandledTrigger(trigger, this).ConfigureAwait(false);
                    return;
                }
                else
                {
                    throw new DStateMachineTransitionException(
                        $"No transition defined for state '{_state}' with trigger '{trigger}'.");
                }
            }

            Transition<TState>? validTransition = null;
            foreach (var transition in transitions)
            {
                if (await transition.Guard().ConfigureAwait(false))
                {
                    validTransition = transition;
                    break;
                }
            }
            if (validTransition == null)
            {
                if (_onUnhandledTrigger != null)
                {
                    await _onUnhandledTrigger(trigger, this).ConfigureAwait(false);
                    return;
                }
                else
                {
                    throw new DStateMachineTransitionException(
                        $"No valid guard passed for state '{_state}' with trigger '{trigger}'.");
                }
            }

            if (validTransition.IsInternal)
            {
                // Internal transitions execute their delegate for side effects only.
                await validTransition.DestinationSelector().ConfigureAwait(false);
                return;
            }

            // Execute exit actions for the current state.
            if (_stateActions.TryGetValue(_state, out var currentActions))
            {
                foreach (var exitAction in currentActions.ExitActions)
                {
                    await exitAction().ConfigureAwait(false);
                }
            }

            var destination = await validTransition.DestinationSelector().ConfigureAwait(false);
            _state = destination;

            // Execute entry actions for the new state.
            if (_stateActions.TryGetValue(_state, out var newActions))
            {
                foreach (var entryAction in newActions.EntryActions)
                {
                    await entryAction().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Synchronously fires the specified trigger.
        /// </summary>
        public void Fire(TTrigger trigger)
        {
            FireAsync(trigger).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Begins configuration for the specified state.
        /// </summary>
        public StateConfiguration<TTrigger, TState> Configure(TState state)
        {
            GetOrCreateStateActions(state);
            return new StateConfiguration<TTrigger, TState>(state, this);
        }

        /// <summary>
        /// Exports the current state machine configuration as a DOT formatted string.
        /// </summary>
        public string ExportToDot()
        {
            var sb = new StringBuilder();
            sb.AppendLine("digraph StateMachine {");
            sb.AppendLine("    rankdir=LR;");
            // Iterate through each transition.
            foreach (var kvp in _transitions)
            {
                // Source state and trigger are the key.
                string source = kvp.Key.Item1?.ToString() ?? "null";
                string trigger = kvp.Key.Item2?.ToString() ?? "null";
                foreach (var transition in kvp.Value)
                {
                    string destination;
                    if (transition.IsInternal)
                    {
                        // For internal transitions, the state remains unchanged.
                        destination = source;
                    }
                    else
                    {
                        // Attempt to get the destination value.
                        try
                        {
                            var task = transition.DestinationSelector();
                            task.Wait(50); // wait briefly
                            destination = task.IsCompletedSuccessfully ? task.Result?.ToString() ?? "null" : "?";
                        }
                        catch
                        {
                            destination = "?";
                        }
                    }
                    string label = trigger;
                    if (transition.IsInternal)
                    {
                        label += " (internal)";
                    }
                    sb.AppendLine($"    \"{source}\" -> \"{destination}\" [ label = \"{label}\" ];");
                }
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        internal StateActions GetOrCreateStateActions(TState state)
        {
            if (!_stateActions.TryGetValue(state, out var actions))
            {
                actions = new StateActions();
                _stateActions[state] = actions;
            }
            return actions;
        }
    }
}
