using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using VideoRental.Core;

namespace VideoRental.Web
{
    public class Startup
    {
        public const string AppS3BucketKey = "AppS3Bucket";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // use old handler so that uses IHTTP so we can se stuff in fiddler.
            //AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

            // Add S3 to the ASP.NET Core dependency injection framework.
            //services.AddAWSService<Amazon.S3.IAmazonS3>();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                     options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("veryVerySecretKey"))
                    };
                });

            services.AddHttpContextAccessor();

            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<ICookieTokenProvider, CookieTokenProviderHttpContext>();
            services.AddSingleton<IVideoRepository, VideoRepository>();
            services.AddSingleton<IOrderRepository, OrderRepository>();
            services.AddSingleton<IAddressRepository, AddressRepository>();

            // BlurayRentalHttpClient will have transient scope
            services.AddHttpClient<BlurayRentalHttpClient>(c => {
                c.BaseAddress = new Uri("https://www.store-3d-blurayrental.com");
                c.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        // Order matters https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/index?tabs=aspnetcore2x&view=aspnetcore-3.0
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            
            app.UseAuthentication();    // This has to go before UseMvc()
            app.UseMvc();

        }
    }
}
