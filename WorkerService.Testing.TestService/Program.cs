using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WorkerService.Testing.TestService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            throw new InvalidOperationException("This is a SUT project. It's not intended to run by self.");
#pragma warning disable 162
            CreateHostBuilder(args).Build().Run();
#pragma warning restore 162
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IBusService, BusService>();
                    services.AddHostedService<QueueListener>();
                });
    }
}
