using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Siderite.Tyrion.Bot
{
    /// <summary>
    /// Class holding the commands that Tyrion answers to
    /// </summary>
    public class TyrionCommands : ModuleBase
    {
        private readonly Regex _regCommand = new Regex(@"^((""(?<param>[^""]*)""|(?<param>[^\s]+))\s*)*$",
            RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly TyrionSettings _settings;
        private readonly ILogger<TyrionCommands> _logger;
        private Regex _regYm;
        private Dictionary<string, EmoticonInfo> _ymEmoticons;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        public TyrionCommands(
            TyrionSettings settings,
            ILogger<TyrionCommands> logger)
        {
            _settings = settings;
            _logger = logger;
            InitializeYahooEmoticons();
        }

        private IMessageChannel Channel
        {
            get { return Context.Channel; }
        }
        private DiscordSocketClient Client
        {
            get { return Context.Client as DiscordSocketClient; }
        }
        private IUserMessage Message
        {
            get { return Context.Message; }
        }

        /// <summary>
        /// "translate on/off" will turn on or off translating of metric/imperial units
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Command("translate")]
        public async Task Translate([Remainder] string args = "")
        {
            await Channel.TriggerTypingAsync();
            //CheckAdmin();
            var parameters = GetParameters(args);
            var value = parameters.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
            {
                var on = Regex.IsMatch(value, @"^(1|on|true|enable|enabled|yes)$", RegexOptions.IgnoreCase);
                _settings.Translate = on;
            }
            await Channel.SendMessageAsync("Translate is " + (_settings.Translate ? "ON" : "OFF"), true);
        }

        /// <summary>
        /// "ym on/off" will turn on or off interpretations of Yahoo Messenger emoticons
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Command("ym")]
        public async Task YahooEmoticons([Remainder] string args = "")
        {
            await Channel.TriggerTypingAsync();
            //CheckAdmin();
            var parameters = GetParameters(args);
            var value = parameters.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
            {
                var on = Regex.IsMatch(value, @"^(1|on|true|enable|enabled|yes)$", RegexOptions.IgnoreCase);
                _settings.YmEnabled = on;
            }
            await Channel.SendMessageAsync("Yahoo emoticons are " + (_settings.YmEnabled ? "ON" : "OFF"), true);
        }

        /// <summary>
        /// Response to "help"
        /// </summary>
        /// <returns></returns>
        [Command("help")]
        public async Task Help()
        {
            await Channel.TriggerTypingAsync();
            await Channel.SendMessageAsync("I am still a pup! Let me know what you want me to do.", true);
        }

        /// <summary>
        /// Response to "hello"
        /// </summary>
        /// <returns></returns>
        [Command("hello")]
        public async Task Hello()
        {
            await Channel.TriggerTypingAsync();
            await Channel.SendMessageAsync("I am still a pup! Let me know what you want me to do.", true);
        }

        /// <summary>
        /// Reposne to "play"
        /// </summary>
        /// <returns></returns>
        [Command("play")]
        public async Task Play()
        {
            await Channel.TriggerTypingAsync();
            var games = new[] { "nice", "with ball", "with chew toy", "with bone", "around" };
            var game = games[new Random().Next(games.Length)];
            await Client.SetGameAsync(game, "https://siderite.dev", ActivityType.Listening);
            await Channel.SendMessageAsync("Woof, woof!", true);
        }

        /// <summary>
        /// "fart" sends an embedded fart wav.
        /// </summary>
        /// <returns></returns>
        [Command("fart")]
        public async Task Fart()
        {
            await Channel.SendManifestResourceAsync(GetType().Assembly,"audio/fart.wav");
        }

        /// <summary>
        /// This command will be executed whenever a command does not match any other defined command
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        [Command("parse")]
        public async Task Parse([Remainder]string text="")
        {
            await RecognizeName(text);
            await RecognizeBone(text);
            await TranslateUnits(text);
            await AddYahooEmoticons(text);
        }

        /// <summary>
        /// Loads the Yahoo Messenger emoticon resources
        /// </summary>
        private void InitializeYahooEmoticons()
        {
            try
            {
                using (var stream = GetType().Assembly.GetManifestStream("ym.json"))
                {
                    var json = new StreamReader(stream).ReadToEnd();
                    _ymEmoticons = JsonConvert.DeserializeObject<List<EmoticonInfo>>(json).ToDictionary(e => e.emoticon, e => e);
                }
                foreach (var pair in _ymEmoticons)
                {
                    pair.Value.path = "/img/ym/" + pair.Value.file;
                }
                _regYm = new Regex(String.Join("|", _ymEmoticons.Keys.OrderByDescending(k => k.Length).Select(Regex.Escape)));
                _settings.YmEnabled = true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, $"Error at {nameof (InitializeYahooEmoticons)}: {ex.Message}");
                _settings.YmEnabled = false;
            }
        }

        /// <summary>
        /// if the content of the message (not the prefix) contains Tyrion's name, it will add dog and love reactions
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task RecognizeName(string content)
        {
            if (Regex.IsMatch(content, @"\bTyr(i(on)?|y)?\b"))
            {
                await Channel.TriggerTypingAsync();
                await Message.AddReactionAsync(new Emoji("🐶"));
                await Message.AddReactionAsync(new Emoji("❤️"));
            }
        }

        /// <summary>
        /// if there is a bone in the message content, Tyrion will response with dog, love and bone
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task RecognizeBone(string content)
        {
            if (Regex.IsMatch(content, @"\bbone\b", RegexOptions.IgnoreCase))
            {
                await Channel.TriggerTypingAsync();
                await Message.AddReactionAsync(new Emoji("🐶"));
                await Message.AddReactionAsync(new Emoji("❤️"));
                await Message.AddReactionAsync(new Emoji("🦴"));
            }
        }

        /// <summary>
        /// if the message content contains one of the defined Yahoo Messenger emoticons, send the emoticon image
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task AddYahooEmoticons(string content)
        {
            if (!_settings.YmEnabled || _regYm == null) return;
            var match = _regYm.Match(content);
            var count = 0;
            while (match.Success)
            {
                if (_ymEmoticons.TryGetValue(match.Value, out EmoticonInfo emoticon))
                {
                    await Channel.SendManifestResourceAsync(GetType().Assembly,emoticon.path, emoticon.name);
                    if (count++ >= 5) break;
                }
                match = match.NextMatch();
            }
        }

        /// <summary>
        /// Translates imperial units to metric and viceversa
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task TranslateUnits(string content)
        {
            if (!_settings.Translate) return;
            var sb = new StringBuilder();
            var matches = Regex.Matches(content, @"(?<nr>[+-]?\d+(\.\d+)?)\s*pound(s)?", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            foreach (var match in matches.OfType<Match>())
            {
                var pounds = double.Parse(match.Groups["nr"].Value);
                var kg = pounds * 0.45359237;
                sb.Append($"{pounds:N2} pounds is {kg:N2} in kg.");
            }
            matches = Regex.Matches(content, @"(?<nr>[+-]?\d+(\.\d+)?)\s*(kg|kilo[s]?|kilogram[s]?)\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            foreach (var match in matches.OfType<Match>())
            {
                var kg = double.Parse(match.Groups["nr"].Value);
                var pounds = kg / 0.45359237;
                sb.Append($"{kg:N2} kg is {pounds:N2} in pounds.");
            }
            matches = Regex.Matches(content, @"(?<nr>[+-]?\d+(\.\d+)?)\s*F(ahrenheit)?\b", RegexOptions.ExplicitCapture);
            foreach (var match in matches.OfType<Match>())
            {
                var f = double.Parse(match.Groups["nr"].Value);
                var c = (f - 32) * 5 / 9.0;
                sb.Append($"{f:N2}F is {c:N2}C.");
            }
            matches = Regex.Matches(content, @"(?<nr>[+-]?\d+(\.\d+)?)\s*C(elsius)?\b", RegexOptions.ExplicitCapture);
            foreach (var match in matches.OfType<Match>())
            {
                var c = double.Parse(match.Groups["nr"].Value);
                var f = c * 9 / 5.0 + 32;
                sb.Append($"{c:N2}C is {f:N2}F.");
            }
            matches = Regex.Matches(content, @"(?<feet>[+-]?\d+(\.\d+)?)\s*feet(\s+(?<inches>[+-]?\d+(\.\d+)?)\s*inch(es)?)?", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            foreach (var match in matches.OfType<Match>())
            {
                var feet = double.Parse(match.Groups["feet"].Value);
                if (!string.IsNullOrWhiteSpace(match.Groups["inches"].Value))
                {
                    feet += double.Parse(match.Groups["inches"].Value)/12;
                }
                var m = feet / 3.280839895;
                sb.Append($"{feet:N2} feet is {m:N2} in meters.");
            }
            matches = Regex.Matches(content, @"(?<nr>[+-]?\d+(\.\d+)?)\s*(m\b|meter(s)?)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            foreach (var match in matches.OfType<Match>())
            {
                var m = double.Parse(match.Groups["nr"].Value);
                var feet = m * 3.280839895;
                sb.Append($"{m:N2} meters is {feet:N2} in feet.");
            }
            matches = Regex.Matches(content, @"(?<nr>[+-]?\d+(\.\d+)?)\s*(cm\b|centimeter(s)?)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            foreach (var match in matches.OfType<Match>())
            {
                var cm = double.Parse(match.Groups["nr"].Value);
                var feet = cm/100 * 3.280839895;
                sb.Append($"{cm:N2} centimeters is {feet:N2} in feet, {feet*12:N2} in inches.");
            }
            matches = Regex.Matches(content, @"(?<nr>[+-]?\d+(\.\d+)?)\s*(km|kilometer(s)?)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            foreach (var match in matches.OfType<Match>())
            {
                var km = double.Parse(match.Groups["nr"].Value);
                var miles = km * 0.62137119;
                sb.Append($"{km:N2} kilometers is {miles:N2} in miles.");
            }
            matches = Regex.Matches(content, @"(?<nr>[+-]?\d+(\.\d+)?)\s*mile(s)?", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            foreach (var match in matches.OfType<Match>())
            {
                var miles = double.Parse(match.Groups["nr"].Value);
                var km = miles / 0.62137119;
                sb.Append($"{miles:N2} miles is {km:N2} in kilometers.");
            }

            if (sb.Length > 0)
            {
                await Channel.SendMessageAsync(sb.ToString(), false);
                await Message.AddReactionAsync(new Emoji("🐶"));
            }
        }

        /// <summary>
        /// The admin user defined in appsettings.json will be compared to the source of the message
        /// If they are different, then an exception will be raised
        /// (use in Admin commands)
        /// </summary>
        private void CheckAdmin()
        {
            if (Context.User.ToString() != _settings.AdminUser)
            {
                throw new Exception("No access to this command");
            }
        }

        /// <summary>
        /// Used to get more complex parameters like a "b c" (in this case the parameter values will be "a" and "b c")
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private string[] GetParameters(string args)
        {
            var match = _regCommand.Match(args);
            var parameters = match.Success
                ? match.Groups["param"].Captures.Select(c => c.Value).ToArray()
                : null;
            return parameters;
        }

        private class EmoticonInfo
        {
            public string file { get; set; }
            public string name { get; set; }
            public string emoticon { get; set; }
            public string path { get; internal set; }
        }

    }
}
