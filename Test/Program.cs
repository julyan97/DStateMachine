using System;
using DStateMachine;

class Program
{
    static void Main()
    {
        var machine = new DStateMachine<string, string>("Pesho");

        machine.OnUnhandledTrigger(
            (trigger, sm) =>
            {
                 Console.WriteLine($"Unhandled trigger '{sm.CurrentState}' in state '{trigger}'");
                return Task.CompletedTask;
            });

        machine.Configure("Pesho")
            .OnEntry(m => Console.WriteLine($"Entering Pesho; current state: {m.CurrentState}"))
            .OnExit(m => Console.WriteLine("Exiting Pesho"))
            .OnTrigger("smeni", builder => 
                builder
                    .ChangeState("Pesho2").If(() => false) 
                    .ExecuteAction(() => Console.WriteLine("Internal transition executed")).If(() => false)  
                    );
               
        machine.Configure("Pesho2")
            .OnEntry(m => Console.WriteLine($"Entering Pesho2; current state: {m.CurrentState}"))
            .OnExit(m => Console.WriteLine("Exiting Pesho2"))
            .OnTrigger("smeni2", builder =>
                builder.ChangeState("Pesho"))
            .OnTrigger("smeni", builder =>
                builder.ChangeState("Pesho2"));

        machine.Fire("smeni");
        
        string dotGraph = machine.ExportToDot();
        Console.WriteLine(dotGraph);

    }
}