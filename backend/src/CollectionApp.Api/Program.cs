using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using System;
using System.Reflection;

namespace CollectionApp.Api
{
    public class Program
    {

        public static string ENVIRONMENT = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        public static void Main(string[] args)
        {
            Console.WriteLine("ASPNETCORE_ENVIRONMENT = " + ENVIRONMENT);
            try
            {
                // Launch the application
                //////////////////////////
                // CreateDefaultBuilder loads appsettings.json, appsettings.{env}.json, user secrets and env variables
                using var host = Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webHostBuilder =>
                    {
                        webHostBuilder
                            .UseStartup<Startup>()
                            .UseUrls("http://0.0.0.0:8080", "https://0.0.0.0:8081");
                    })
                    .Build();
                host.Run();

            }
            catch (System.Exception ex)
            {
                Log.Fatal($"Failed to start {Assembly.GetExecutingAssembly().GetName().Name}", ex);
                throw;
            }
        }
    }
}