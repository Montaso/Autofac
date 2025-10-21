using Autofac;
using Autofac.Core;

namespace Lab3_Test
{
    public class ContainerTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Worker_UsesCatCalc_ForDefaultInstance(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope = container.BeginLifetimeScope();
            using var sw = new StringWriter();
            Console.SetOut(sw);

            var worker = scope.Resolve<Worker>();
            worker.Work("Hello", "World");

            var output = sw.ToString();
            Assert.Contains("HelloWorld", output);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Worker2_UsesPlusCalc_ForDefaultInstance(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope = container.BeginLifetimeScope();
            using var sw = new StringWriter();
            Console.SetOut(sw);

            var worker2 = scope.Resolve<Worker2>();
            worker2.Work("10", "20");

            var output = sw.ToString();
            Assert.Contains("30", output);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Worker3_UsesCatCalc_ForDefaultInstance(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope = container.BeginLifetimeScope();
            using var sw = new StringWriter();
            Console.SetOut(sw);

            var worker3 = scope.Resolve<Worker3>();
            worker3.Work("Foo", "Bar");

            var output = sw.ToString();
            Assert.Contains("FooBar", output);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StateCalc_IsSingleton(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope1 = container.BeginLifetimeScope();
            using var scope2 = container.BeginLifetimeScope();

            var calc1 = scope1.ResolveNamed<ICalculator>("state");
            var calc2 = scope2.ResolveNamed<ICalculator>("state");

            Assert.Same(calc1, calc2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Worker_UsesStateCalc_ForNamedInstance(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope = container.BeginLifetimeScope();
            using var sw = new StringWriter();
            Console.SetOut(sw);

            var worker = scope.ResolveNamed<Worker>("state");
            worker.Work("Test", "State");

            var output = sw.ToString();
            Assert.Contains("TestState1", output);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Worker2_UsesStateCalc_ForNamedInstance(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope = container.BeginLifetimeScope();
            using var sw = new StringWriter();
            Console.SetOut(sw);

            var worker2 = scope.ResolveNamed<Worker2>("state");
            worker2.Work("A", "B");

            var output = sw.ToString();
            Assert.Contains("AB1", output);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Worker3_UsesStateCalc_ForNamedInstance(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope = container.BeginLifetimeScope();
            using var sw = new StringWriter();
            Console.SetOut(sw);

            var worker3 = scope.ResolveNamed<Worker3>("state");
            worker3.Work("X", "Y");

            var output = sw.ToString();
            Assert.Contains("XY1", output);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UnitOfWork_ReturnsSameInstance_WithinSameScope(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope = container.BeginLifetimeScope();

            var uow1 = scope.Resolve<IUnitOfWork>();
            var uow2 = scope.Resolve<IUnitOfWork>();

            Assert.Same(uow1, uow2);
            Assert.Equal(uow1.Id, uow2.Id);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UnitOfWork_ReturnsDifferentInstances_InDifferentScopes(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            IUnitOfWork uow1, uow2;

            using (var scope1 = container.BeginLifetimeScope())
            {
                uow1 = scope1.Resolve<IUnitOfWork>();
            }

            using (var scope2 = container.BeginLifetimeScope())
            {
                uow2 = scope2.Resolve<IUnitOfWork>();
            }

            Assert.NotSame(uow1, uow2);
            Assert.NotEqual(uow1.Id, uow2.Id);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TransactionContext_ReturnsSameInstance_WithinTaggedScope(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope = container.BeginLifetimeScope("transaction");

            var stepOne = scope.Resolve<StepOneService>();
            var stepTwo = scope.Resolve<StepTwoService>();

            var contextFieldStepOne = typeof(StepOneService).GetField("_context",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var contextFieldStepTwo = typeof(StepTwoService).GetField("_context",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var contextOne = (ITransactionContext)contextFieldStepOne.GetValue(stepOne);
            var contextTwo = (ITransactionContext)contextFieldStepTwo.GetValue(stepTwo);

            Assert.Same(contextOne, contextTwo);
            Assert.Equal(contextOne.TransactionId, contextTwo.TransactionId);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TransactionContext_ReturnsDifferentInstances_InDifferentTaggedScopes(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            ITransactionContext context1, context2;

            using (var scope1 = container.BeginLifetimeScope("transaction"))
            {
                context1 = scope1.Resolve<ITransactionContext>();
            }

            using (var scope2 = container.BeginLifetimeScope("transaction"))
            {
                context2 = scope2.Resolve<ITransactionContext>();
            }

            Assert.NotSame(context1, context2);
            Assert.NotEqual(context1.TransactionId, context2.TransactionId);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TransactionContext_ThrowsException_WhenResolvedOutsideTaggedScope(bool useImperative)
        {
            var container = useImperative ? Program.BuildImperativeContainer() : Program.BuildDeclarativeContainer();

            using var scope = container.BeginLifetimeScope();

            Assert.Throws<DependencyResolutionException>(() => 
                scope.Resolve<ITransactionContext>());
        }
    }
}
