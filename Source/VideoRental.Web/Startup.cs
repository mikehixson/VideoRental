using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using VideoRental.Tdbr;

namespace VideoRental.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
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
            services.AddSingleton<IAuthenticationTokenProvider, HttpContextAuthenticationTokenProvider>();
            services.AddSingleton<IVideoRepository, VideoRepository>();
            services.AddSingleton<IOrderRepository, OrderRepository>();

            // BlurayRentalHttpClient will have transient scope. Factory will mange HttpMessageHandler instances.
            services.AddHttpClient<TdbrHttpClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                { 
                    AllowAutoRedirect = false,  // AllowAutoRedirect = false Gives us a chance to grab the Set-Cookie header before redirection.
                    UseCookies = false  // UseCookies = false sets it so HttpClient doesnt use a CookieContainer. HttpClient instances are reused so we dont want cookies from past requests.
                });  
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
