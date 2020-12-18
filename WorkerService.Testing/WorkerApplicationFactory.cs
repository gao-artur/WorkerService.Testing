// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;

namespace WorkerService.Testing
{
    public class WorkerApplicationFactory<TEntryPoint> : IDisposable where TEntryPoint : class
    {
        private bool _disposed;
        private IHost _host;
        private Action<IHostBuilder> _configuration;
        private readonly List<WorkerApplicationFactory<TEntryPoint>> _derivedFactories = new List<WorkerApplicationFactory<TEntryPoint>>();

        public WorkerApplicationFactory()
        {
            _configuration = ConfigureHost;
        }

        ~WorkerApplicationFactory()
        {
            Dispose(false);
        }

        public virtual IServiceProvider Services
        {
            get
            {
                EnsureStarted();
                return _host.Services;
            }
        }

        public IReadOnlyList<WorkerApplicationFactory<TEntryPoint>> Factories => _derivedFactories.AsReadOnly();

        public WorkerApplicationFactory<TEntryPoint> WithHostBuilder(Action<IHostBuilder> configuration) =>
            WithHostBuilderCore(configuration);

        public async Task StartAsync()
        {
            EnsureDepsFile();

            var hostBuilder = CreateHostBuilder();
            SetContentRoot(hostBuilder);
            _configuration(hostBuilder);
            _host = CreateHost(hostBuilder);

            await _host.StartAsync();
        }

        public Task StopAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }

        internal virtual WorkerApplicationFactory<TEntryPoint> WithHostBuilderCore(Action<IHostBuilder> configuration)
        {
            var factory = new DelegatedWorkerApplicationFactory(
                CreateHost,
                CreateHostBuilder,
                GetTestAssemblies,
                builder =>
                {
                    _configuration(builder);
                    configuration(builder);
                });

            _derivedFactories.Add(factory);

            return factory;
        }

        private void EnsureStarted()
        {
            if (_host == null)
            {
                throw new Exception("Worker service wasn't built. Call StartAsync() method");
            }
        }

        private void SetContentRoot(IHostBuilder builder)
        {
            var metadataAttributes = GetContentRootMetadataAttributes(
                typeof(TEntryPoint).Assembly.FullName,
                typeof(TEntryPoint).Assembly.GetName().Name);

            string contentRoot = null;
            for (var i = 0; i < metadataAttributes.Length; i++)
            {
                var contentRootAttribute = metadataAttributes[i];
                var contentRootCandidate = Path.Combine(
                    AppContext.BaseDirectory,
                    contentRootAttribute.ContentRootPath);

                var contentRootMarker = Path.Combine(
                    contentRootCandidate,
                    Path.GetFileName(contentRootAttribute.ContentRootTest));

                if (File.Exists(contentRootMarker))
                {
                    contentRoot = contentRootCandidate;
                    break;
                }
            }

            if (contentRoot != null)
            {
                builder.UseContentRoot(contentRoot);
            }
            else
            {
                builder.UseSolutionRelativeContentRoot(typeof(TEntryPoint).Assembly.GetName().Name);
            }
        }

