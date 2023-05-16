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
            var service = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>("service1", service);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Same(service, provider.GetKeyedService<IService>("service1"));
        }

        interface IService { }

        class Service : IService { }
    }
}
