using Autofac;
using Autofac.Configuration;
using Microsoft.Extensions.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<UnitOfWork>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();

        builder.RegisterType<TransactionContext>()
            .As<ITransactionContext>()
            .InstancePerMatchingLifetimeScope("transaction");

        builder.RegisterType<StepOneService>();
        builder.RegisterType<StepTwoService>();
        builder.RegisterType<TransactionProcessor>();

        builder.RegisterType<CatCalc>().AsSelf();
        builder.RegisterType<PlusCalc>().AsSelf();
        builder.RegisterType<StateCalc>()
            .WithParameter("counter", 0)
            .As<ICalculator>()
            .SingleInstance()
            .Named<ICalculator>("state");

        builder.Register<Worker>(c => new Worker(c.Resolve<CatCalc>()));

        builder.Register<Worker>(c =>
        {
            var stateCalc = c.ResolveNamed<ICalculator>("state");
            return new Worker((StateCalc)stateCalc);
        }).Named<Worker>("state");

        builder.Register<Worker2>(c =>
        {
            var worker = new Worker2();
            worker.SetCalculator(c.Resolve<PlusCalc>());
            return worker;
        });

        builder.Register<Worker2>(c =>
        {
            var worker = new Worker2();
            worker.SetCalculator(c.ResolveNamed<ICalculator>("state"));
            return worker;
        }).Named<Worker2>("state");

        builder.Register<Worker3>(c =>
        {
            var worker = new Worker3();
            worker.Calculator = c.Resolve<CatCalc>();
            return worker;
        });

        builder.Register<Worker3>(c =>
        {
            var worker = new Worker3();
            worker.Calculator = c.ResolveNamed<ICalculator>("state");
            return worker;
        }).Named<Worker3>("state");

        var container = builder.Build();

        Console.WriteLine("=== Default workers ===");
        using (var scope = container.BeginLifetimeScope())
        {
            var worker = scope.Resolve<Worker>();
            worker.Work("Hello, ", "World!");

            var worker2 = scope.Resolve<Worker2>();
            worker2.Work("10", "20");

            var worker3 = scope.Resolve<Worker3>();
            worker3.Work("Foo", "Bar");
        }

        Console.WriteLine("\n=== 'state' Workers ===");
        using (var scope = container.BeginLifetimeScope())
        {
            var workerState = scope.ResolveNamed<Worker>("state");
            workerState.Work("Test", "State");

            var worker2State = scope.ResolveNamed<Worker2>("state");
            worker2State.Work("State", "Test");

            var worker3State = scope.ResolveNamed<Worker3>("state");
            worker3State.Work("A", "B");
        }

        Console.WriteLine("\n=== UnitOfWork ===");
        using (var scope = container.BeginLifetimeScope())
        {
            var uow1 = scope.Resolve<IUnitOfWork>();
            var uow2 = scope.Resolve<IUnitOfWork>();
            Console.WriteLine($"UnitOfWork 1 ID: {uow1.Id}");
            Console.WriteLine($"UnitOfWork 2 ID: {uow2.Id}");
            Console.WriteLine($"Same instance: {uow1.Id == uow2.Id}");
        }

        Console.WriteLine("\n=== TransactionContext ===");
        using (var scope = container.BeginLifetimeScope("transaction"))
        {
            var processor = scope.Resolve<TransactionProcessor>();
            processor.Process();
        }

        Console.WriteLine("Super");
    }

    public static IContainer BuildImperativeContainer()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<UnitOfWork>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();

        builder.RegisterType<TransactionContext>()
            .As<ITransactionContext>()
            .InstancePerMatchingLifetimeScope("transaction");

        builder.RegisterType<StepOneService>();
        builder.RegisterType<StepTwoService>();
        builder.RegisterType<TransactionProcessor>();

        builder.RegisterType<CatCalc>().AsSelf();
        builder.RegisterType<PlusCalc>().AsSelf();
        builder.RegisterType<StateCalc>()
            .WithParameter("counter", 0)
            .As<ICalculator>()
            .SingleInstance()
            .Named<ICalculator>("state");

        builder.Register<Worker>(c => new Worker(c.Resolve<CatCalc>()));

        builder.Register<Worker>(c =>
        {
            var stateCalc = c.ResolveNamed<ICalculator>("state");
            return new Worker((StateCalc)stateCalc);
        }).Named<Worker>("state");

        builder.Register<Worker2>(c =>
        {
            var worker = new Worker2();
            worker.SetCalculator(c.Resolve<PlusCalc>());
            return worker;
        });

        builder.Register<Worker2>(c =>
        {
            var worker = new Worker2();
            worker.SetCalculator(c.ResolveNamed<ICalculator>("state"));
            return worker;
        }).Named<Worker2>("state");

        builder.Register<Worker3>(c =>
        {
            var worker = new Worker3();
            worker.Calculator = c.Resolve<CatCalc>();
            return worker;
        });

        builder.Register<Worker3>(c =>
        {
            var worker = new Worker3();
            worker.Calculator = c.ResolveNamed<ICalculator>("state");
            return worker;
        }).Named<Worker3>("state");

        return builder.Build();
    }

    public static IContainer BuildDeclarativeContainer()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var builder = new ContainerBuilder();
        builder.RegisterModule(new ConfigurationModule(config.GetSection("autofac")));

        builder.RegisterType<TransactionContext>()
            .As<ITransactionContext>()
            .InstancePerMatchingLifetimeScope("transaction");
        
        builder.Register<Worker>(c => new Worker(c.Resolve<CatCalc>()));

        builder.Register<Worker2>(c =>
        {
            var worker = new Worker2();
            worker.SetCalculator(c.Resolve<PlusCalc>());
            return worker;
        });

        builder.Register<Worker3>(c =>
        {
            var worker = new Worker3();
            worker.Calculator = c.Resolve<CatCalc>();
            return worker;
        });
        
        builder.Register<Worker>(c =>
        {
            var stateCalc = c.Resolve<StateCalc>();
            return new Worker(stateCalc);
        }).Named<Worker>("state");

        builder.Register<Worker2>(c =>
        {
            var worker = new Worker2();
            worker.SetCalculator(c.Resolve<StateCalc>());
            return worker;
        }).Named<Worker2>("state");

        builder.Register<Worker3>(c =>
        {
            var worker = new Worker3();
            worker.Calculator = c.Resolve<StateCalc>();
            return worker;
        }).Named<Worker3>("state");

        return builder.Build();
    }

}