        private WorkerApplicationFactoryContentRootAttribute[] GetContentRootMetadataAttributes(
            string tEntryPointAssemblyFullName,
            string tEntryPointAssemblyName)
        {
            var testAssembly = GetTestAssemblies();
            var metadataAttributes = testAssembly
                .SelectMany(a => a.GetCustomAttributes<WorkerApplicationFactoryContentRootAttribute>())
                .Where(a => string.Equals(a.Key, tEntryPointAssemblyFullName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(a.Key, tEntryPointAssemblyName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Priority)
                .ToArray();

            return metadataAttributes;
        }

        protected virtual IEnumerable<Assembly> GetTestAssemblies()
        {
            try
            {
                var context = DependencyContext.Default;

                var runtimeProjectLibraries = context.RuntimeLibraries
                    .ToDictionary(r => r.Name, r => r, StringComparer.Ordinal);

                var entryPointAssemblyName = typeof(TEntryPoint).Assembly.GetName().Name;

                // Find the list of projects referencing TEntryPoint.
                var candidates = context.CompileLibraries
                    .Where(library => library.Dependencies.Any(d => string.Equals(d.Name, entryPointAssemblyName, StringComparison.Ordinal)));

                var testAssemblies = new List<Assembly>();
                foreach (var candidate in candidates)
                {
                    if (runtimeProjectLibraries.TryGetValue(candidate.Name, out var runtimeLibrary))
                    {
                        var runtimeAssemblies = runtimeLibrary.GetDefaultAssemblyNames(context);
                        testAssemblies.AddRange(runtimeAssemblies.Select(Assembly.Load));
                    }
                }

                return testAssemblies;
            }
            catch (Exception)
            {
                // ignored
            }

            return Array.Empty<Assembly>();
        }

        private void EnsureDepsFile()
        {
            if (typeof(TEntryPoint).Assembly.EntryPoint == null)
            {
                throw new InvalidOperationException(
                    $"The provided Type '{typeof(TEntryPoint).Name}' does not belong "
                    + "to an assembly with an entry point. A common cause for this error "
                    + "is providing a Type from a class library.");
            }

            var depsFileName = $"{typeof(TEntryPoint).Assembly.GetName().Name}.deps.json";
            var depsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, depsFileName));
            if (!depsFile.Exists)
            {
                throw new InvalidOperationException(
                    $"Can't find '{depsFile.FullName}'. A common causes for this error are:{Environment.NewLine}" +
                    $"1. Having shadow copying enabled when the tests run. Disable shadow copying.{Environment.NewLine}" +
                    "2. Using nuget version < 5.0. Check the nuget version 'dotnet nuget --version'.");
            }
        }

        protected virtual IHostBuilder CreateHostBuilder()
        {
            var hostBuilder = HostFactoryResolver.ResolveHostBuilderFactory(typeof(TEntryPoint).Assembly)?.Invoke(Array.Empty<string>());
            hostBuilder.UseEnvironment(Environments.Development);
            return hostBuilder;
        }

        protected virtual IHost CreateHost(IHostBuilder builder)
        {
            return builder.Build();
        }

        /// <summary>
        /// Gives a fixture an opportunity to configure the application before it gets built.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> for the application.</param>
        protected virtual void ConfigureHost(IHostBuilder builder)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources;
        /// <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var factory in _derivedFactories)
                {
                    factory.Dispose();
                }

                _host?.Dispose();
            }

            _disposed = true;
        }

        private class DelegatedWorkerApplicationFactory : WorkerApplicationFactory<TEntryPoint>
        {
            private readonly Func<IHostBuilder, IHost> _createHost;
            private readonly Func<IHostBuilder> _createHostBuilder;
            private readonly Func<IEnumerable<Assembly>> _getTestAssemblies;

            public DelegatedWorkerApplicationFactory(
                Func<IHostBuilder, IHost> createHost,
                Func<IHostBuilder> createHostBuilder,
                Func<IEnumerable<Assembly>> getTestAssemblies,
                Action<IHostBuilder> configureWebHost)
            {
                _createHost = createHost;
                _createHostBuilder = createHostBuilder;
                _getTestAssemblies = getTestAssemblies;
                _configuration = configureWebHost;
            }

            protected override IHost CreateHost(IHostBuilder builder) => _createHost(builder);

            protected override IHostBuilder CreateHostBuilder() => _createHostBuilder();

            protected override IEnumerable<Assembly> GetTestAssemblies() => _getTestAssemblies();

            protected override void ConfigureHost(IHostBuilder builder) => _configuration(builder);

            internal override WorkerApplicationFactory<TEntryPoint> WithHostBuilderCore(Action<IHostBuilder> configuration)
            {
                return new DelegatedWorkerApplicationFactory(
                    _createHost,
                    _createHostBuilder,
                    _getTestAssemblies,
                    builder =>
                    {
                        _configuration(builder);
                        configuration(builder);
                    });
            }
        }
    }
}
