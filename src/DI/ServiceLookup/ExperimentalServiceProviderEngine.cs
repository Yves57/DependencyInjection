// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ExperimentalServiceProviderEngine : DynamicServiceProviderEngine
    {
        private readonly Dictionary<Type, Func<ServiceProviderEngineScope, object>> _realized2 = new Dictionary<Type, Func<ServiceProviderEngineScope, object>>(50);

        private readonly ReadOnlyDictionary _realized3;

        public ExperimentalServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors, IServiceProviderEngineCallback callback) : base(serviceDescriptors, callback)
        {
            ResolveSingletonServices(serviceDescriptors);
            RealizeAllClosedServices(serviceDescriptors);
            _realized3 = new ReadOnlyDictionary(_realized2);
        }

        protected override Func<ServiceProviderEngineScope, object> GetOrCreateRealizedService(Type serviceType)
        {
            if (_realized3.TryGetValue(serviceType, out var result))
            {
                return result;
            }
            return base.GetOrCreateRealizedService(serviceType); // Open generics realization case...
        }

        protected override Func<ServiceProviderEngineScope, object> RealizeService(IServiceCallSite callSite)
        {
            if (_realized3 == null)
            {
                var realizedService = ExpressionResolverBuilder.Build(callSite);
                _realized2[callSite.ServiceType] = realizedService;
                return realizedService;
            }
            else
            {
                return base.RealizeService(callSite);
            }
        }

        private void ResolveSingletonServices(IEnumerable<ServiceDescriptor> serviceDescriptors)
        {
            var callSite = CallSiteFactory.CreateCallSite(typeof(IServiceProvider), new CallSiteChain());
            if (callSite != null)
            {
                RuntimeResolver.Resolve(callSite, Root);
            }

            callSite = CallSiteFactory.CreateCallSite(typeof(IServiceScopeFactory), new CallSiteChain());
            if (callSite != null)
            {
                RuntimeResolver.Resolve(callSite, Root);
            }

            foreach (var descriptor in serviceDescriptors.Where(sd => sd.Lifetime == ServiceLifetime.Singleton && !sd.ServiceType.IsGenericTypeDefinition))
            {
                callSite = CallSiteFactory.CreateCallSite(descriptor.ServiceType, new CallSiteChain());
                if (callSite != null)
                {
                    RuntimeResolver.Resolve(callSite, Root);
                }
            }
        }

        private void RealizeAllClosedServices(IEnumerable<ServiceDescriptor> serviceDescriptors)
        {
            foreach (var descriptor in serviceDescriptors.Where(sd => !sd.ServiceType.IsGenericTypeDefinition))
            {
                CreateServiceAccessor(descriptor.ServiceType);
            }
            CreateServiceAccessor(typeof(IServiceProvider));
            CreateServiceAccessor(typeof(IServiceScopeFactory));
        }
    }

    internal class ReadOnlyDictionary
    {
        class Entry
        {
            public int HashCode;
            public Type Key;
            public Func<ServiceProviderEngineScope, object> Value;
            public Entry Next;
        }

        private readonly Entry[] _buckets;

        private readonly int _bucketCount = 101; // Get best prime...

        public ReadOnlyDictionary(IEnumerable<KeyValuePair<Type, Func<ServiceProviderEngineScope, object>>> entries)
        {
            _buckets = new Entry[_bucketCount];
            foreach (var entry in entries)
            {
                var hashCode = entry.Key.GetHashCode();
                var bucketIndex = hashCode % _bucketCount;
                if (_buckets[bucketIndex] == null)
                {
                    _buckets[bucketIndex] = new Entry()
                    {
                        HashCode = hashCode,
                        Key = entry.Key,
                        Value = entry.Value,
                        Next = null
                    };
                }
                else
                {
                    var oldFirstEntry = _buckets[bucketIndex];

                    _buckets[bucketIndex] = new Entry()
                    {
                        HashCode = hashCode,
                        Key = entry.Key,
                        Value = entry.Value,
                        Next = oldFirstEntry
                    };
                }
            }
        }

        public bool TryGetValue(Type type, out Func<ServiceProviderEngineScope, object> func)
        {
            var hashCode = type.GetHashCode();
            var bucketIndex = hashCode % _bucketCount;

            var entry = _buckets[bucketIndex];
            while (entry != null)
            {
                if (entry.HashCode == hashCode && entry.Key == type)
                {
                    func = entry.Value;
                    return true;
                }
                entry = entry.Next;
            }

            func = null;
            return false;
        }
    }
}