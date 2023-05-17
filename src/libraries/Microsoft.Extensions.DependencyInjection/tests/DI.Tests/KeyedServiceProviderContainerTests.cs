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

        interface IService { }

        class Service : IService
        {
            private readonly Guid _id = Guid.NewGuid();

            public override string? ToString() => _id.ToString();
        }
    }
}
