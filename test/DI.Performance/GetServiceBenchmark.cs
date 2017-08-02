﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.DependencyInjection.Performance
{
    [Config(typeof(CoreConfig))]
    public class GetServiceBenchmark
    {
        private const int OperationsPerInvoke = 50000;

        private IServiceProvider _transientSp;
        private IServiceScope _scopedSp;
        private IServiceProvider _singletonSp;

        [Params(ServiceProviderMode.Compiled, ServiceProviderMode.Mixed, ServiceProviderMode.Runtime)]
        public ServiceProviderMode Mode { get; set; }

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoDI()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = new A(new B(new C()));
                temp.Foo();
            }
        }

        [GlobalSetup(Target = nameof(Transient))]
        public void SetupTransient()
        {
            var services = new ServiceCollection();
            services.AddTransient<A>();
            services.AddTransient<B>();
            services.AddTransient<C>();
            _transientSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = Mode
            });
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Transient()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = _transientSp.GetService<A>();
                temp.Foo();
            }
        }

        [GlobalSetup(Target = nameof(Scoped))]
        public void SetupScoped()
        {
            var services = new ServiceCollection();
            services.AddScoped<A>();
            services.AddScoped<B>();
            services.AddScoped<C>();
            _scopedSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = Mode
            }).CreateScope();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Scoped()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = _scopedSp.ServiceProvider.GetService<A>();
                temp.Foo();
            }
        }

        [GlobalSetup(Target = nameof(Singleton))]
        public void SetupScopedSingleton()
        {
            var services = new ServiceCollection();
            services.AddSingleton<A>();
            services.AddSingleton<B>();
            services.AddSingleton<C>();
            _singletonSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = Mode
            });
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Singleton()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = _singletonSp.GetService<A>();
                temp.Foo();
            }
        }

        private class A
        {
            public A(B b)
            {

            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class B
        {
            public B(C c)
            {

            }
        }

        private class C
        {

        }
    }
}