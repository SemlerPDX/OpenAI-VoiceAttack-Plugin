using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Speech.Synthesis;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing methods to check for plugin updates. If found, speaks a simple message using TTS and also prints redirect link to the GitHub repo.
    /// </summary>
    /// <para>
    /// <br>OpenAI API VoiceAttack Plugin</br>
    /// <br>Copyright (C) 2023 Aaron Semler</br>
    /// <br><see href="https://github.com/SemlerPDX">github.com/SemlerPDX</see></br>
    /// <br><see href="https://veterans-gaming.com/semlerpdx-avcs">veterans-gaming.com/semlerpdx-avcs</see></br>
    /// <br /><br />
    /// This program is free software: you can redistribute it and/or modify<br />
    /// it under the terms of the GNU General Public License as published by<br />
    /// the Free Software Foundation, either version 3 of the License, or<br />
    /// (at your option) any later version.<br />
    /// <br />
    /// This program is distributed in the hope that it will be useful,<br />
    /// but WITHOUT ANY WARRANTY; without even the implied warranty of<br />
    /// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the<br />
    /// GNU General Public License for more details.<br />
    /// <br />
    /// You should have received a copy of the GNU General Public License<br />
    /// along with this program.  If not, see <see href="https://www.gnu.org/licenses/">gnu.org/licenses</see>.
    /// </para>
    public static class Updates
    {
        private static readonly string PluginVersionPage = "https://veterans-gaming.com/semlerpdx/vglabs/downloads/voiceattack-plugin-openai/version.html";
        private static readonly string PluginDownloadPage = "veterans-gaming.com/semlerpdx/openai";


        /// <summary>
        /// A string indicating the latest version of this plugin, changed if an update is found.
        /// </summary>
        public static string LatestVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Check the raw version number page for this plugin on the secure VG website.
        /// </summary>
        /// <returns>A string containing the latest version, or an empty string upon failure.</returns>
        private static string GetLatestVersion()
        {
            try
            {
                using (var client = new WebClient())
                {
                    string version = client.DownloadString(PluginVersionPage);
                    return version.Trim();
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: {ex.Message}", "red");
                return String.Empty;
            }
        }

        /// <summary>
        /// A very simple update check that runs one time when the plugin is loadeded by VoiceAttack in the <see cref="OpenAIplugin.VA_Init1(dynamic)"/> method..
        /// </summary>
        /// <returns>True when update has been found, false if otherwise.</returns>
        public static bool UpdateCheck()
        {
            try
            {
                // Get current version
                string currentVersion = LatestVersion;

                // Get latest version
                string latestVersion = GetLatestVersion();
                if (String.IsNullOrEmpty(latestVersion))
                {
                    return false;
                }

                LatestVersion = latestVersion;

                // Check if update notice should be shown
                if (new Version(currentVersion) < new Version(latestVersion))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: {ex.Message}", "red");
            }
            return false;
        }

        /// <summary>
        /// A very simple update check message that speaks if the current version is below the latest published version.<br />
        /// This can only happen once per session, during init.
        /// </summary>
        public static void UpdateMessage()
        {
            try
            {

                // Get current version
                string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                OpenAIplugin.VA_Proxy.WriteToLog("", "blank");
                OpenAIplugin.VA_Proxy.WriteToLog($"Please update the OpenAI Plugin for VoiceAttack to v{LatestVersion}", "grey");
                OpenAIplugin.VA_Proxy.WriteToLog($"Download here:  {PluginDownloadPage}", "grey");
                OpenAIplugin.VA_Proxy.WriteToLog("", "blank");

                using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
                {
                    synthesizer.Volume = 65;
                    synthesizer.Rate = 1;
                    synthesizer.Speak("Please update the OpenAI Plugin for VoiceAttack to the latest version.");
                }

            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: {ex.Message}", "red");
            }
        }

    }
}
