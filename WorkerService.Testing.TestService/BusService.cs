using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerService.Testing.TestService
{
    public interface IBusService
    {
        CancellationTokenSource RegisterAsyncOperation<TRequest, TResponse>(
            string operationName,
            Func<TRequest, Task<TResponse>> operationHandler);
    }

    public class BusService : IBusService
    {
        public CancellationTokenSource RegisterAsyncOperation<TRequest, TResponse>(string operationName, Func<TRequest, Task<TResponse>> operationHandler)
        {
            throw new NotImplementedException();
        }
    }
}