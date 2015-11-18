using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Data.Entity;
using Shell.Models;
using Bah.Core.Site.Multitenancy;
using Microsoft.Framework.Primitives;
using Bah.Core.Site.Configuration;
using Microsoft.Framework.OptionsModel;
using Bah.Core.Site.Configuration.Options;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Shell
{
    public class TenantLookup : INamedTenantLookup<Tenant>
    {
        private ILogger _logger;

        public TenantLookup(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TenantLookup>();
        }

        public Task<Tenant> Lookup(string name)
        {
            //var dbConnectionString = this.config.DefaultConnection.ConnectionString;
            var t = new Tenant()
            {
                Name = name
                //DbConnectionString = dbConnectionString
                //"Server=.;Database=aspnet5_" + name + ";Trusted_Connection=True;MultipleActiveResultSets=true"
            };

            _logger.LogInformation($"Found tenant {name}");

            return Task.FromResult(t);
        }
    }

    public class MyOptions
    {
        public string MyOption1 { get; set; }
        public int MyOption2 { get; set; }
    }

    public class Startup
    {
        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            this.ConfigurationStore = new ConfigurationStore();
            // TODO: Be able to configure dynamically on first tenant view.
            var tenantNames = new string[]
            {
                "a",
                "client1"
            };

            foreach (var tenantName in tenantNames)
            {
                var variables = new
                {
                    site = env.EnvironmentName,
                    client = tenantName
                };

                var siteName = variables.site;// env.EnvironmentName;
                var clientName = variables.client;

                var builder = new ConfigurationBuilder()
                    .SetBasePath(appEnv.ApplicationBasePath)
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings-{siteName}.json", variables)
                    .AddJsonFile($"appsettings-{clientName}.json", variables, optional: true)
                    .AddJsonFile($"appsettings-{clientName}.{siteName}.json", variables, optional: true)
                    .AddJsonFile("final.json", variables, optional: true)
                    .AddJsonFile("local.json", variables, optional: true)
                    ;

                var config = builder;

                this.ConfigurationStore.Configurations.Add(tenantName, config.Build());
            }
        }

        //public IConfigurationRoot Configuration { get; set; }
        public ConfigurationStore ConfigurationStore { get; set; }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();

            // Uncomment the following line to add Web API services which makes it easier to port Web API 2 controllers.
            // You will also need to add the Microsoft.AspNet.Mvc.WebApiCompatShim package to the 'dependencies' section of project.json.
            // services.AddWebApiConventions();

            // services.AddOptions();
            //services.Configure<DataOptions>(this.ConfigurationStore.Get("").GetSection("Data"));
            //services.Configure<MyOptions>((x) => this.Configuration.Get<MyOptions>("MyOptions"));

            services.AddMultitenancy<Tenant>()
                .AddRouteProvider();

            services.AddSingleton<ConfigurationStore>((x) => this.ConfigurationStore);
            services.TenantConfigure<DataOptions>((configuration) => configuration.GetSection("Data"));

            services.AddSingleton<INamedTenantLookup<Tenant>, TenantLookup>();

            services.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<TestDbContext>()
                .AddMultitenantDbContext<TestDbContext>(services);
            //services.AddScoped<ITestDbContext>(provider => provider.GetService<TestDbContext>());
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();

            // Configure the HTTP request pipeline.

            // Add the following to the request pipeline only in development environment.
            if (env.IsEnvironment("dev"))
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // send the request to the following path or controller action.
                app.UseExceptionHandler("/Home/Error");
            }

            // Add the platform handler to the request pipeline.
            app.UseIISPlatformHandler();

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Setup Tenant IoC and rewrite path url.
            app.UseTenantResolver(requireTenant: true);

            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{tenant}/{controller=Home}/{action=Index}/{id?}");

                // Uncomment the following line to add a route for porting Web API 2 controllers.
                // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");
            });
        }
    }
}
