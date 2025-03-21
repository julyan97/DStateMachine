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

## 🙌 Contributions

Pull requests and issues are welcome! If you'd like to contribute improvements or new features, feel free to fork and open a PR.

---

## 📄 License

This project is licensed under the MIT License.

