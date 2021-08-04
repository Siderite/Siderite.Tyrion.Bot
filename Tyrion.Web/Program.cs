using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Siderite.Tyrion.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tyrion.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // That's all you need to add the Tyrion bot to your ASP.Net or Web API .NET Core site
                .UseTyrionBot()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
