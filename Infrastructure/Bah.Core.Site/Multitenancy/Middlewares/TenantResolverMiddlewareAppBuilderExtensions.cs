using Bah.Core.Site.Multitenancy.Middlewares;
using Microsoft.AspNet.Builder;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Internal;
using Microsoft.Framework.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bah.Core.Site.Multitenancy
{
    public static class TenantResolverMiddlewareAppBuilderExtensions
    {
        public static EntityFrameworkServicesBuilder AddMultitenantDbContext<TContext>(
            this EntityFrameworkServicesBuilder builder, IServiceCollection services) where TContext : DbContext
        {
            services.AddScoped(s =>
            {
                var tenantService = s.GetRequiredService<ITenantService<ITenant>>();
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

        public static void UseTenantResolver(this IApplicationBuilder builder, bool requireTenant = false)
        {
            builder.UseMiddleware<TenantResolverMiddleware>();
            if (requireTenant)
                builder.UseMiddleware<RequireTenantMiddleware>();
        }
    }
}
