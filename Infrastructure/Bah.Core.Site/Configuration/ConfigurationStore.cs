using Bah.Core.Site.Multitenancy;
using Microsoft.Framework.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bah.Core.Site.Configuration
{
    public class ConfigurationStore
    {
        //private Func<object, IConfigurationBuilder> builder;

        public ConfigurationStore()
        {
            this.Configurations = new Dictionary<string, IConfigurationRoot>();
            //this.builder = builder;
        }

        public Dictionary<string, IConfigurationRoot> Configurations { get; set; }

        public IConfigurationRoot Get(string tenant)
        {
            IConfigurationRoot config;
            if (!this.Configurations.TryGetValue(tenant, out config))
            {
                /*
                builder();
                var builder = new ConfigurationBuilder()
                    .SetBasePath(appEnv.ApplicationBasePath)
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings-{siteName}.json", variables)
                    .AddJsonFile($"appsettings-{clientName}.json", variables)
                    .AddJsonFile($"appsettings-{clientName}.{siteName}.json", variables, optional: true)
                    .AddJsonFile("final.json", variables, optional: true)
                    .AddJsonFile("local.json", variables, optional: true)
                    ;

                config = builder.Build();
                */
                throw new ArgumentOutOfRangeException("unknown tenant");
            }

            return config;
        }
    }
}
