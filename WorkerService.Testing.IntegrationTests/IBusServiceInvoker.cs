using System.Threading.Tasks;

namespace WorkerService.Testing.IntegrationTests
{
    public interface IBusServiceInvoker<in TRequest, TResponse>
    {
        Task<TResponse> Invoke(TRequest request);
    }
}