using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bah.Core.Site.Multitenancy.Middlewares
{
    class RequireTenantMiddleware
    {
        static readonly Task CompletedTask = Task.FromResult((object)null);

        readonly RequestDelegate _next;

        public RequireTenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context, ITenantService service)
        {
            if (service.Tenant == null)
            {
                context.Response.StatusCode = 404;
                return CompletedTask;
            }

            return _next(context);
        }
    }
}
