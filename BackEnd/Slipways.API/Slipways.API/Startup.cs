using System.Net;
using com.b_velop.Utilities.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Microsoft.AspNetCore.Identity;
using com.b_velop.Slipways.Domain.Identity;
using com.b_velop.Slipways.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MediatR;
using com.b_velop.Slipways.Application.Slipway;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Repositories;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authorization;
using com.b_velop.Slipways.Application.Interfaces;
using com.b_velop.Slipways.Application.User;
using com.b_velop.Slipways.Infrastructure.Docker;
using com.b_velop.Slipways.Infrastructure.Security;
using FluentValidation.AspNetCore;

namespace com.b_velop.Slipways.API
{
    public class Startup
    {
        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public ILogger<Startup> Logger { get; set; }
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public void ConfigureServices(
            IServiceCollection services)
        {
            var secretProvider = new SecretProvider();
            services.AddCors(opt =>
            {
                opt.AddPolicy("CorsPolicy", policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:3000");
                });
            });

            services.AddControllers(opt =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                opt.Filters.Add(new AuthorizeFilter(policy));
            }) .AddFluentValidation(cfg =>
                {
                    cfg.RegisterValidatorsFromAssemblyContaining<Login>();
                })
                .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.AddCors();
            services.AddScoped<IJwtGenerator, JwtGenerator>();
            services.AddScoped<IUserAccessor, UserAccessor>();

            var builder = services.AddIdentityCore<AppUser>(opt =>
            {
                opt.Password.RequireDigit = false;
                opt.Password.RequiredLength = 4;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
            });

            builder = new IdentityBuilder(builder.UserType, typeof(Role), builder.Services);
            builder.AddEntityFrameworkStores<SlipwaysContext>();
            builder.AddRoleValidator<RoleValidator<Role>>();
            builder.AddRoleManager<RoleManager<Role>>();
            builder.AddSignInManager<SignInManager<AppUser>>();

            var keyString = string.Empty;
            var connection = string.Empty;
            
            if (WebHostEnvironment.IsProduction())
            {
                keyString = secretProvider.GetSecret("key");
                var dbPassword = secretProvider.GetSecret("db_slip_password");
                connection = $"Host=slip_db;Port=5432;Username=sailor;Password={dbPassword};Database=slipways;";
            }
            else
            { 
                connection = Configuration.GetConnectionString("postgres");
                keyString = Configuration["TokenKey"];
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
                {
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateAudience = false, // Url is comming from
                        ValidateIssuer = false
                    };
                });

            services.AddMediatR(typeof(List).Assembly);
            services.AddAutoMapper(typeof(List).Assembly);
            
            services.AddScoped<ISlipwayRepository, SlipwayRepository>();       
            services.AddScoped<IStationRepository, StationRepository>();       
            services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
            services.AddScoped<IWaterRepository, WaterRepository>();
            services.AddScoped<IMarinaRepository, MarinaRepository>();
            
            services.AddDbContext<SlipwaysContext>(options =>
            {
                options.UseNpgsql(connection);
                options.UseLazyLoadingProxies();
            });
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            ILogger<Startup> logger)
        {
            Logger = logger;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder =>
                {
                    builder.Run(async context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null)
                        {
                            Logger.LogError(9999, error.Error, error.Error.Message);
                            context.Response.AddApplicationError(error.Error.Message);
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
            }
            app.UseHttpMetrics();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            
            app.UseRouting();
            app.UseCors("CorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics();
                endpoints.MapFallbackToController("Index", "Fallback");
            });
        }
    }
}
