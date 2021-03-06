﻿using IntermediatorBotSample.Bot;
using Intermediator.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;

namespace IntermediatorBotSample
{
    public class Startup
    {
        public IConfiguration Configuration
        {
            get;
        }
        public ILoggerFactory LoggerFactory { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddUserSecrets<Startup>()
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddControllersAsServices();
            services.AddSingleton(_ => Configuration);
            
            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // The Dialog that will be run by the bot.
            services.AddSingleton<Dialogs.MainDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, IntermediatorBot<Dialogs.MainDialog>>();
            services.AddSingleton<Services.IBotServices, Services.DispatcherService>();

            // Add the HttpClientFactory to be used for the QnAMaker calls.
            services.AddHttpClient();

            services.AddBot<IntermediatorBot<Dialogs.MainDialog>>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(Configuration);

                // The CatchExceptionMiddleware provides a top-level exception handler for your bot. 
                // Any exceptions thrown by other Middleware, or by your OnTurn method, will be 
                // caught here. To facillitate debugging, the exception is sent out, via Trace, 
                // to the emulator. Trace activities are NOT displayed to users, so in addition
                // an "Ooops" message is sent. 
                options.Middleware.Add(new CatchExceptionMiddleware<Exception>(async (context, exception) =>
                {
                    await context.TraceActivityAsync("Bot Exception", exception);
                    await context.SendActivityAsync($"Sorry, it looks like something went wrong:{exception.Message}");
#if DEBUG
                    await context.SendActivityAsync($"Stack trace:\n{exception.ToString()}");
#endif
                }));

                // The Memory Storage used here is for local bot debugging only. When the bot
                // is restarted, anything stored in memory will be gone. 
                //IStorage dataStore = new MemoryStorage();

                // The File data store, shown here, is suitable for bots that run on 
                // a single machine and need durable state across application restarts.                 
                // IStorage dataStore = new FileStorage(System.IO.Path.GetTempPath());

                // For production bots use the Azure Table Store, Azure Blob, or 
                // Azure CosmosDB storage provides, as seen below. To include any of 
                // the Azure based storage providers, add the Microsoft.Bot.Builder.Azure 
                // Nuget package to your solution. That package is found at:
                //      https://www.nuget.org/packages/Microsoft.Bot.Builder.Azure/

                // IStorage dataStore = new Microsoft.Bot.Builder.Azure.AzureTableStorage("AzureTablesConnectionString", "TableName");
                // IStorage dataStore = new Microsoft.Bot.Builder.Azure.AzureBlobStorage("AzureBlobConnectionString", "containerName");

                // Handoff middleware
                options.Middleware.Add(new Intermediator.Middleware.TimeoutMiddleware(Configuration, LoggerFactory));
                options.Middleware.Add(new Middleware.HandoffMiddleware(Configuration, LoggerFactory));
            });

            services.AddMvc(); // Required Razor pages
        }

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            LoggerFactory = loggerFactory;

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseMvc() // Required Razor pages
                .UseBotFramework();
        }
    }
}
