#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using CloudFsmApi.Config;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Model;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using System.IO;
using System.Linq;

namespace CloudFsmApi
{
    public class Startup
    {
        private const int ApiMajorVersion = 1;

        private const int ApiMinorVersion = 0;

        private const string ApiTitle = "Raven.CloudFsm API";

        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        private IHostingEnvironment HostingEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger"; // serve the UI at root
                options.SwaggerEndpoint($"/swagger/{GetSwaggerApiVersion()}/swagger.json", $"{ApiTitle} {GetSwaggerApiVersion()}");
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<StorageConfig>(Configuration.GetSection("Storage"));
            services.Configure<DownlinkManagerConfig>(Configuration.GetSection("DownlinkManager"));

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.DocInclusionPredicate((version, apiDescription) =>
                {
                    var values = apiDescription.RelativePath
                     .Split('/')
                     .Select(v => v.Replace("v{api-version}", version));

                    apiDescription.RelativePath = string.Join("/", values);

                    var versionParameter = apiDescription.ParameterDescriptions.FirstOrDefault(p => p.Name == "api-version");

                    if (versionParameter != null)
                    {
                        apiDescription.ParameterDescriptions.Remove(versionParameter);
                    }

                    return true;
                });

                c.SwaggerDoc(GetSwaggerApiVersion(), new Info
                {
                    Version = GetSwaggerApiVersion(),
                    Title = ApiTitle,
                });

                //Set the comments path for the swagger json and ui.
                var xmlPath = Path.Combine(HostingEnvironment.WebRootPath, GetType().Assembly.GetName().Name + ".xml");

                c.IncludeXmlComments(xmlPath);
                var xmlPath2 = Path.Combine(HostingEnvironment.WebRootPath, "Model.xml");
                c.IncludeXmlComments(xmlPath2);


            });

            services.AddMvc(options =>
            {
            }).AddFluentValidation()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

            services.AddApiVersioning(
                o =>
                {
                    o.ReportApiVersions = true;
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                });

            services.AddApplicationInsightsTelemetry();

            //Add services here
            services.AddSingleton<IDownlinkManager, DownlinkManager>();
            services.AddSingleton<ISceneMgr, FsmSceneManager>();
        }

        private static string GetSwaggerApiVersion()
        {
            return $"v{ApiMajorVersion}.{ApiMinorVersion}";
        }
    }
}
