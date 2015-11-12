using Bah.Core.Site.Multitenancy;
using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bah.Core.Site.Multitenancy
{
    public interface ITenantProvider<TTenant>
        where TTenant : ITenant
    {
        Task<Tuple<TTenant, bool>> TryGetTenant(HttpContext context);
    }

    public interface ITenantService
    {
        ITenant Tenant { get; }
    }

    public interface ITenantService<out TTenant> : ITenantService
        where TTenant : ITenant
    {
        new TTenant Tenant { get; }
    }

    abstract class TenantService
    {
        internal abstract Task<bool> SetTenant(HttpContext context);
    }

    class TenantService<TTenant> : TenantService, ITenantService<TTenant>
        where TTenant : ITenant
    {
        readonly ITenantProvider<TTenant>[] _providers;

        public TTenant Tenant { get; private set; }

        ITenant ITenantService.Tenant => Tenant;

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
        /// <summary>
        /// Register ITenantService into IoC. 
        /// </summary>
        /// <typeparam name="TTenant"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static ServiceWrapper<TTenant> AddMultitenancy<TTenant>(this IServiceCollection collection)
            where TTenant : ITenant
        {
            collection.AddScoped<ITenantService<TTenant>, TenantService<TTenant>>()
                .AddScoped(services => (ITenantService<ITenant>)services.GetService(typeof(ITenantService<TTenant>)))
                .AddScoped(services => (ITenantService)services.GetService(typeof(ITenantService<TTenant>)))
                .AddScoped(services => (TenantService)services.GetService(typeof(ITenantService<TTenant>)));

            return new ServiceWrapper<TTenant>(collection);
        }

        /// <summary>
        /// Setup multitenancy to be selected via route: [tenant]/path
        /// </summary>
        /// <typeparam name="TTenant"></typeparam>
        /// <param name="collection"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static ServiceWrapper<TTenant> AddRouteProvider<TTenant>(this ServiceWrapper<TTenant> collection, Action<RouteTenantProviderOptions> configureOptions = null)
    where TTenant : class, ITenant
        {
            var s = collection.ServiceCollection;
            s.AddOptions();
            s.AddSingleton<ITenantProvider<TTenant>, RouteTenantProvider<TTenant>>();

            if (configureOptions != null)
                s.Configure(configureOptions);

            return collection;
        }
    }

    public interface INamedTenantLookup<TTenant>
    {
        Task<TTenant> Lookup(string name);
    }

    public struct ServiceWrapper<TTenant>
        where TTenant : ITenant
    {
        public ServiceWrapper(IServiceCollection collection)
        {
            ServiceCollection = collection;
        }

        internal IServiceCollection ServiceCollection { get; }
    }
}



