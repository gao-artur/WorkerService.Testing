using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WorkerService.Testing.TestService;

namespace WorkerService.Testing.IntegrationTests
{
    [TestClass]
    public class TestServiceTests
    {
        private static IBusServiceInvoker<RequestMessage, ResponseMessage> _plusOperationInvoker;

        private static IBusServiceInvoker<RequestMessage, ResponseMessage> _minusOperationInvoker;

        private static IBusServiceInvoker<RequestMessage, ResponseMessage> _notExistOperationInvoker;

        private static WorkerApplicationFactory<Program> _workerApplicationFactory;

        [ClassInitialize]
        public static async Task ClassInit(TestContext _)
        {
            var busServiceMock = new Mock<IBusService>();

            _plusOperationInvoker = busServiceMock.SetupOperationInvoker
                <RequestMessage, ResponseMessage>("Plus");

            _minusOperationInvoker = busServiceMock.SetupOperationInvoker
                <RequestMessage, ResponseMessage>("Minus");

            _notExistOperationInvoker = busServiceMock.SetupOperationInvoker
                <RequestMessage, ResponseMessage>("NotExist");

            _workerApplicationFactory = new WorkerApplicationFactory<Program>()
                    .WithHostBuilder(builder =>
                    {
                        builder.ConfigureServices((host, services) =>
                        {
                            services.Replace(ServiceDescriptor.Singleton(busServiceMock.Object));
                        });
                    });

            await _workerApplicationFactory.StartAsync();
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            await _workerApplicationFactory.StopAsync();
        }

        [TestMethod]
        public async Task PlusOperationTest_5()
        {
            var result = await _plusOperationInvoker.Invoke(new RequestMessage { Left = 2, Right = 3 });
            Assert.AreEqual(5, result.Result);
        }

        [TestMethod]
        public async Task MinusOperationTest_Minus1()
        {
            var result = await _minusOperationInvoker.Invoke(new RequestMessage { Left = 2, Right = 3 });
            Assert.AreEqual(-1, result.Result);
        }

        [TestMethod]
        public async Task NotExistOperationTest_BusServiceInvokerException()
        {
            await Assert.ThrowsExceptionAsync<BusServiceInvokerException>(
                () => _notExistOperationInvoker.Invoke(new RequestMessage { Left = 2, Right = 3 }));
        }
    }
}
