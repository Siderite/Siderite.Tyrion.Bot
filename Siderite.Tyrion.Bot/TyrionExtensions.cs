using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Siderite.Tyrion.Bot
{
    /// <summary>
    /// Extension methods used in the projects
    /// </summary>
    public static class TyrionExtensions
    {
        /// <summary>
        /// Get a manifest stream from resources by name
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Stream GetManifestStream(this Assembly assembly, string filename)
        {
            string name = "Siderite.Tyrion.Bot.resources." + Regex.Replace(filename.Trim(new[] { '/', '\\' }), @"[\/]", ".");
            return assembly.GetManifestResourceStream(name);
        }

        /// <summary>
        /// Send a manifest resource to a Discord channel
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="assembly"></param>
        /// <param name="path"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task<IUserMessage> SendManifestResourceAsync(this IMessageChannel channel, Assembly assembly, string path, string message=null)
        {
            using (var stream = assembly.GetManifestStream(path))
            {
                var filename = Path.GetFileName(path);
                return await channel.SendFileAsync(stream, filename, message);
            }
        }
    }
}
