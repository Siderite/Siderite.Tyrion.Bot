using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Siderite.Tyrion.Bot
{
    /// <summary>
    /// Extension methods for a .NET Core web or API site
    /// </summary>
    public static class TyrionBootstrap
    {
        /// <summary>
        /// Use Tyrion bot for <see cref="IWebHostBuilder"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseTyrionBot(this IWebHostBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services
                    .AddSingleton<TyrionSettings>()
                    .AddSingleton<CommandService>()
                    .AddHostedService<TyrionBot>();
            });
        }

        /// <summary>
        /// Use Tyrion bot for <see cref="IHostBuilder"/> (.Net 5.0)
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHostBuilder UseTyrionBot(this IHostBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services
                    .AddSingleton<TyrionSettings>()
                    .AddSingleton<CommandService>()
                    .AddHostedService<TyrionBot>();
            });
        }
    }
}
