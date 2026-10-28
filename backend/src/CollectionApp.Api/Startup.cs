using AspNetCoreRateLimit;
using CollectionApp.Api.Http.Configurations;
using CollectionApp.Api.Http.Exceptions;
using CollectionApp.Api.Http.Filters;
using CollectionApp.Api.Http.Middlewares;
using CollectionApp.Api.Swagger;
using CollectionApp.Application.Messaging;
using CollectionApp.Infrastructure.Utils;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Logging;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;

namespace CollectionApp.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; private set; }
        public string Environment { get; private set; }
        protected List<Assembly> LoadedAssemblies { get; private set; } = AssemblyTools.GetAssemblies();

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env.EnvironmentName;
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Default HttpContextAccessor
            services.AddHttpContextAccessor();

            // Default processing stratgey
            ///////////////////////////////////////
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

            //Check response filter / Format possible exceptions
            ////////////////////////////////////
            services.AddControllers(options =>
            {
                options.Filters.Add<HttpExceptionFilter>();
            });

            //Initialize application
            ///////////////////////////////////////////
            InitializeServices(services, Configuration);

            //CORS
            ///////////////////////////
            services.AddCors();

            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(180);
            });

            //Response compression
            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
            });

            // AspNetCoreRateLimit
            services.AddMemoryCache();
            //load general configuration from appsettings.json
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

            //load ip rules from appsettings.json
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            // inject counter and rules stores
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IRateLimitConfiguration, CustomRateLimitConfiguration>();

            //Add API versioning
            ///////////////////////////////////
            ConfigureApiVersion(services);

            //Swagger
            ////////////////////////////////////////////
            services.AddSwagger(Configuration);

            //Add MediatR events management
            ///////////////////////////////////////////
            services.AddMessaging(LoadedAssemblies);

        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="apiVersionProvider"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider apiVersionProvider, ILoggerFactory loggerFactory)
        {
            app.UseIpRateLimiting();

            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                IdentityModelEventSource.ShowPII = true;
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Localization
            ///////////////////////////
            IList<CultureInfo> supportedCultures = new List<CultureInfo>
            {
                //new CultureInfo("en-US"),
                new CultureInfo("fr-FR"),
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("fr-FR"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            // Swagger
            ///////////////////////////
            app.AddSwagger(Configuration, apiVersionProvider);
            
            // CORS
            ////////////////////////////////////////////////
            var corsOrigins = (Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []).Select(x => x.ToLower()).ToList();
            Console.WriteLine("Allowed Origins = " + string.Join(";", corsOrigins));
            app.UseCors(builder => builder
                .SetIsOriginAllowed(origin => corsOrigins.Any(x => origin?.StartsWith(x) ?? false))
                .AllowAnyHeader().AllowAnyMethod().AllowCredentials()
                .WithExposedHeaders("Content-Disposition", "Authorization", "Access-Control-Allow-Origin"));


            // Add middlewares
            ////////////////////////////////////////////

            //Microsoftt middlewares
            app.UseRouting();

            //Checking response middleware
            app.UseStatusCodePages(statusCodeContext =>
            {
                HttpExceptionTools.SetExceptionResponse(statusCodeContext.HttpContext).Wait();
                return Task.CompletedTask;
            });

            //Add authentication middlewares
            ConfigureAuthorization(app);

            //Custom middlewares
            ConfigureMiddlewares(app);

            //Culture
            //////////////////////////////////////
            app.Use(async (context, next) =>
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                await next();
            });

            //Endpoints & Redirections
            /////////////////////////////////////
            app.UseHttpsRedirection();

            app.UseEndpoints(endpoints =>
            {
                // Mapping of endpoints goes here:
                endpoints.MapControllers();
                // endpoints.MapSwagger();
            });
            app.UseDefaultFiles();


        }

        /// <summary>
        /// Configure default authorization middlewares
        /// </summary>
        /// <param name="app">An IApplicationBuilder</param>
        private void ConfigureAuthorization(IApplicationBuilder app)
        {
            //Add authentication middlewares
            app.UseAuthentication();

            //Add authorization middlewares
            app.UseAuthorization();
        }

        /// <summary>
        /// Configure additional Middlewares
        /// </summary>
        /// <param name="app">An IApplicationBuilder</param>
        private void ConfigureMiddlewares(IApplicationBuilder app)
        {
            app.UseExceptionMiddleware();
            app.UseSecureCookieMiddleware();
        }

        private void InitializeServices(IServiceCollection services, IConfiguration configuration)
        {
            
        }

        private void ConfigureApiVersion(IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            // Registers IApiVersionDescriptionProvider
            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
        }

        
    }
   
}