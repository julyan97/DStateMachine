# DStateMachine

**DStateMachine** is a powerful and flexible asynchronous state machine library for .NET, designed with a clean, fluent API and production-ready architecture. It supports dynamic transitions, guard conditions, entry/exit hooks, and internal transitions, making it ideal for complex stateful workflows.

---

## ✨ Features

- **Generic Support**: Works with any type for states and triggers (e.g., `string`, `int`, `enum`).
- **Fluent API**: Concise and expressive DSL for configuration.
- **Asynchronous Execution**: Seamless async/await support for transitions and actions.
- **Entry and Exit Hooks**: Configure entry/exit actions per state.
- **Guard Clauses**: Conditionally block transitions.
- **Internal Transitions**: Perform side-effect actions without state change.
- **Dynamic Transitions**: Determine the destination state at runtime.
- **DOT Export**: Generate DOT-format graphs for visualization.

---

## 📚 Example Usage

```csharp
var sm = new DStateMachine<string, string>("A");

sm.Configure("A")
    .OnEntry(() => Console.WriteLine("Entering A"))
    .OnExit(() => Console.WriteLine("Exiting A"))
    .OnTrigger("toB", tb => tb.ChangeState("B"));

sm.Configure("B").OnEntry(() => Console.WriteLine("Entered B"));

await sm.FireAsync("toB");
Console.WriteLine(sm.CurrentState); // Output: B
```

---

## ✅ Feature Examples

### ✅ Generic Type Support
```csharp
var sm = new DStateMachine<int, int>(0);
sm.Configure(0).OnTrigger(1, tb => tb.ChangeState(2));
sm.Fire(1);
Console.WriteLine(sm.CurrentState); // Output: 2
```

### 🔁 Entry and Exit Actions
```csharp
var sm = new DStateMachine<string, string>("Init");
bool entered = false, exited = false;

sm.Configure("Init")
    .OnEntry(() => { entered = true; return Task.CompletedTask; })
    .OnExit(() => { exited = true; return Task.CompletedTask; })
    .OnTrigger("go", tb => tb.ChangeState("Done"));

sm.Configure("Done").OnEntry(() => Task.CompletedTask);
sm.Fire("go");
Console.WriteLine($"Entered: {entered}, Exited: {exited}"); // Output: Entered: False, Exited: True
```

### ⛔ Guard Clauses
```csharp
var sm = new DStateMachine<string, string>("A");
sm.Configure("A")
    .OnTrigger("toB", tb => tb.ChangeState("B").If(() => false));

sm.OnUnhandledTrigger((trigger, machine) => {
    Console.WriteLine("Blocked by guard");
    return Task.CompletedTask;
});

sm.Fire("toB"); // Output: Blocked by guard
```

### ⏳ Asynchronous Transitions
```csharp
var sm = new DStateMachine<string, string>("Start");
sm.Configure("Start")
    .OnTrigger("load", tb => tb.ChangeStateAsync(async () => {
        await Task.Delay(100);
        return "Loaded";
    }));

sm.Configure("Loaded").OnEntry(() => Task.CompletedTask);
await sm.FireAsync("load");
Console.WriteLine(sm.CurrentState); // Output: Loaded
```

### 🧠 Dynamic Transitions
```csharp
var sm = new DStateMachine<string, string>("A");
sm.Configure("A")
    .OnTrigger("toNext", tb => tb.ChangeState(() => DateTime.Now.Second % 2 == 0 ? "Even" : "Odd"));

sm.Configure("Even").OnEntry(() => Task.CompletedTask);
sm.Configure("Odd").OnEntry(() => Task.CompletedTask);

sm.Fire("toNext");
Console.WriteLine(sm.CurrentState); // Output: "Even" or "Odd"
```

