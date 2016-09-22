﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.ActiveDirectory;
using Microsoft.AspNetCore.Authentication.ActiveDirectory.Events;
using Microsoft.AspNetCore.Http;

namespace Sample_AspNet5.Mvc6.Ntlm
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //ActiveDirectory: Add the authentication middleware configuration
            services.AddAuthentication(options => new ActiveDirectoryCookieOptions());

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                // TODO: Fix this for RTM
                //app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();



            //ActiveDirectory: set up cookies for client-side session identitfication
            //app.UseCookieAuthentication(new ActiveDirectoryCookieOptions().ApplicationCookie);

            //EXAMPLE: using with a custom action URL
            app.UseCookieAuthentication(
                new ActiveDirectoryCookieOptions(
                    new CookieAuthenticationOptions()
                    {
                        AuthenticationScheme = typeof(ActiveDirectoryCookieOptions).Namespace + ".Application",
                        AutomaticAuthenticate = true,
                        AutomaticChallenge = true,
                        ReturnUrlParameter = "ReturnUrl",
                        LoginPath = new PathString("/windowsauthentication/ntlm"),
                        AccessDeniedPath = new PathString("/windowsauthentication/ntlm")
                    }).ApplicationCookie
            );

            //ActiveDirectory: add the NTLM middlware in the pipeline
            app.UseNtlm(new ActiveDirectoryOptions
            {
                AutomaticAuthenticate = false,
                AutomaticChallenge = false,
                AuthenticationScheme = ActiveDirectoryOptions.DefaultAuthenticationScheme,
                SignInAsAuthenticationScheme = ActiveDirectoryOptions.DefaultAuthenticationScheme,

                //Optionally, you can handle the events below
                Events = new AuthenticationEvents()
                {
                    OnAuthenticationSucceeded = succeededContext =>
                    {
                        var userName = succeededContext.Ticket.Principal.Identity.Name;

                        //do something on successful authentication

                        return Task.FromResult<object>(null);
                    },
                    OnAuthenticationFailed = failedContext =>
                    {
                        //do something on failed authentication

                        return Task.FromResult<object>(null);
                    }
                }
            });
            

            //EXAMPLE: using with a custom action URL
            //ActiveDirectory: add the NTLM middlware in the pipeline
            //app.UseNtlm(new ActiveDirectoryOptions
            //{
            //    AutomaticAuthenticate = false,
            //    AutomaticChallenge = false,
            //    AuthenticationScheme = ActiveDirectoryOptions.DefaultAuthenticationScheme,
            //    SignInAsAuthenticationScheme = ActiveDirectoryOptions.DefaultAuthenticationScheme,
            //    CallbackPath = new PathString("/api/windowsauthentication/ntlm")
            //});

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                //EXAMPLE: using with a custom action URL
                //routes.MapRoute(
                //    name: "authentication",
                //    template: "api/{controller=WindowsAuthentication}/{action=Ntlm}");

            });
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration();

            var webHost = webHostBuilder.Build();
            webHost.Run();
        }
    }
}
