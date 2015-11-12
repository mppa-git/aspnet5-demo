using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Shell.Middlewares;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shell.Middlewares
{
    public interface ITenantProvider<TTenant>
    {
        Task<Tuple<TTenant, bool>> TryGetTenant(HttpContext context);
    }

    public interface ITenantService
    {
        object Tenant { get; }
    }

    public interface ITenantService<out TTenant> : ITenantService
    {
        new TTenant Tenant { get; }
    }

    abstract class TenantService
    {
        internal abstract Task<bool> SetTenant(HttpContext context);
    }

    class TenantService<TTenant> : TenantService, ITenantService<TTenant>
    {
        readonly ITenantProvider<TTenant>[] _providers;

        public TTenant Tenant { get; private set; }

        object ITenantService.Tenant => Tenant;

        public TenantService(IEnumerable<ITenantProvider<TTenant>> providers)
        {
            if (providers == null)
                throw new ArgumentNullException(nameof(providers));

            _providers = providers.ToArray();

            if (_providers.Length == 0)
            {
                throw new InvalidOperationException("No tenant providers added");
            }
        }

        internal override async Task<bool> SetTenant(HttpContext context)
        {
            foreach (var provider in _providers)
            {
                var result = await provider.TryGetTenant(context);
                if (result == null) // used as a signal from a provider to stop processing. This is basically an error, and request processing should stop
                    return false;

                if (result.Item2)
                {
                    Tenant = result.Item1;
                }
            }

            return true;
        }
    }
}

namespace Microsoft.Framework.DependencyInjection
{
    public static class TenantServiceExtensions
    {
        public static ServiceWrapper<TTenant> AddMultiTenant<TTenant>(this IServiceCollection collection)
            where TTenant : class
        {
            collection.AddScoped<ITenantService<TTenant>, TenantService<TTenant>>()
                .AddScoped(services => (ITenantService)services.GetService(typeof(ITenantService<TTenant>)))
                .AddScoped(services => (TenantService)services.GetService(typeof(ITenantService<TTenant>)));

            return new ServiceWrapper<TTenant>(collection);
        }

        /*
        public static ServiceWrapper<TTenant> AddSubdomainProvider<TTenant>(this ServiceWrapper<TTenant> collection, Action<SubdomainTenantProviderOptions> configureOptions = null)
            where TTenant : class
        {
            var s = collection.ServiceCollection;
            s.AddOptions();
            s.AddSingleton<ITenantProvider<TTenant>, SubdomainTenantProvider<TTenant>>();

            if (configureOptions != null)
                s.Configure(configureOptions);

            return collection;
        }*/

        public static ServiceWrapper<TTenant> AddRouteProvider<TTenant>(this ServiceWrapper<TTenant> collection, Action<RouteTenantProviderOptions> configureOptions = null)
    where TTenant : class
        {
            var s = collection.ServiceCollection;
            s.AddOptions();
            s.AddSingleton<ITenantProvider<TTenant>, RouteTenantProvider<TTenant>>();

            if (configureOptions != null)
                s.Configure(configureOptions);

            return collection;
        }
    }
}


namespace Shell.Middlewares
{
    public interface INamedTenantLookup<TTenant>
    {
        Task<TTenant> Lookup(string name);
    }

    public struct ServiceWrapper<TTenant>
        where TTenant : class
    {
        public ServiceWrapper(IServiceCollection collection)
        {
            ServiceCollection = collection;
        }

        internal IServiceCollection ServiceCollection { get; }
    }
}
