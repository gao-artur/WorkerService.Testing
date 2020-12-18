using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace WorkerService.Testing
{
    internal static class HostBuilderExtensions
    {
        public static IHostBuilder UseSolutionRelativeContentRoot(
            this IHostBuilder builder,
            string solutionRelativePath,
            string solutionName = "*.sln")
        {
            return builder.UseSolutionRelativeContentRoot(solutionRelativePath, AppContext.BaseDirectory, solutionName);
        }

        private static IHostBuilder UseSolutionRelativeContentRoot(
            this IHostBuilder builder,
            string solutionRelativePath,
            string applicationBasePath,
            string solutionName)
        {
            if (solutionRelativePath == null)
            {
                throw new ArgumentNullException(nameof(solutionRelativePath));
            }

            if (applicationBasePath == null)
            {
                throw new ArgumentNullException(nameof(applicationBasePath));
            }

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionPath = Directory.EnumerateFiles(directoryInfo.FullName, solutionName).FirstOrDefault();
                if (solutionPath != null)
                {
                    builder.UseContentRoot(Path.GetFullPath(Path.Combine(directoryInfo.FullName, solutionRelativePath)));
                    return builder;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo?.Parent != null);

            throw new InvalidOperationException($"Solution root could not be located using application root {applicationBasePath}.");
        }
    }
}