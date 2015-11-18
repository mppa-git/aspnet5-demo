using Bah.Core.Site.Configuration;
using Bah.Core.Site.Multitenancy;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TenantOptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddTenantOptions(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAdd(ServiceDescriptor.Singleton(typeof(ITenantOptions<>), typeof(TenantOptionsManager<>)));
            return services;
        }

        private static bool IsAction(Type type)
        {
            return (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Action<>));
        }

        private static IEnumerable<Type> FindIConfigureOptions(Type type)
        {
            var serviceTypes = type.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(ITenantConfigureOptions<>));
            if (!serviceTypes.Any())
            {
                string error = "TODO: No ITenantConfigureOptions<> found.";
                if (IsAction(type))
                {
                    error += " did you mean Configure(Action<T>)";
                }
                throw new InvalidOperationException(error);
            }
            return serviceTypes;
        }

        public static IServiceCollection ConfigureTenantOptions(this IServiceCollection services, Type configureType)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var serviceTypes = FindIConfigureOptions(configureType);
            foreach (var serviceType in serviceTypes)
            {
                services.AddTransient(serviceType, configureType);
            }
            return services;
        }

        public static IServiceCollection ConfigureTenantOptions<TSetup>(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.ConfigureTenantOptions(typeof(TSetup));
        }

        /*
        public static IServiceCollection ConfigureTenantOptions(this IServiceCollection services, object configureInstance)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureInstance == null)
            {
                throw new ArgumentNullException(nameof(configureInstance));
            }

            var serviceTypes = FindIConfigureOptions(configureInstance.GetType());
            foreach (var serviceType in serviceTypes)
            {
                services.AddInstance(serviceType, configureInstance);
            }
            return services;
        }
        */

        /*
    public static IServiceCollection TenantConfigure<TOptions>(
        this IServiceCollection services,
        Action<TOptions> setupAction)
        where TOptions : class
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (setupAction == null)
        {
            throw new ArgumentNullException(nameof(setupAction));
        }

        services.ConfigureTenantOptions(new TenantConfigureOptions<TOptions>(setupAction));
        return services;
    }*/

        /// <summary>
        /// Configure an options section in a per-tenant way.  After registering via this method, 
        /// you can retrieve the options for your tenant via IoC like so:
        /// <code>serviceProvider.GetService<ITenantOptions<TOptions>>();</code>
        /// </summary>
        public static IServiceCollection TenantConfigure<TOptions>(
            this IServiceCollection services,
            Func<Framework.Configuration.IConfigurationRoot, Framework.Configuration.IConfiguration> getConfigFunc)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (getConfigFunc == null)
            {
                throw new ArgumentNullException(nameof(getConfigFunc));
            }

            services.AddScoped<ITenantConfigureOptions<TOptions>>((s) =>
            {
                var tenantService = s.GetRequiredService<ITenantService<ITenant>>();
                if (tenantService.Tenant == null)
                    throw new ArgumentNullException(nameof(tenantService.Tenant));

                var tenantName = tenantService.Tenant.Name;

                var configStore = s.GetRequiredService<ConfigurationStore>();
                var config = configStore.Get(tenantName);

                var section = getConfigFunc(config);
                var sectionInstance = new ConfigureFromTenantConfigurationOptions<TOptions>(section);
                return sectionInstance;
            });

            // services.ConfigureTenantOptions(new ConfigureFromTenantConfigurationOptions<TOptions>(config));
            return services;
        }
    }
}