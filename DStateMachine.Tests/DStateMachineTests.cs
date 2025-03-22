using DStateMachine;

namespace Tests
{
    // For enum tests
    public enum TestState { A, B, C }
    public enum TestTrigger { X, Y, Z }

    public class DStateMachineTests
    {
        #region Generic State and Trigger Types

        [Fact]
        public async Task GenericTypes_String_Test()
        {
            var sm = new DStateMachine<string, string>("Start");
            sm.Configure("Start").OnTrigger("go", tb => tb.ChangeState("End"));
            sm.Fire("go");
            Assert.Equal("End", sm.CurrentState);
        }

        [Fact]
        public async Task GenericTypes_Enum_Test()
        {
            var sm = new DStateMachine<TestTrigger, TestState>(TestState.A);
            sm.Configure(TestState.A).OnTrigger(TestTrigger.X, tb => tb.ChangeState(TestState.B));
            sm.Fire(TestTrigger.X);
            Assert.Equal(TestState.B, sm.CurrentState);
        }

        [Fact]
        public async Task GenericTypes_Int_Test()
        {
            var sm = new DStateMachine<int, int>(0);
            sm.Configure(0).OnTrigger(1, tb => tb.ChangeState(2));
            sm.Fire(1);
            Assert.Equal(2, sm.CurrentState);
        }

        #endregion

        #region Entry/Exit Actions

        [Fact]
        public async Task EntryExitActions_Synchronous_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            bool entryCalled = false;
            bool exitCalled = false;
            sm.Configure("A")
                .OnEntry(() => { entryCalled = true; return Task.CompletedTask; })
                .OnExit(() => { exitCalled = true; return Task.CompletedTask; })
                .OnTrigger("toB", tb => tb.ChangeState("B"));
            sm.Configure("B").OnEntry(() => Task.CompletedTask);
            sm.Fire("toB");
            Assert.True(exitCalled);
            Assert.False(entryCalled);
            Assert.Equal("B", sm.CurrentState);
        }