### 🔁 Internal Transitions
```csharp
var sm = new DStateMachine<string, string>("Idle");
bool logged = false;

sm.Configure("Idle")
    .OnTrigger("ping", tb => tb.ExecuteAction(() => logged = true));

await sm.FireAsync("ping");
Console.WriteLine($"State: {sm.CurrentState}, Logged: {logged}");
// Output: State: Idle, Logged: True
```

### 💬 Fluent DSL
```csharp
var sm = new DStateMachine<string, string>("X");
sm.Configure("X")
    .OnTrigger("a", tb => tb.ChangeState("A"))
    .OnTrigger("b", tb => tb.ChangeState("B"));

Console.WriteLine(sm.Configure("X").Machine == sm); // Output: True
```

### 📈 DOT Graph Export
```csharp
var sm = new DStateMachine<string, string>("Start");
sm.Configure("Start").OnTrigger("toEnd", tb => tb.ChangeState("End"));
sm.Configure("End").OnEntry(() => Task.CompletedTask);

string dot = sm.ExportToDot();
Console.WriteLine(dot);
// Output: DOT-format string of the state machine
```

---

## 🎓 Getting Started

1. Clone the repository or add the files to your project.
2. Create a new instance: `new DStateMachine<TTrigger, TState>(initialState)`.
3. Configure states using `.Configure(state)` and chain `OnEntry`, `OnExit`, and `OnTrigger`.
4. Fire transitions using `Fire(trigger)` or `await FireAsync(trigger)`.

---

# DStateMachine Documentation

## `OnTrigger` and Transition States

### Overview

The `OnTrigger` method is part of the fluent API provided by `DStateMachine`. It configures state transitions based on triggers. Each transition may specify synchronous or asynchronous destination states, guard conditions, or internal actions.

### Method Signature

```csharp
public StateConfiguration<TTrigger, TState> OnTrigger(TTrigger trigger, Action<TransitionBuilder<TTrigger, TState>> config)
```

- `trigger`: The trigger causing the transition.
- `config`: Configuration action for defining the transitions.

---

## `TransitionBuilder<TTrigger, TState>`

### Overview

The `TransitionBuilder` class provides a fluent interface to define transitions for a given trigger. It supports:

- Fixed and dynamic destination states
- Asynchronous transitions
- Guard conditions
- Internal (side-effect-only) actions

### Methods

#### `ChangeState`
```csharp
public TransitionBuilder<TTrigger, TState> ChangeState(TState destination)
```
Transitions to a specific state.

#### `ChangeState(Func<TState> destinationSelector)`
```csharp
public TransitionBuilder<TTrigger, TState> ChangeState(Func<TState> destinationSelector)
```
Transitions dynamically based on the provided function.

#### `ChangeStateAsync(Func<Task<TState>> destinationSelector)`
```csharp
public TransitionBuilder<TTrigger, TState> ChangeStateAsync(Func<Task<TState>> destinationSelector)
```
Asynchronously determines the destination state.

#### `If(Func<bool> guard)`
```csharp
public TransitionBuilder<TTrigger, TState> If(Func<bool> guard)
```
Adds a synchronous guard to the most recent transition.

#### `IfAsync(Func<Task<bool>> asyncGuard)`
```csharp
public TransitionBuilder<TTrigger, TState> IfAsync(Func<Task<bool>> asyncGuard)
```
Adds an asynchronous guard.

#### `ExecuteAction(Action action = null)`
```csharp
public TransitionBuilder<TTrigger, TState> ExecuteAction(Action action = null)
```
Defines an internal transition with a synchronous side-effect.

#### `ExecuteActionAsync(Func<Task> actionAsync = null)`
```csharp
public TransitionBuilder<TTrigger, TState> ExecuteActionAsync(Func<Task> actionAsync = null)
```
Defines an internal transition with an asynchronous side-effect.

### Example Usage per Method

#### `ChangeState`
```csharp
.OnTrigger(Triggers.Start, t => t.ChangeState(States.Running))
```

#### `ChangeState (dynamic)`
```csharp
.OnTrigger(Triggers.Restart, t => t.ChangeState(() => ComputeNextState()))
```

