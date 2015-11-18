using Bah.Core.Site.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            s.AddTenantOptions();
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
