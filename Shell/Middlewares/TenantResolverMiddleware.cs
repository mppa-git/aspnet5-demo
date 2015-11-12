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

    class TenantResolverMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public TenantResolverMiddleware(RequestDelegate next, ILoggerFactory loggerFactory) //, ITenantService<Tenant> tenantService)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<TenantResolverMiddleware>();
            //tenantService.Tenant.
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
                {
                    //var optionsBuilder2 = new DbContextOptionsBuilder<TContext>();
                    //optionsBuilder2.UseSqlServer("Server=.;Database=aspnet5_b;Trusted_Connection=True;MultipleActiveResultSets=true");
                    //return optionsBuilder2.Options;
                    return null;
                }

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
