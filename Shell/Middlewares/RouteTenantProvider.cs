using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shell.Middlewares
{
    public class RouteTenantProviderOptions
    {
        //public string BaseDomain { get; set; } = null;

        //public bool AllowMultipleLevels { get; set; } = false;

        public bool RequireValidTenant { get; set; } = true;
    }

    public class RouteTenantProvider<TTenant> : ITenantProvider<TTenant>
        where TTenant : class
    {
        readonly static Tuple<TTenant, bool> Failure = Tuple.Create(default(TTenant), false);
        //readonly static Regex PortNumberRegex = new Regex(":\\d{1,5}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        readonly RouteTenantProviderOptions _options;
        readonly ILogger _logger;

        public RouteTenantProvider(IOptions<RouteTenantProviderOptions> options, ILoggerFactory factory)
        {
            _options = options.Value;
            _logger = factory.CreateLogger<RouteTenantProvider<TTenant>>();
        }

        public async Task<Tuple<TTenant, bool>> TryGetTenant(HttpContext context)
        {
            /*
            StringValues hostNameHeader;
            if (!context.Request.Headers.TryGetValue("Host", out hostNameHeader))
                return Failure;

            if (hostNameHeader.Count != 1)
                return Failure;

            var hostName = hostNameHeader[0];
            if (string.IsNullOrEmpty(hostName))
                return Failure;

            // Remove port number
            hostName = PortNumberRegex.Replace(hostName, "");

            string Route;
            if (!string.IsNullOrEmpty(_options.BaseDomain))
            {
                if (!hostName.EndsWith("." + _options.BaseDomain))
                    return Failure;

                Route = hostName.Substring(0, hostName.Length - _options.BaseDomain.Length - 1);
            }
            else
            {
                // Assuming the normal Route.dommain.topdomain layout.
                // Also, default exception for "localhost". Anything else
                // will fail and need to be configured using BaseDomain
                // on the options object.
                var parts = hostName.Split('.');

                if (parts[parts.Length - 1] == "localhost")
                {
                    if (parts.Length <= 1)
                        return Failure;

                    Route = string.Join(".", new ArraySegment<string>(parts, 0, parts.Length - 1));
                }
                else
                {
                    if (parts.Length <= 2)
                        return Failure;

                    Route = string.Join(".", new ArraySegment<string>(parts, 0, parts.Length - 2));
                }
            }

            if (Route.Contains(".") && !_options.AllowMultipleLevels)
                return Failure;
                */

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
    //originalPath, tenant.Id, context.Request.Path.Value));

            return Tuple.Create(tenant, true);
        }
    }
}
