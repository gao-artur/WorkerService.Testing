using System;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace WorkerService.Testing
{
    internal class HostFactoryResolver
    {
        private const string CreateHostBuilder = "CreateHostBuilder";

        public static Func<string[], IHostBuilder> ResolveHostBuilderFactory(Assembly assembly)
        {
            return ResolveFactory<IHostBuilder>(assembly, CreateHostBuilder);
        }

        private static Func<string[], T> ResolveFactory<T>(Assembly assembly, string name)
        {
            var programType = assembly.EntryPoint.DeclaringType;

            var factory = programType.GetTypeInfo().GetDeclaredMethod(name);
            if (!IsFactory<T>(factory))
            {
                throw new Exception($"Method '{name}' was not found in class '{programType?.FullName}' (Assembly: {assembly.FullName})");
            }

            return args => (T)factory.Invoke(null, new object[] { args });
        }

        private static bool IsFactory<TReturn>(MethodInfo factory)
        {
            return factory != null
                   && typeof(TReturn).IsAssignableFrom(factory.ReturnType)
                   && factory.GetParameters().Length == 1
                   && typeof(string[]) == factory.GetParameters()[0].ParameterType;
        }
    }
}