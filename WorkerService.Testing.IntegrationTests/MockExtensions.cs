using System;
using System.Threading.Tasks;
using Moq;
using WorkerService.Testing.TestService;

namespace WorkerService.Testing.IntegrationTests
{
    public static class MockExtensions
    {
        public static IBusServiceInvoker<TRequest, TResponse> SetupOperationInvoker<TRequest, TResponse>(this Mock<IBusService> mock, string operationName)
        {
            var invoker = new BusServiceInvoker<TRequest, TResponse>(operationName);
            mock.Setup(busService => busService.RegisterAsyncOperation(
                        operationName,
                        It.IsAny<Func<TRequest, Task<TResponse>>>()))
                .Callback(
                    (
                        string _,
                        Func<TRequest, Task<TResponse>> operationHandler) =>
                    {
                        invoker.Handler = operationHandler;
                    });

            return invoker;
        }
    }
}