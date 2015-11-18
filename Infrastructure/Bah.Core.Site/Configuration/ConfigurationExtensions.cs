using Bah.Core.Site.Configuration;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using System;
using System.IO;

namespace Bah.Core.Site.Configuration
{
    public static class ConfigurationExtensions
    {
        public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder configurationBuilder, string path, object variables, bool optional)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Invalid path", nameof(path));
            }

            var fullPath = Path.Combine(configurationBuilder.GetBasePath(), path);

            if (!optional && !File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.", fullPath);
            }

            configurationBuilder.Add(new JsonConfigurationProviderWithSubstitution(fullPath, optional: optional, variables: variables));
            return configurationBuilder;
        }

        public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder configurationBuilder, string path, object variables)
        {
            return configurationBuilder.AddJsonFile(path, variables, false);
        }

        /*

        /// <summary>
        /// Configure an options section in a per-tenant way.  After registering via this method, 
        /// you can retrieve the options for your tenant via IoC like so:
        /// <code>serviceProvider.GetService<IOptions<TOptions>>();</code>
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="services"></param>
        /// <param name="getSection"></param>
        /// <returns></returns>
        public static IServiceCollection TenantConfigure<TOptions>(this IServiceCollection services, Func<IConfigurationRoot, IConfiguration> getSection)
            where TOptions : class
        {
            services.AddScoped<ITenantOptions<TOptions>>((s) =>
            {
                var tenantService = s.GetRequiredService<Multitenancy.ITenantService>();
                var tenantName = tenantService.Tenant.Name;

                var configStore = s.GetService<ConfigurationStore>();
                var config = configStore.Get(tenantName);

                var section = getSection(config); //.GetSection("Data");
                var sectionInstance = new ConfigureFromConfigurationOptions<TOptions>(section);
                return sectionInstance;
            });

            return services;
        }
        */
    }
}