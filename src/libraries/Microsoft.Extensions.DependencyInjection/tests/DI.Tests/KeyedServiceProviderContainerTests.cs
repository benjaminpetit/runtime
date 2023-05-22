// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection.Specification;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class KeyedServiceProviderContainerTests : DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) => collection.BuildServiceProvider();

        [Fact]
        public void ResolveKeyedService()
        {
            var service1 = new Service();
            var service2 = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>("service1", service1);
            serviceCollection.AddKeyedSingleton<IService>("service2", service2);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Same(service1, provider.GetKeyedService<IService>("service1"));
            Assert.Same(service2, provider.GetKeyedService<IService>("service2"));
        }

        [Fact]
        public void ResolveKeyedServiceSingletonInstance()
        {
            var service = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>("service1", service);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Same(service, provider.GetKeyedService<IService>("service1"));
        }

        [Fact]
        public void ResolveKeyedServiceSingletonInstanceWithKeyInjection()
        {
            var serviceKey = "this-is-my-service";
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService, Service>(serviceKey);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            var svc = provider.GetKeyedService<IService>(serviceKey);
            Assert.NotNull(svc);
            Assert.Equal(serviceKey, svc.ToString());
        }

        [Fact]
        public void ResolveKeyedServiceSingletonFactory()
        {
            var service = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>("service1", (sp, key) => service);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Same(service, provider.GetKeyedService<IService>("service1"));
        }

        [Fact]
        public void ResolveKeyedServiceSingletonType()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService, Service>("service1");

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Equal(typeof(Service), provider.GetKeyedService<IService>("service1")!.GetType());
        }

        [Fact]
        public void ResolveKeyedServiceTransientFactory()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<IService>("service1", (sp, key) => new Service(key as string));

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            var first = provider.GetKeyedService<IService>("service1");
            var second = provider.GetKeyedService<IService>("service1");
            Assert.NotSame(first, second);
            Assert.Equal("service1", first.ToString());
            Assert.Equal("service1", second.ToString());
        }

        [Fact]
        public void ResolveKeyedServiceTransientType()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<IService, Service>("service1");

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            var first = provider.GetKeyedService<IService>("service1");
            var second = provider.GetKeyedService<IService>("service1");
            Assert.NotSame(first, second);
        }

        interface IService { }

        class Service : IService
        {
            private readonly string _id;

            public Service() => _id = Guid.NewGuid().ToString();

            public Service([ServiceKey] string id) => _id = id;

            public override string? ToString() => _id;
        }
    }
}
