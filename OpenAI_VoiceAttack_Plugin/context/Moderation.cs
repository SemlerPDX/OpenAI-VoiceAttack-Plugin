using OpenAI_API;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing methods for evaluating text content with the OpenAI API Moderation
    /// via the <see cref="OpenAIAPI"/> library.
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
    public static class Moderation
    {
        private static readonly string DEFAULT_FLAGGED_TTS_MESSAGE = "The input provided has been flagged as inappropriate.";

        /// <summary>
        /// A method to use OpenAI Moderation API to check user input text, and
        /// to provide the flagged categories which apply (if any).
        /// <br /><br />
        /// Return is set to the 'OpenAI_ContentFlagged' boolean variable as <see langword="true"/> if content was flagged,
        /// or as <see langword="false"/> if not flagged.
        /// <br />Also sets the 'OpenAI_Response' text variable to a string of flagged categories formatted for TTS,
        /// or to the original user input (unmodified) if not flagged.
        /// </summary>
        public static async Task Explain()
        {
            string userInput = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_UserInput") ?? string.Empty;
            string categoriesString = string.Empty;
            OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_ContentFlagged", false);

            if (string.IsNullOrEmpty(userInput))
            {
                throw new Exception("Moderation text in OpenAI_UserInput text variable is null or empty.");
            }

            OpenAIAPI api = OpenAI_Key.LoadKey
                ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.ApiKey, OpenAI_Key.ApiOrg))
                : new OpenAIAPI(
                    APIAuthentication.LoadFromPath(
                        directory: OpenAI_Key.DefaultKeyFileFolder,
                        filename: OpenAI_Key.DefaultKeyFilename,
                        searchUp: true
                    )
                );

            var result = await api.Moderation.CallModerationAsync(userInput);

            if (result.Results[0].Flagged)
            {
                var flaggedCategories = result.Results[0].FlaggedCategories.ToList();

                // Format any flagged categories for use in Text-to-Speech:
                if (flaggedCategories.Count == 1)
                {
                    categoriesString = flaggedCategories[0];
                }
                else if (flaggedCategories.Count == 2)
                {
                    categoriesString = $"{flaggedCategories[0]} and {flaggedCategories[1]}";
                }
                else
                {
                    categoriesString = $"{string.Join(", ", flaggedCategories.Take(flaggedCategories.Count - 1))}, and {flaggedCategories.Last()}";
                }

            }

            string response = result.Results[0].Flagged ? categoriesString : OpenAI_Plugin.VA_Proxy.GetText("OpenAI_UserInput");
            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Response", response);
            OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_ContentFlagged", result.Results[0].Flagged);
        }

        /// <summary>
        /// A boolean method to use OpenAI Moderation API to check user input text.
        /// <br /><br />
        /// Return is set to the 'OpenAI_ContentFlagged' boolean variable (and returns) <see langword="true"/> 
        /// if content was flagged, or <see langword="false"/> if not flagged.
        /// <br />Also sets the 'OpenAI_Response' text variable to a message stating input was inappropriate, or to the
        /// original user input (unmodified) if not flagged.
        /// </summary>
        public static async Task<bool> Check()
        {
            string userInput = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_UserInput") ?? string.Empty;
            string flagMessage = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_TTS_ContentFlagged") ?? DEFAULT_FLAGGED_TTS_MESSAGE;

            OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_ContentFlagged", false);

            if (string.IsNullOrEmpty(userInput))
            {
                Logging.WriteToLog_Long("OpenAI Plugin Error: Moderation text in OpenAI_UserInput text variable is null or empty.", "red");
                return false;
            }

            OpenAIAPI api = OpenAI_Key.LoadKey
                ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.ApiKey, OpenAI_Key.ApiOrg))
                : new OpenAIAPI(
                    APIAuthentication.LoadFromPath(
                        directory: OpenAI_Key.DefaultKeyFileFolder,
                        filename: OpenAI_Key.DefaultKeyFilename,
                        searchUp: true
                    )
                );

            var result = await api.Moderation.CallModerationAsync(userInput);

            string response = result.Results[0].Flagged
                            ? flagMessage
                            : OpenAI_Plugin.VA_Proxy.GetText("OpenAI_UserInput");

            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Response", response);
            OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_ContentFlagged", result.Results[0].Flagged);

            return result.Results[0].Flagged;
        }

    }
}
