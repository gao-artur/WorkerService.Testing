using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkerService.Testing.TestService
{
    public class QueueListener : IHostedService
    {
        private readonly ILogger<QueueListener> _logger;

        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        private readonly IBusService _busService;

        private readonly List<CancellationTokenSource> _busCancellationTokenSources;

        private readonly CancellationTokenSource _workerCancellationTokenSource;

        public QueueListener(ILogger<QueueListener> logger, IHostApplicationLifetime hostApplicationLifetime, IBusService busService)
        {
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
            _busService = busService;

            _busCancellationTokenSources = new List<CancellationTokenSource>();

            _workerCancellationTokenSource = new CancellationTokenSource();
            _workerCancellationTokenSource.Token.Register(CancelAllBusRegistrations);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _hostApplicationLifetime.ApplicationStarted.Register(RegisterHandlers);

            return Task.CompletedTask;
        }

        private void RegisterHandlers()
        {
            var ct1 = _busService.RegisterAsyncOperation<RequestMessage, ResponseMessage>("Plus", OnPlus);

            _busCancellationTokenSources.Add(ct1);

            var ct2 = _busService.RegisterAsyncOperation<RequestMessage, ResponseMessage>("Minus", OnMinus);

            _busCancellationTokenSources.Add(ct2);
        }

        private Task<ResponseMessage> OnPlus(RequestMessage request)
        {
            _logger.LogInformation($"OnPlus: {request.Left} + {request.Right}");

            var result = request.Left + request.Right;

            return Task.FromResult(new ResponseMessage { Result = result });
        }

        private Task<ResponseMessage> OnMinus(RequestMessage request)
        {
            _logger.LogInformation($"OnMinus: {request.Left} - {request.Right}");

            var result = request.Left - request.Right;

            return Task.FromResult(new ResponseMessage { Result = result });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _workerCancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        private void CancelAllBusRegistrations()
        {
            foreach (var cts in _busCancellationTokenSources)
            {
                cts.Cancel();
            }
        }
    }
}
