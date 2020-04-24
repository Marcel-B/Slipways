using AutoMapper;
using com.b_velop.Slipways.Data.Extensions;
using com.b_velop.Slipways.GraphQL.Middlewares;
using com.b_velop.Slipways.GrQl.Data;
using com.b_velop.Slipways.GrQl.Data.GraphQLSchema;
using com.b_velop.Slipways.Persistence;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using GraphQL.Server.Ui.Voyager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace com.b_velop.Slipways.GrQl
{
    public class Startup
    {
        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            Configuration = configuration;
            WebHostEnvironment = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public void ConfigureServices(
            IServiceCollection services)
        {
            services.AddControllers();
            services.AddAutoMapper(typeof(Program).Assembly);

            services.AddScoped<IDependencyResolver>(s => new FuncDependencyResolver(s.GetRequiredService));
            services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            services.AddSingleton<DataLoaderDocumentListener>();
            services.AddScoped<AppSchema>();
            services.AddScoped<IGraphQlRepository, GraphQlRepository>();
            services.AddDbContext<SlipwaysContext>(options =>
            {
                var connection = Configuration.GetConnectionString("postgres");
                options.UseNpgsql(connection);
                options.UseLazyLoadingProxies();
            }, ServiceLifetime.Transient);
            
            services.AddGraphQL(options =>
            {
                options.ExposeExceptions = true;
                options.EnableMetrics = true;
            })
               .AddWebSockets()
               .AddDataLoader()
               .AddGraphTypes(ServiceLifetime.Scoped);

            services.AddSlipwaysData();

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseWebSockets();
            app.UseMetricServer();
            app.UseHttpMetrics();
            app.UseMetricsMiddleware();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });

            app.UseGraphQL<AppSchema>("/graphql");
            app.UseGraphQLPlayground(options: new GraphQLPlaygroundOptions());
            app.UseGraphQLVoyager(options: new GraphQLVoyagerOptions());
        }
    }
}
