// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public class KeyedServiceProviderWrapperFactory<TContainerBuilder> : IServiceProviderFactory<TContainerBuilder> where TContainerBuilder : notnull
    {
        private readonly IServiceProviderFactory<TContainerBuilder> _serviceProviderFactory;

        public KeyedServiceProviderWrapperFactory(IServiceProviderFactory<TContainerBuilder> serviceProviderFactory) => _serviceProviderFactory = serviceProviderFactory;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:MakeGenericType", Justification = "TODO BPETIT")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode", Justification = "TODO BPETIT")]
        TContainerBuilder IServiceProviderFactory<TContainerBuilder>.CreateBuilder(IServiceCollection services)
        {
            // Remove all registered key service descriptors from the collection
            var descriptors = services.Where(d => d.ServiceKey != null).ToList();

            var newServices = new ServiceCollection();
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceKey == null)
                {
                    // use it as is
                    newServices.Add(descriptor);
                }
                else
                {
                    // wrap it
                    var wrappedType = typeof(KeyedService<>).MakeGenericType(descriptor.ServiceType);
                    IKeyedService keyedService;
                    if (descriptor.ImplementationInstance != null)
                    {
                        keyedService = (IKeyedService)Activator.CreateInstance(
                            typeof(InstanceKeyedService<>).MakeGenericType(descriptor.ServiceType),
                            descriptor.ServiceKey,
                            descriptor.ImplementationInstance)!;
                    }
                    else if (descriptor.ImplementationFactory != null)
                    {
                        keyedService = (IKeyedService)Activator.CreateInstance(
                            typeof(FactoryKeyedService<>).MakeGenericType(descriptor.ServiceType),
                            descriptor.ServiceKey,
                            descriptor.ImplementationFactory)!;
                    }
                    else if (descriptor.ImplementationType != null)
                    {
                        keyedService = (IKeyedService)Activator.CreateInstance(
                            typeof(TypeKeyedService<>).MakeGenericType(descriptor.ServiceType),
                            descriptor.ServiceKey,
                            descriptor.ImplementationType)!;
                    }
                    else
                    {
                        // TODO BPETIT
                        throw new ArgumentException("Incompatible service descriptor");
                    }
                    // TODO BPETIT transient? scoped?
                    var wrappedDescriptor = new ServiceDescriptor(wrappedType, sp => keyedService, descriptor.Lifetime);
                    newServices.Add(wrappedDescriptor);
                }
            }

            return _serviceProviderFactory.CreateBuilder(newServices);
        }

        public IServiceProvider CreateServiceProvider(TContainerBuilder containerBuilder) => _serviceProviderFactory.CreateServiceProvider(containerBuilder);
    }

    public class KeyedServiceProviderWrapper : IServiceProvider, ISupportKeyedService
    {
        private readonly IServiceProvider _serviceProvider;

        public KeyedServiceProviderWrapper(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public object? GetService(Type serviceType) => (object?) _serviceProvider.GetService(serviceType);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:MakeGenericType", Justification = "TODO BPETIT")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode", Justification = "TODO BPETIT")]
        public object GetKeyedService(Type serviceType, object serviceKey)
        {
            var wrappedType = typeof(KeyedService<>).MakeGenericType(serviceType);
            //var services = _serviceProvider.GetServices(wrappedType);
            IEnumerable<object?> services = _serviceProvider.GetServices(wrappedType);
            var entry = services.FirstOrDefault(s => s != null && ((IKeyedService) s).ServiceKey == serviceKey);
            if (entry == null)
            {
                throw new ArgumentException("TODO BPETIT");
            }
            return entry;
        }
    }

    internal interface IKeyedService
    {
        public object ServiceKey { get; }

        public abstract object GetService();
    }

    internal abstract class KeyedService<T> : IKeyedService
    {
        public object ServiceKey { get; }

        protected KeyedService(object serviceKey) => ServiceKey = serviceKey;

        public abstract object GetService();
    }

    internal class FactoryKeyedService<T> : KeyedService<T>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Func<IServiceProvider, object> _factory;

        public FactoryKeyedService(string serviceKey, IServiceProvider serviceProvider, Func<IServiceProvider, object> factory)
            : base(serviceKey)
        {
            _serviceProvider = serviceProvider;
            _factory = factory;
        }

        public override object GetService() => _factory(_serviceProvider);
    }

    internal class InstanceKeyedService<T> : KeyedService<T>
    {
        private readonly object _instance;

        public InstanceKeyedService(string serviceKey, object instance)
            : base(serviceKey)
        {
            _instance = instance;
        }

        public override object GetService() => _instance;
    }

    internal class TypeKeyedService<T> : KeyedService<T>
    {
        private readonly IServiceProvider _serviceProvider;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private readonly Type _implementationType;

        public TypeKeyedService(
            string serviceKey,
            IServiceProvider serviceProvider,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
            : base(serviceKey)
        {
            _serviceProvider = serviceProvider;
            _implementationType = implementationType;
        }

        public override object GetService() => ActivatorUtilities.CreateInstance(_serviceProvider, _implementationType);
    }
}
