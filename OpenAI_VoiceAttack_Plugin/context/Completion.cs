using OpenAI_API;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System;
using System.Threading.Tasks;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing a method for sending completion requests to the OpenAI API using the <see cref="OpenAIAPI"/> library.
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
    public static class Completion
    {
        /// <summary>
        /// Send a text completion request to the OpenAI API using the supplied input,
        /// and return the completed text, using the specified parameters (optional).
        /// </summary>
        public static async Task CompleteText()
        {
            try
            {
                string userInput = OpenAIplugin.VA_Proxy.GetText("OpenAI_UserInput") ?? String.Empty;
                string response = String.Empty;

                if (String.IsNullOrEmpty(userInput)) { throw new Exception("User prompt in OpenAI_UserInput text variable is null or empty."); }

                OpenAIplugin.VA_Proxy.SetText("OpenAI_UserInput_Last", userInput);

                // Set the OpenAI GPT Model and any/all user options and create a new Completion request:
                Model userModel = ModelsGPT.GetOpenAI_Model(true);

                // Ensure max tokens in 'OpenAI_MaxTokens' is within range for the selected model, uses 512 if not set/invalid
                int userMaxTokens = ModelsGPT.GetValidMaxTokens(userModel);

                decimal getTemp = OpenAIplugin.VA_Proxy.GetDecimal("OpenAI_Temperature") ?? 0.2M;
                decimal getTopP = OpenAIplugin.VA_Proxy.GetDecimal("OpenAI_TopP") ?? 0M;
                decimal getFreqP = OpenAIplugin.VA_Proxy.GetDecimal("OpenAI_FrequencyPenalty") ?? 0M;
                decimal getPresP = OpenAIplugin.VA_Proxy.GetDecimal("OpenAI_PresencePenalty") ?? 0M;

                string getStopSequences = OpenAIplugin.VA_Proxy.GetText("OpenAI_StopSequences") ?? String.Empty;
                string[] userStopSequences = null;
                if (!string.IsNullOrEmpty(getStopSequences))
                {
                    userStopSequences = getStopSequences.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                }

                int? userNumChoices = OpenAIplugin.VA_Proxy.GetInt("OpenAI_NumChoicesPerMessage") ?? 1;
                double userTemperature = (double)getTemp;
                double userTopP = (double)getTopP;
                double userFrequencyPenalty = (double)getFreqP;
                double userPresencePenalty = (double)getPresP;

                OpenAIAPI api = OpenAI_Key.LOAD_KEY
                    ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.API_KEY, OpenAI_Key.API_ORG))
                    : new OpenAIAPI(APIAuthentication.LoadFromPath(
                        directory: OpenAI_Key.DEFAULT_KEY_FILEFOLDER,
                        filename: OpenAI_Key.DEFAULT_KEY_FILENAME,
                        searchUp: true
                ));

                var result = await api.Completions.CreateCompletionAsync(new CompletionRequest(
                    userInput,
                    model: userModel,
                    temperature: userTemperature,
                    max_tokens: userMaxTokens,
                    top_p: userTopP,
                    numOutputs: userNumChoices,
                    frequencyPenalty: userFrequencyPenalty,
                    presencePenalty: userPresencePenalty,
                    stopSequences: userStopSequences
                ));

                if (result != null && !String.IsNullOrEmpty(result.Completions.ToString()))
                {
                    //response = result.Completions.ToString();
                    response = result.Completions[0].ToString();
                }

                if (!String.IsNullOrEmpty(response))
                {
                    OpenAIplugin.VA_Proxy.SetText("OpenAI_Response", response);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"OpenAI Plugin Error: {ex.Message}");
            }
        }
    }
}
