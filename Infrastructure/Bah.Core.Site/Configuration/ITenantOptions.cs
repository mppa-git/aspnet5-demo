using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bah.Core.Site.Configuration
{
    public interface ITenantOptions<out TOptions> where TOptions : class, new()
    {
        TOptions Value { get; }
    }

    public interface ITenantConfigureOptions<in TOptions> where TOptions : class
    {
        void Configure(TOptions options);
    }

    public class TenantConfigureOptions<TOptions> : ITenantConfigureOptions<TOptions> where TOptions : class
    {
        public TenantConfigureOptions(Action<TOptions> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Action = action;
        }

        public Action<TOptions> Action { get; private set; }

        public virtual void Configure(TOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Action.Invoke(options);
        }
    }

    public class ConfigureFromTenantConfigurationOptions<TOptions> : TenantConfigureOptions<TOptions>
    where TOptions : class
    {
        public ConfigureFromTenantConfigurationOptions(Microsoft.Framework.Configuration.IConfiguration config)
            : base(options => Microsoft.Framework.Configuration.ConfigurationBinder.Bind(config, options))
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
        }
    }

    public class TenantOptionsManager<TOptions> : ITenantOptions<TOptions> where TOptions : class, new()
    {
        private TOptions _options;
        private IEnumerable<ITenantConfigureOptions<TOptions>> _setups;

        public TenantOptionsManager(IEnumerable<ITenantConfigureOptions<TOptions>> setups)
        {
            _setups = setups;
        }

        public virtual TOptions Value
        {
            get
            {
                if (_options == null)
                {
                    _options = _setups == null
                        ? new TOptions()
                        : _setups.Aggregate(new TOptions(),
                                            (options, setup) =>
                                            {
                                                setup.Configure(options);
                                                return options;
                                            });
                }
                return _options;
            }
        }
    }
}
