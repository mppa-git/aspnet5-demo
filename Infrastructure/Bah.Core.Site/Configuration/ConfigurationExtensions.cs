using Bah.Core.Site.Configuration;
using Microsoft.Framework.Configuration;
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
    }
}