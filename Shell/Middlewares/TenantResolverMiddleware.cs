using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Internal;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shell.Middlewares
{
    public class Tenant
    {
        public string Id { get; set; }
        public string DbConnectionString { get; set; }
    }

    public interface ITenantFeature
    {
        Tenant Tenant { get; }
    }

    public class TenantFeature
      : ITenantFeature
    {
        public TenantFeature(Tenant tenant)
        {
            Tenant = tenant;
        }

        public Tenant Tenant { get; private set; }

    }

    public class TenantResolverMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public TenantResolverMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<TenantResolverMiddleware>();
        }

        static readonly Task CompletedTask = Task.FromResult((object)null);

        public Task Invoke(HttpContext context, TenantService service)
        {
            using (_logger.BeginScope("TenantResolverMiddleware"))
            {
                var originalPath = context.Request.Path.Value;
                var m = Regex.Match(originalPath, "/([a-zA-Z0-9]+)(/.*)");
                if (!m.Success)
                {
                    // throw new ArgumentOutOfRangeException("tenant");
                    context.Response.StatusCode = 404;
                    return CompletedTask;
                }

                var tenantGroup = m.Groups[1];
                var realPathGroup = m.Groups[2];

                var tenantName = tenantGroup.Value;
                if (!realPathGroup.Success)
                    context.Request.Path = "/";
                else
                    context.Request.Path = realPathGroup.Value;

                var tenant = new Tenant
                {
                    Id = tenantName
                };

                service.SetTenant(context);

                _logger.LogInformation(string.Format("Resolved tenant: {0} => {1}/{2}",
                    originalPath, tenant.Id, context.Request.Path.Value));

                var tenantFeature = new TenantFeature(tenant);
                context.Features.Set<ITenantFeature>(tenantFeature);
                return _next(context);
            }
        }
    }


    public static class TenantResolverMiddlewareAppBuilderExtensions
    {
        public static EntityFrameworkServicesBuilder AddMultitenantDbContext<TContext>(
            this EntityFrameworkServicesBuilder builder, IServiceCollection services) where TContext : DbContext
        {
            services.AddScoped(s =>
            {
                var tenantService = s.GetRequiredService<ITenantService<Tenant>>();
                var tenant = tenantService.Tenant;
                if (tenant == null)
                    return null;

                var optionsBuilder = new DbContextOptionsBuilder<TContext>();
                optionsBuilder.UseSqlServer(tenant.DbConnectionString);
                return optionsBuilder.Options;
            });
            services.AddScoped<DbContextOptions>(s =>
            {
                return s.GetRequiredService<DbContextOptions<TContext>>();
            });
            services.AddScoped(typeof(TContext), DbContextActivator.CreateInstance<TContext>);

            return builder;
        }

        public static void UseTenantResolver(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<TenantResolverMiddleware>();
        }
    }
}