        [Fact]
        public async Task EntryExitActions_Asynchronous_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            bool entryCalled = false;
            bool exitCalled = false;
            sm.Configure("A")
                .OnEntry(async () => { await Task.Delay(10); entryCalled = true; })
                .OnExit(async () => { await Task.Delay(10); exitCalled = true; })
                .OnTrigger("toB", tb => tb.ChangeState("B"));
            sm.Configure("B").OnEntry(() => Task.CompletedTask);
            await sm.FireAsync("toB");
            Assert.True(exitCalled);
            Assert.False(entryCalled);
            Assert.Equal("B", sm.CurrentState);
        }

        [Fact]
        public async Task EntryExitActions_PassMachine_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            string entryMessage = "";
            string exitMessage = "";
            sm.Configure("A")
                .OnEntry(m => { entryMessage = $"Entered: {m.CurrentState}"; })
                .OnExit(m => { exitMessage = $"Exited: {m.CurrentState}"; })
                .OnTrigger("toB", tb => tb.ChangeState("B"));
            sm.Configure("B").OnEntry(() => Task.CompletedTask);
            sm.Fire("toB");
            Assert.Equal("Exited: A", exitMessage);
            Assert.Empty(entryMessage);
        }

        #endregion

        #region Guard Clauses

        [Fact]
        public async Task GuardClauses_Transition_Prevented_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            sm.Configure("A").OnTrigger("toB", tb => tb.ChangeState("B").If(() => false));
            bool handled = false;
            sm.OnUnhandledTrigger(async (trigger, machine) => { handled = true; await Task.CompletedTask; });
            sm.Fire("toB");
            Assert.True(handled);
            Assert.Equal("A", sm.CurrentState);
        }

        [Fact]
        public async Task GuardClauses_Transition_Allowed_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            sm.Configure("A").OnTrigger("toB", tb => tb.ChangeState("B").If(() => true));
            sm.Configure("B").OnEntry(() => Task.CompletedTask);
            sm.Fire("toB");
            Assert.Equal("B", sm.CurrentState);
        }

        [Fact]
        public async Task GuardClauses_Multiple_Transitions_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            // Two transitions: first guard false, second guard true.
            sm.Configure("A").OnTrigger("toX", tb =>
            {
                tb.ChangeState("B").If(() => false);
                tb.ChangeState("C").If(() => true);
            });
            sm.Configure("C").OnEntry(() => Task.CompletedTask);
            sm.Fire("toX");
            Assert.Equal("C", sm.CurrentState);
        }

        #endregion

        #region Asynchronous Transitions

        [Fact]
        public async Task AsyncTransitions_Delay_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            sm.Configure("A").OnTrigger("toB", tb =>
                tb.ChangeStateAsync(async () =>
                {
                    await Task.Delay(100);
                    return "B";
                }));
            sm.Configure("B").OnEntry(() => Task.CompletedTask);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await sm.FireAsync("toB");
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds >= 100);
            Assert.Equal("B", sm.CurrentState);
        }

        [Fact]
        public async Task AsyncTransitions_Task_Return_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            sm.Configure("A").OnTrigger("toB", tb =>
                tb.ChangeStateAsync(async () =>
                {
                    await Task.Delay(50);
                    return "B";
                }));
            sm.Configure("B").OnEntry(() => Task.CompletedTask);
            Task fireTask = sm.FireAsync("toB");
            await fireTask;
            Assert.Equal("B", sm.CurrentState);
        }

        [Fact]
        public async Task AsyncTransitions_Multiple_Fire_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            sm.Configure("A").OnTrigger("toB", tb =>
                tb.ChangeStateAsync(async () =>
                {
                    await Task.Delay(10);
                    return "B";
                }));
            sm.Configure("B").OnTrigger("toC", tb =>
                tb.ChangeStateAsync(async () =>
                {
                    await Task.Delay(10);
                    return "C";
                }));
            sm.Configure("C").OnEntry(() => Task.CompletedTask);
            await sm.FireAsync("toB");
            await sm.FireAsync("toC");
            Assert.Equal("C", sm.CurrentState);
        }

        #endregion

        #region Dynamic State Transitions

        [Fact]
        public async Task DynamicTransitions_Synchronous_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            sm.Configure("A").OnTrigger("toDynamic", tb =>
                tb.ChangeState(() => "B"));
            sm.Configure("B").OnEntry(() => Task.CompletedTask);
            sm.Fire("toDynamic");
            Assert.Equal("B", sm.CurrentState);
        }

        [Fact]
        public async Task DynamicTransitions_Asynchronous_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            sm.Configure("A").OnTrigger("toDynamic", tb =>
                tb.ChangeStateAsync(async () =>
                {
                    await Task.Delay(20);
                    return "B";
                }));
            sm.Configure("B").OnEntry(() => Task.CompletedTask);
            await sm.FireAsync("toDynamic");
            Assert.Equal("B", sm.CurrentState);
        }

        [Fact]
        public async Task DynamicTransitions_Multiple_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            // Two dynamic transitions: first guard false, second guard true.
            sm.Configure("A").OnTrigger("toDynamic", tb =>
            {
                tb.ChangeState(() => "B").If(() => false);
                tb.ChangeState(() => "C").If(() => true);
            });
            sm.Configure("C").OnEntry(() => Task.CompletedTask);
            sm.Fire("toDynamic");
            Assert.Equal("C", sm.CurrentState);
        }

        #endregion

        #region Concise Lambda-Based Fluent DSL

        [Fact]
        public void FluentDSL_MachineAccess_StateConfiguration_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            var config = sm.Configure("A");
            Assert.NotNull(config.Machine);
            Assert.Equal(sm, config.Machine);
        }

        [Fact]
        public void FluentDSL_Chaining_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            // Chain multiple OnTrigger calls.
            sm.Configure("A")
                .OnTrigger("toB", tb => tb.ChangeState("B"))
                .OnTrigger("toC", tb => tb.ChangeState("C"));
            Assert.NotNull(sm.Configure("A").Machine);
        }

        #endregion

        #region Internal Transitions

        [Fact]
        public async Task InternalTransitions_StateNotChanged_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            sm.Configure("A").OnTrigger("internal", tb =>
                tb.ExecuteAction(() => Console.WriteLine("Internal action executed")));
            await sm.FireAsync("internal");
            Assert.Equal("A", sm.CurrentState);
        }

        [Fact]
        public async Task InternalTransitions_ActionExecuted_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            bool actionExecuted = false;
            sm.Configure("A").OnTrigger("internal", tb =>
                tb.ExecuteAction(() => actionExecuted = true));
            await sm.FireAsync("internal");
            Assert.True(actionExecuted);
        }

        [Fact]
        public async Task InternalTransitions_Multiple_Internal_Test()
        {
            var sm = new DStateMachine<string, string>("A");
            int counter = 0;
            sm.Configure("A").OnTrigger("internal", tb =>
            {
                tb.ExecuteAction(() => counter++);
            });
            await sm.FireAsync("internal");
            // Both internal transitions should execute but state remains "A".
            Assert.Equal("A", sm.CurrentState);
            Assert.Equal(1, counter);
        }
        
         [Fact]
        public async Task IgnoreDefaultEntryActions_Test()
        {
            int defaultEntryCounter = 0;
            int stateSpecificEntryCounter = 0;

            var sm = new DStateMachine<TestTrigger, TestState>(TestState.A);

            // Register a global default entry action.
            sm.DefaultOnEntry(machine => { defaultEntryCounter++;});

            // Configure StateB to ignore default entry actions and add its own entry action.
            sm.Configure(TestState.B)
              .IgnoreDefaultEntry()
              .OnEntry(machine => { stateSpecificEntryCounter++; });

            // Configure a transition from StateA to StateB.
            sm.Configure(TestState.A)
              .OnTrigger(TestTrigger.X, t => t.ChangeState(TestState.B));

            await sm.FireAsync(TestTrigger.X);

            // The global default entry action should be ignored.
            Assert.Equal(0, defaultEntryCounter);
            Assert.Equal(1, stateSpecificEntryCounter);
            Assert.Equal(TestState.B, sm.CurrentState);
        }

        [Fact]
        public async Task IgnoreDefaultExitActions_Test()
        {
            int defaultExitCounter = 0;
            int stateSpecificExitCounter = 0;

            var sm = new DStateMachine<TestTrigger, TestState>(TestState.A);

            // Register a global default exit action.
            sm.DefaultOnExit(machine => { defaultExitCounter++; });

            // Configure StateA to ignore default exit actions and add its own exit action.
            sm.Configure(TestState.A)
              .IgnoreDefaultExit()
              .OnExit(machine => { stateSpecificExitCounter++; })
              .OnTrigger(TestTrigger.X, t => t.ChangeState(TestState.B));

            // Configure StateB for completeness.
            sm.Configure(TestState.B)
              .OnEntry(machine => { });

            await sm.FireAsync(TestTrigger.X);

            // The global default exit action should be ignored.
            Assert.Equal(0, defaultExitCounter);
            Assert.Equal(1, stateSpecificExitCounter);
            Assert.Equal(TestState.B, sm.CurrentState);
        }

        [Fact]
        public async Task ExecuteAllActions_WhenNotIgnored_Test()
        {
            int defaultEntryCounter = 0;
            int defaultExitCounter = 0;
            int stateSpecificEntryCounter = 0;
            int stateSpecificExitCounter = 0;

            var sm = new DStateMachine<TestTrigger, TestState>(TestState.A);

            // Register global default entry and exit actions.
            sm.DefaultOnEntry(machine => { defaultEntryCounter++; return Task.CompletedTask; });
            sm.DefaultOnExit(machine => { defaultExitCounter++; return Task.CompletedTask; });

            // Configure StateA without ignoring default exit actions.
            sm.Configure(TestState.A)
              .OnExit(machine => { stateSpecificExitCounter++; })
              .OnTrigger(TestTrigger.X, t => t.ChangeState(TestState.B));

            // Configure StateB without ignoring default entry actions.
            sm.Configure(TestState.B)
              .OnEntry(machine => { stateSpecificEntryCounter++; });

            await sm.FireAsync(TestTrigger.X);

            // Both global default and state-specific actions should be executed.
            Assert.Equal(1, defaultExitCounter);
            Assert.Equal(1, stateSpecificExitCounter);
            Assert.Equal(1, defaultEntryCounter);
            Assert.Equal(1, stateSpecificEntryCounter);
            Assert.Equal(TestState.B, sm.CurrentState);
        }

        #endregion
    }
}