public interface IUnitOfWork
{
    Guid Id { get; }
}

public class UnitOfWork : IUnitOfWork
{
    public Guid Id { get; }

    public UnitOfWork()
    {
        Id = Guid.NewGuid();
    }
}

public interface ITransactionContext
{
    Guid TransactionId { get; }
}

public class TransactionContext : ITransactionContext
{
    public Guid TransactionId { get; }

    public TransactionContext()
    {
        TransactionId = Guid.NewGuid();
    }
}

public class StepOneService
{
    private readonly ITransactionContext _context;

    public StepOneService(ITransactionContext context)
    {
        _context = context;
    }

    public void Execute()
    {
        Console.WriteLine($"StepOneService - TransactionId: {_context.TransactionId}");
    }
}

public class StepTwoService
{
    private readonly ITransactionContext _context;

    public StepTwoService(ITransactionContext context)
    {
        _context = context;
    }

    public void Execute()
    {
        Console.WriteLine($"StepTwoService - TransactionId: {_context.TransactionId}");
    }
}

public class TransactionProcessor
{
    private readonly StepOneService _stepOne;
    private readonly StepTwoService _stepTwo;

    public TransactionProcessor(StepOneService stepOne, StepTwoService stepTwo)
    {
        _stepOne = stepOne;
        _stepTwo = stepTwo;
    }

    public void Process()
    {
        Console.WriteLine("=== Processing Transaction ===");
        _stepOne.Execute();
        _stepTwo.Execute();
    }
}

public interface ICalculator
{
    string Eval(string a, string b);
}

public class CatCalc : ICalculator
{
    public string Eval(string a, string b)
    {
        return a + b;
    }
}

public class PlusCalc : ICalculator
{
    public string Eval(string a, string b)
    {
        var int_a = int.Parse(a);
        var int_b = int.Parse(b);
        var sum = int_a + int_b;
        return sum.ToString();
    }
}

public class StateCalc : ICalculator
{
    private int _counter;

    public StateCalc(int counter)
    {
        _counter = counter;
    }

    public string Eval(string a, string b)
    {
        return a + b + (++_counter);
    }
}

public class Worker
{
    private ICalculator calc;

    public Worker(ICalculator calc)
    {
        this.calc = calc;
    }

    public void Work(string a, string b)
    {
        Console.WriteLine(calc.Eval(a, b));
    }
}

public class Worker2
{
    private ICalculator calc;

    public void SetCalculator(ICalculator calc)
    {
        this.calc = calc;
    }

    public void Work(string a, string b)
    {
        Console.WriteLine(calc.Eval(a, b));
    }
}

public class Worker3
{
    public ICalculator Calculator { get; set; }

    public void Work(string a, string b)
    {
        Console.WriteLine(Calculator.Eval(a, b));
    }
}
