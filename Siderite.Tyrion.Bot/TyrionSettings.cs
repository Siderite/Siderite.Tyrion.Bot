using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Siderite.Tyrion.Bot
{
    /// <summary>
    /// Defines the information saved in appsettings.json
    /// </summary>
    public class TyrionSettings
    {
        /// <summary>
        /// Automatically reads the values from configuration
        /// </summary>
        /// <param name="config"></param>
        public TyrionSettings(IConfiguration config)
        {
            Token = config.GetValue<string>("Tyrion:Token");
            AdminUser = config.GetValue<string>("Tyrion:AdminUser");
            Translate = true;
            YmEnabled = true;
        }

        /// <summary>
        /// Discord bot channel
        /// </summary>
        public string Token { get; }
        /// <summary>
        /// Initial value for metric/imperial unit translation
        /// </summary>
        public bool Translate { get; set; }
        /// <summary>
        /// Initial value for Yahoo Messenger emoticons support
        /// </summary>
        public bool YmEnabled { get; set; }
        /// <summary>
        /// The Discord username of the admin user
        /// </summary>
        public string AdminUser { get; }
    }
}
