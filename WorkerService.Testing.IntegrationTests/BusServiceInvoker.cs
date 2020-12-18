using System;
using System.Threading.Tasks;

namespace WorkerService.Testing.IntegrationTests
{
    public class BusServiceInvoker<TRequest, TResponse> : IBusServiceInvoker<TRequest, TResponse>
    {
        private readonly string _operationName;

        public BusServiceInvoker(string operationName)
        {
            _operationName = operationName;
        }

        public virtual Func<TRequest, Task<TResponse>> Handler { private get; set; }

        public virtual Task<TResponse> Invoke(TRequest request)
        {
            if (Handler == null)
            {
                throw new BusServiceInvokerException(AssertionErrorMessage);
            }

            return Handler(request);
        }

        protected virtual string AssertionErrorMessage =>
            $"No handler was registered for operation name {_operationName}, "
            + $"request type {typeof(TRequest).Name}, response type {typeof(TResponse).Name}";
    }
}