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

        private readonly List<Func<DStateMachine<TTrigger, TState>, Task>> _defaultEntryActions = new List<Func<DStateMachine<TTrigger, TState>, Task>>();
        private readonly List<Func<DStateMachine<TTrigger, TState>, Task>> _defaultExitActions = new List<Func<DStateMachine<TTrigger, TState>, Task>>();


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


        /// <summary>
        /// Begins configuration for the specified state.
        /// </summary>
        public StateConfiguration<TTrigger, TState> Configure(TState state)
        {
            GetOrCreateStateActions(state);
            return new StateConfiguration<TTrigger, TState>(state, this);
        }

        public DStateMachine<TTrigger, TState> DefaultOnEntry(Func<DStateMachine<TTrigger, TState>, Task> action)
        {
            _defaultEntryActions.Add(action);
            return this;
        }

        public DStateMachine<TTrigger, TState> DefaultOnEntry(Action<DStateMachine<TTrigger, TState>> action)
        {
            _defaultEntryActions.Add(sm =>
            {
                action(sm);
                return Task.CompletedTask;
            });
            return this;
        }

        public DStateMachine<TTrigger, TState> DefaultOnExit(Func<DStateMachine<TTrigger, TState>, Task> action)
        {
            _defaultExitActions.Add(action);
            return this;
        }

        public DStateMachine<TTrigger, TState> DefaultOnExit(Action<DStateMachine<TTrigger, TState>> action)
        {
            _defaultExitActions.Add(sm =>
            {
                action(sm);
                return Task.CompletedTask;
            });
            return this;
        }

        /// <summary>
        /// Exports the current state machine configuration as a DOT formatted string.
        /// </summary>
        public string ExportToDot()
        {
            var sb = new StringBuilder();
            sb.AppendLine("digraph StateMachine {");
            sb.AppendLine("    // Graph layout settings");
            sb.AppendLine("    rankdir=LR;");
            sb.AppendLine("    splines=ortho;");
            sb.AppendLine("    nodesep=0.8;");
            sb.AppendLine("    ranksep=1.2;");
            sb.AppendLine();
            sb.AppendLine("    // Node and edge styling");
            sb.AppendLine("    node [shape=box, style=\"filled,rounded\", fontname=\"Segoe UI\", fontsize=10, margin=\"0.2,0.1\", fillcolor=\"#f0f0f0\"];");
            sb.AppendLine("    edge [fontname=\"Segoe UI\", fontsize=9];");
            sb.AppendLine();

            // Gather all states to ensure every node is declared.
            var states = new HashSet<string>();
            foreach (var kvp in _transitions)
            {
                string source = kvp.Key.Item1?.ToString() ?? "null";
                states.Add(source);
                foreach (var transition in kvp.Value)
                {
                    string destination;
                    if (transition.IsInternal)
                    {
                        destination = source;
                    }
                    else
                    {
                        try
                        {
                            var task = transition.DestinationSelector();
                            task.Wait(50); // Brief wait for the task to complete.
                            destination = task.IsCompletedSuccessfully ? task.Result?.ToString() ?? "null" : "?";
                        }
                        catch
                        {
                            destination = "?";
                        }
                    }

                    states.Add(destination);
                }
            }

            // Declare nodes explicitly.
            foreach (var state in states)
            {
                sb.AppendLine($"    \"{state}\" [label=\"{state}\"];");
            }

            sb.AppendLine();

            // Output transitions with combined edge attributes.
            foreach (var kvp in _transitions)
            {
                string source = kvp.Key.Item1?.ToString() ?? "null";
                string trigger = kvp.Key.Item2?.ToString() ?? "null";
                foreach (var transition in kvp.Value)
                {
                    string destination;
                    if (transition.IsInternal)
                    {
                        destination = source;
                    }
                    else
                    {
                        try
                        {
                            var task = transition.DestinationSelector();
                            task.Wait(50);
                            destination = task.IsCompletedSuccessfully ? task.Result?.ToString() ?? "null" : "?";
                        }
                        catch
                        {
                            destination = "?";
                        }
                    }

                    // If the transition is internal, style the edge as dashed and add a note.
                    string edgeLabel = transition.IsInternal ? $"{trigger} (internal)" : trigger;
                    string edgeAttributes = transition.IsInternal
                        ? $"[label=\"{edgeLabel}\", style=dashed, color=gray]"
                        : $"[label=\"{edgeLabel}\"]";

                    sb.AppendLine($"    \"{source}\" -> \"{destination}\" {edgeAttributes};");
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }


        /// <summary>
        /// Returns a string representing the state machine configuration as a tree-like diagram.
        /// </summary>
        public string VisualizeAsTree()
        {
            var sb = new StringBuilder();
            sb.AppendLine("State Machine Configuration:");
            sb.AppendLine();

            // Build a mapping from each source state to its list of transition strings.
            var stateTransitions = new Dictionary<string, List<string>>();

            // Include states from transitions.
            foreach (var kvp in _transitions)
            {
                var source = kvp.Key.Item1?.ToString() ?? "null";
                var trigger = kvp.Key.Item2?.ToString() ?? "null";

                if (!stateTransitions.ContainsKey(source))
                    stateTransitions[source] = new List<string>();

                foreach (var transition in kvp.Value)
                {
                    string destination;
                    if (transition.IsInternal)
                    {
                        destination = source;
                    }
                    else
                    {
                        try
                        {
                            var task = transition.DestinationSelector();
                            task.Wait(50); // Brief wait
                            destination = task.IsCompletedSuccessfully ? task.Result?.ToString() ?? "null" : "?";
                        }
                        catch
                        {
                            destination = "?";
                        }
                    }

                    string transitionType = transition.IsInternal ? "internal" : "normal";
                    string line = $"Trigger '{trigger}' -> {destination} ({transitionType})";
                    stateTransitions[source].Add(line);
                }
            }

            // Ensure that any state with actions but no transitions is included.
            foreach (var state in _stateActions.Keys)
            {
                var stateStr = state?.ToString() ?? "null";
                if (!stateTransitions.ContainsKey(stateStr))
                    stateTransitions[stateStr] = new List<string>();
            }

            // Format each state and its transitions as a tree.
            foreach (var kvp in stateTransitions)
            {
                string state = kvp.Key;
                var transitions = kvp.Value;

                sb.AppendLine(state);
                for (int i = 0; i < transitions.Count; i++)
                {
                    // Use '└──' for the last item, '├──' for others.
                    string branch = (i == transitions.Count - 1) ? "└── " : "├── ";
                    sb.AppendLine(branch + transitions[i]);
                }

                sb.AppendLine(); // Extra line for spacing between states.
            }

            return sb.ToString();
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

            // Execute state-specific exit actions.
            if (_stateActions.TryGetValue(_state, out var currentActions))
            {
                foreach (var exitAction in currentActions.ExitActions)
                {
                    await exitAction().ConfigureAwait(false);
                }
            }

            // Execute default exit actions, each receiving the state machine.
            foreach (var defaultExit in _defaultExitActions)
            {
                await defaultExit(this).ConfigureAwait(false);
            }

            var destination = await validTransition.DestinationSelector().ConfigureAwait(false);
            _state = destination;

            // Execute default entry actions, each receiving the state machine.
            foreach (var defaultEntry in _defaultEntryActions)
            {
                await defaultEntry(this).ConfigureAwait(false);
            }

            // Execute state-specific entry actions.
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

        internal StateActions GetOrCreateStateActions(TState state)
        {
            if (!_stateActions.TryGetValue(state, out var actions))
            {
                actions = new StateActions();
                _stateActions[state] = actions;
            }

            return actions;
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
    }
}