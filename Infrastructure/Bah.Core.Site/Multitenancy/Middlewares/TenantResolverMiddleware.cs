using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bah.Core.Site.Multitenancy.Middlewares
{
    class TenantResolverMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public TenantResolverMiddleware(RequestDelegate next, ILoggerFactory loggerFactory) //, ITenantService<Tenant> tenantService)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<TenantResolverMiddleware>();
        }

        static readonly Task CompletedTask = Task.FromResult((object)null);

        public async Task Invoke(HttpContext context, TenantService service)
        {
            using (_logger.BeginScope("TenantResolverMiddleware"))
            {
                _logger.LogInformation("Invoing resolver middleware.");
                var originalPath = context.Request.Path.Value;
                var m = Regex.Match(originalPath, "/([a-zA-Z0-9]+)(/.*)");
                if (!m.Success)
                {
                    throw new ArgumentOutOfRangeException("tenant");
                    //context.Response.StatusCode = 404;
                    //return CompletedTask;
                }

                var tenantGroup = m.Groups[1];
                var realPathGroup = m.Groups[2];

                _logger.LogInformation("Setting tenant.");
                if (!await service.SetTenant(context))
                {
                    throw new Exception("failed");
                }
                _logger.LogInformation("Done setting tenant.");

                var tenantName = tenantGroup.Value;
                if (!realPathGroup.Success)
                    context.Request.Path = "/";
                else
                    context.Request.Path = realPathGroup.Value;


                /*
                var tenant = new Tenant
                {
                    Id = tenantName
                };


                _logger.LogInformation(string.Format("Resolved tenant: {0} => {1}/{2}",
                    originalPath, tenant.Id, context.Request.Path.Value));

                var tenantFeature = new TenantFeature(tenant);
                context.Features.Set<ITenantFeature>(tenantFeature);
                */

                await _next(context);
            }
        }
    }
}
