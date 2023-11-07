using Lounge.Web.Authentication;
using Lounge.Web.Data;
using Lounge.Web.Data.ChangeTracking;
using Lounge.Web.Settings;
using Lounge.Web.Stats;
using Lounge.Web.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Lounge.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddResponseCaching();
            services.AddControllers()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddAuthentication("Basic")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lounge_API", Version = "v1" });
                c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
                {
                    Description = "Basic auth added to authorization header",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Scheme = "basic",
                    Type = SecuritySchemeType.Http
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Basic" } }] = new List<string>()
                });

                c.CustomSchemaIds(s => s.FullName);
            });

            services.AddSingleton<PlayerStatsCache>();
            services.AddSingleton<IPlayerStatCache>(s => s.GetRequiredService<PlayerStatsCache>());
            services.AddSingleton<IDbCacheUpdateSubscriber>(s => s.GetRequiredService<PlayerStatsCache>());

            services.AddSingleton<PlayerDetailsCache>();
            services.AddSingleton<IPlayerDetailsCache>(s => s.GetRequiredService<PlayerDetailsCache>());
            services.AddSingleton<IDbCacheUpdateSubscriber>(s => s.GetRequiredService<PlayerDetailsCache>());

            services.AddSingleton<RecordsCache>();
            services.AddSingleton<IRecordsCache>(s => s.GetRequiredService<RecordsCache>());
            services.AddSingleton<IDbCacheUpdateSubscriber>(s => s.GetRequiredService<RecordsCache>());

            services.AddSingleton<IPlayerDetailsViewModelService, PlayerDetailsViewModelService>();

            services.AddSingleton<ITableImageService, TableImageService>();
            services.AddSingleton<ILoungeSettingsService, LoungeSettingsService>();
            services.AddSingleton<IMkcRegistryApi, MkcRegistryApi>();
            services.AddTransient<IMkcRegistryDataUpdater, MkcRegistryDataUpdater>();
            services.AddTransient<IChangeTracker, ChangeTracker>();

            services.AddSingleton<DbCache>();
            services.AddSingleton<IChangeTrackingSubscriber>(s => s.GetRequiredService<DbCache>());
            services.AddSingleton<IDbCache>(s => s.GetRequiredService<DbCache>());

            services.AddHostedService<MkcRegistrySyncBackgroundService>();
            services.AddHostedService<DbChangeTrackingBackgroundService>();

            services.Configure<LoungeSettings>(Configuration);
            services.AddHttpClient("WithRedirects").ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
            services.AddHttpClient("NoRedirects").ConfigurePrimaryHttpMessageHandler(() => 
                new HttpClientHandler() 
                { 
                    AllowAutoRedirect = false, 
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
                });

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services
                .AddRazorPages(options => { options.Conventions.AddPageRoute("/Leaderboard", ""); })
                .AddViewLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var cultures = new CultureInfo[] { new("en"), new("ja"), new("fr"), new("de"), new("es"), new("it") };
                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
                app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Lounge_API v1"));
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            app.UseSwagger();

            var locOptions = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int durationInSeconds = 60 * 60 * 24;
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=" + durationInSeconds;
                }
            });
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseResponseCaching();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
