using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bah.Core.Site.Multitenancy
{
    public class RouteTenantProviderOptions
    {
        public bool RequireValidTenant { get; set; } = true;
    }

    public class RouteTenantProvider<TTenant> : ITenantProvider<TTenant>
        where TTenant : ITenant
    {
        readonly static Tuple<TTenant, bool> Failure = Tuple.Create(default(TTenant), false);

        readonly RouteTenantProviderOptions _options;
        readonly ILogger _logger;

        public RouteTenantProvider(IOptions<RouteTenantProviderOptions> options, ILoggerFactory factory)
        {
            _options = options.Value;
            _logger = factory.CreateLogger<RouteTenantProvider<TTenant>>();
        }

        public async Task<Tuple<TTenant, bool>> TryGetTenant(HttpContext context)
        {
            string Route = "";

            var originalPath = context.Request.Path.Value;
            var m = Regex.Match(originalPath, "/([a-zA-Z0-9]+)(/.*)");
            if (m.Success)
            {
                var tenantGroup = m.Groups[1];
                var realPathGroup = m.Groups[2];

                Route = tenantGroup.Value;
            }

            _logger.LogInformation("Route is {0}. Looking up tenant.", Route);

            var lookup = context.RequestServices.GetRequiredService<INamedTenantLookup<TTenant>>();

            var tenant = await lookup.Lookup(Route);

            if (tenant == null)
            {
                if (_options.RequireValidTenant)
                {
                    _logger.LogCritical("Valid tenant for Route {0} not found.", Route);
                    context.Response.StatusCode = 404;
                    return null;
                }
                else
                {
                    return Failure;
                }
            }

            //_logger.LogInformation(string.Format("Resolved tenant: {0} => {1}/{2}",
            //    originalPath, tenant., context.Request.Path.Value));

            return Tuple.Create(tenant, true);
        }
    }
}