#### `ChangeStateAsync`
```csharp
.OnTrigger(Triggers.Refresh, t => t.ChangeStateAsync(async () => await GetNextStateAsync()))
```

#### `If`
```csharp
.OnTrigger(Triggers.Start, t => t.ChangeState(States.Running).If(() => IsReady))
```

#### `IfAsync`
```csharp
.OnTrigger(Triggers.Start, t => t.ChangeStateAsync(GetRunningState).IfAsync(IsReadyAsync))
```

#### `ExecuteAction`
```csharp
.OnTrigger(Triggers.Ping, t => t.ExecuteAction(() => Console.WriteLine("Pinged!")))
```

#### `ExecuteActionAsync`
```csharp
.OnTrigger(Triggers.Ping, t => t.ExecuteActionAsync(async () => await LogPingAsync()))
```

---

## Combined Example

```csharp
enum States { Idle, Running, Stopped }
enum Triggers { Start, Stop, Pause, Ping }

var stateMachine = new DStateMachine<Triggers, States>(States.Idle);

stateMachine.Configure(States.Idle)
    .OnEntry(() => Console.WriteLine("Entering Idle"))
    .OnExit(() => Console.WriteLine("Exiting Idle"))
    .OnTrigger(Triggers.Start, t => t.ChangeState(States.Running).If(() => CanStart()))
    .OnTrigger(Triggers.Pause, t => t.ExecuteActionAsync(async () => await LogPauseAttempt()))
    .OnTrigger(Triggers.Ping, t => t.ExecuteAction(() => Console.WriteLine("Ping from Idle")));

stateMachine.Configure(States.Running)
    .OnTrigger(Triggers.Stop, t => t.ChangeStateAsync(async () => await DetermineStopState()))
    .OnTrigger(Triggers.Ping, t => t.ExecuteAction(() => Console.WriteLine("Ping from Running")));

await stateMachine.FireAsync(Triggers.Start);
```

---

## Handling Unhandled Triggers

You can define a handler for triggers without defined transitions:

```csharp
stateMachine.OnUnhandledTrigger(async (trigger, machine) =>
{
    await LogAsync($"Unhandled trigger {trigger} in state {machine.CurrentState}");
});
```

---

```csharp
public StateConfiguration<TTrigger, TState> OnEntry(Action<DStateMachine<TTrigger, TState>> action)
public StateConfiguration<TTrigger, TState> OnEntry(Func<Task> asyncAction)

public StateConfiguration<TTrigger, TState> OnExit(Action<DStateMachine<TTrigger, TState>> action)
public StateConfiguration<TTrigger, TState> OnExit(Func<Task> asyncAction)
```

- `OnEntry`: Defines an action that runs after the state is entered.
- `OnExit`: Defines an action that runs before the state is exited.
- Both support synchronous and asynchronous versions.

### Usage Examples

#### Synchronous Entry/Exit
```csharp
stateMachine.Configure(States.Idle)
    .OnEntry(sm => Console.WriteLine("Now in Idle"))
    .OnExit(sm => Console.WriteLine("Leaving Idle"));
```

#### Asynchronous Entry/Exit
```csharp
stateMachine.Configure(States.Running)
    .OnEntry(async () => await LogAsync("Entered Running"))
    .OnExit(async () => await LogAsync("Exited Running"));
```

You may define multiple entry or exit actions per state—they will be executed in the order they are registered.

---

## Best Practices

- Define guards clearly to ensure transitions occur under expected conditions.
- Use internal transitions (`ExecuteAction`, `ExecuteActionAsync`) for logging, notifications, or other side effects.
- Favor asynchronous methods for operations involving I/O or long-running tasks.
- Keep transition logic lightweight and non-blocking.




## 🙌 Contributions

Pull requests and issues are welcome! If you'd like to contribute improvements or new features, feel free to fork and open a PR.

---

## 📄 License

This project is licensed under the MIT License.

