using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Siderite.Tyrion.Bot
{
    /// <summary>
    /// A sample Discord bot to be used inside ASP.Net Core web projects
    /// </summary>
    public class TyrionBot:IHostedService,IDisposable
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _services;
        private readonly TyrionSettings _settings;
        private readonly ILogger<TyrionBot> _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="commandService"></param>
        /// <param name="services"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        public TyrionBot( 
            CommandService commandService,
            IServiceProvider services,
            TyrionSettings settings,
            ILogger<TyrionBot> logger)
        {
            _client = new DiscordSocketClient();
            _commandService = commandService;
            _services=services;
            _settings = settings;
            _logger = logger;
            _logger.LogInformation("Started Tyrion");
        }

        /// <summary>
        /// Executed when the bot starts
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            _client.Log += log;
            _client.Ready += ready;
            _client.MessageReceived += messageReceived;
            _commandService.Log += log;
            _commandService.CommandExecuted += commandExecuted;
            // commands will be automatically loaded from this assembly (see TyrionCommands.cs)
            var assembly = GetType().Assembly;
            await _commandService.AddModulesAsync(assembly, _services);
            // don't forget to add the necessary credentials to appsettings.json
            await _client.LoginAsync(TokenType.Bot, _settings.Token);
            await _client.StartAsync();
        }

        /// <summary>
        /// Executed when a bot command is executed (a chat command starting with Tyr or Tyrion)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task commandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found (no defined commands matched the input)
            if (!command.IsSpecified)
            {
                await context.Channel.SendMessageAsync("Woof!", true);
                await context.Message.AddReactionAsync(new Emoji("🐶"));
                return;
            }

            // log failure to the console 
            if (!result.IsSuccess)
            {
                await log(new LogMessage(LogSeverity.Error, nameof(commandExecuted), $"Error: {result.ErrorReason}"));
                return;
            }
            // "parse" is the name of the command that will handle all unrecognized commands
            // "Tyr something" will attempt to match a command called "something"
            //   and when it fails, it executes command "parse" with parameter "something", equivalent to "Tyr parse something"
            if (command.Value.Name != "parse")
            {
                // react to message
                await context.Message.AddReactionAsync(new Emoji("🐶"));
            }
        }

        /// <summary>
        /// Executed when the bot is loaded, logged in and ready to receive commands
        /// </summary>
        /// <returns></returns>
        private async Task ready()
        {
            try
            {
                await _client.SetStatusAsync(UserStatus.Online);
                await _client.SetGameAsync("nice", "https://siderite.dev", ActivityType.Listening);
            }
            catch (Exception ex)
            {
                await log(new LogMessage(LogSeverity.Error, nameof(ready), $"Error: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// executed when a Discord message is received
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task messageReceived(SocketMessage message)
        {
            try
            {
                await processMessage(message);
            }
            catch (Exception ex)
            {
                var reply = await message.Channel.SendMessageAsync($"Error: {ex.Message}", false);
                await reply.AddReactionAsync(new Emoji("😱"));
            }
        }

        /// <summary>
        /// Handle Discord messages
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private async Task processMessage(SocketMessage msg)
        {
            // only user socket messages
            if (msg.Author.IsBot || msg.Source != MessageSource.User) return;
            if (msg is not SocketUserMessage message) return;

            // command has to start with Tyr, Tyry or Tyrion to respond
            var match = Regex.Match(message.Content, @"^\s*Tyr(ion|y)?\s+", RegexOptions.IgnoreCase);
            int? pos = null;
            if (match.Success)
            {
                pos = match.Length;
            }
            else
            {
                // if it's not a match, but it's sent on the private channel of the bot, it doesn't need a prefix
                var dmChannel = await message.Author.GetOrCreateDMChannelAsync();
                if (message.Channel==dmChannel)
                {
                    pos = 0;
                }
            }
            var context = new SocketCommandContext(_client, message);
            if (pos.HasValue)
            {
                await _commandService.ExecuteAsync(context, message.Content[pos.Value..],_services);
            } else
            {
                await _commandService.ExecuteAsync(context, "parse "+message.Content, _services);
            }
        }

        /// <summary>
        /// Handles discord client and command service logging
        /// Will also be used manually in code in some cases
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Task log(LogMessage message)
        {
            _logger.Log(getLogLevel(message.Severity), message.Exception, message.Source + ":" + message.Message);
            Debug.WriteLine($"Tyrion log: {message.Message}");
            return Task.CompletedTask;
        }

        private static LogLevel getLogLevel(LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Debug => LogLevel.Debug,
                _ => throw new NotImplementedException(nameof(severity) + "=" + severity.ToString()),
            };
        }

        /// <summary>
        /// Executed when the bot shuts down
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            await _client.SetGameAsync(null);
            await _client.SetStatusAsync(UserStatus.Offline);
            _client.Log -= log;
            _client.Ready -= ready;
            _client.MessageReceived -= messageReceived;
            _commandService.Log -= log;
            _commandService.CommandExecuted -= commandExecuted;
            await _client.StopAsync();
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running Tyrion as hosted service");
            await RunAsync();
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            await StopAsync();
            _logger.LogInformation("Stopping Tyrion as hosted service");
        }

        /// <summary>
        /// Disposable implementation disposes the Discord client
        /// </summary>
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        {
            _client.Dispose();
        }
    }
}
