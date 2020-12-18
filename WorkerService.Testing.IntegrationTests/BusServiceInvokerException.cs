using System;

namespace WorkerService.Testing.IntegrationTests
{
    public class BusServiceInvokerException : Exception
    {
        public BusServiceInvokerException(string message) : base(message)
        {

        }
    }
}