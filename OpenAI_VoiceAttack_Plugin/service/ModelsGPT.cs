using OpenAI_API;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing methods for identifying the OpenAI_API <see cref="Model"/> and
    /// valid MaxTokens to use for a given OpenAI API request.
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
    public static class ModelsGPT
    {
        #region OpenAImodels
        private static readonly Model DEFAULT_CHAT_MODEL = Model.ChatGPTTurbo;
        private static readonly string DEFAULT_CHAT_MODEL_NAME = "ChatGPTTurbo";

        private static readonly Model DEFAULT_COMPLETION_MODEL = Model.DavinciText;
        private static readonly string DEFAULT_COMPLETION_MODEL_NAME = "DavinciText";

        /// <summary>
        /// A dictionary of GPT models with text string names as keys and their values as a corresponding OpenAI_API <see cref="Model"/>. 
        /// </summary>
        private static Dictionary<string, Model> OpenAImodels { get; } = new Dictionary<string, Model>()
        {
            { "AdaText", Model.AdaText },
            { "text-ada-001", Model.AdaText },
            { "BabbageText", Model.BabbageText },
            { "text-babbage-001", Model.BabbageText },
            { "CurieText", Model.CurieText },
            { "text-curie-001", Model.CurieText },
            { "DavinciText", Model.DavinciText },
            { "text-davinci-003", Model.DavinciText },
            { "CushmanCode", Model.CushmanCode },
            { "code-cushman-001", Model.CushmanCode },
            { "DavinciCode", Model.DavinciCode },
            { "code-davinci-002", Model.DavinciCode },
            { "ChatGPTTurbo", Model.ChatGPTTurbo },
            { "gpt-3.5-turbo", Model.ChatGPTTurbo },
            { "ChatGPTTurbo0301", Model.ChatGPTTurbo0301 },
            { "gpt-3.5-turbo-0301", Model.ChatGPTTurbo0301 },
            { "GPT4", Model.GPT4 },
            { "gpt-4", Model.GPT4 },
            { "GPT4_32k_Context", Model.GPT4_32k_Context },
            { "gpt-4-32k", Model.GPT4_32k_Context }
        };

        /// <summary>
        /// A dictionary of GPT models as keys and their max tokens as integer values.
        /// </summary>
        private static Dictionary<Model, int> MaxTokensByModel { get; } = new Dictionary<Model, int>()
        {
            { Model.AdaText, 2048 },
            { Model.BabbageText, 2048 },
            { Model.CurieText, 2048 },
            { Model.DavinciText, 4096 },
            { Model.CushmanCode, 2048 },
            { Model.DavinciCode, 8000 },
            { Model.ChatGPTTurbo, 4096 },
            { Model.ChatGPTTurbo0301, 4096 },
            { Model.GPT4, 8192 },
            { Model.GPT4_32k_Context, 32768 }
        };

        private static List<string> OpenAICompletionModels { get; } = new List<string>()
        {
            "AdaText",
            "text-ada-001",
            "BabbageText",
            "text-babbage-001",
            "CurieText",
            "text-curie-001",
            "DavinciText",
            "text-davinci-003",
            "CushmanCode",
            "code-cushman-001",
            "DavinciCode",
            "code-davinci-002"
        };

        private static List<string> OpenAIChatGPTmodels { get; } = new List<string>()
        {
            "ChatGPTTurbo",
            "gpt-3.5-turbo",
            "ChatGPTTurbo0301",
            "gpt-3.5-turbo-0301",
            "GPT4",
            "gpt-4",
            "GPT4_32k_Context",
            "gpt-4-32k"
        };
        #endregion


        /// <summary>
        /// A method to get the <see cref="OpenAIAPI"/> <see cref="Model"/> specified in the 'OpenAI_Model' VoiceAttack text variable.
        /// <br>See also <see cref="OpenAImodels"/> and <see cref="MaxTokensByModel"/></br>
        /// </summary>
        /// <returns> The <see cref="OpenAIAPI"/> <see cref="Model"/> corresponding to the provided model name,
        /// or <see cref="Model.ChatGPTTurbo"/> if not set (or no match found).</returns>
        /// <exception cref="ArgumentNullException">
        /// On any exceptions, the default <see cref="OpenAIAPI"/> <see cref="Model.ChatGPTTurbo"/> will be set, and notify users of the error.
        /// </exception>
        public static Model GetOpenAI_Model() { return GetOpenAI_Model(false); }
        /// <summary>
        /// A method to get the <see cref="OpenAIAPI"/> <see cref="Model"/> specified in the 'OpenAI_Model' VoiceAttack text variable.
        /// <br>See also <see cref="OpenAImodels"/> and <see cref="MaxTokensByModel"/></br>
        /// </summary>
        /// <param name="completionModel">
        /// A boolean to return only a model which can be used in Completion (<see cref="Model.DavinciText"/> if not set, or no match found).</param>
        /// <returns> The <see cref="OpenAIAPI"/> <see cref="Model"/> corresponding to the provided model name.
        /// <br /><see cref="Model.ChatGPTTurbo"/> or <see cref="Model.DavinciText"/> will be returned (as appropriate) if not set, or no match found.</returns>
        /// <exception cref="ArgumentNullException">
        /// On any exceptions, the default <see cref="OpenAIAPI"/> <see cref="Model.ChatGPTTurbo"/> will be set, and notify users of the error.
        /// </exception>
        public static Model GetOpenAI_Model(bool completionModel)
        {
            try
            {
                /// NOTE: new test for new 16k models - NOT GONNA WORK!!!
                //string newModelName = CheckNewModels(completionModel);
                //if (!string.IsNullOrEmpty(newModelName))
                //    return null;

                string modelName = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_Model") ?? (completionModel
                    ? DEFAULT_CHAT_MODEL_NAME
                    : DEFAULT_COMPLETION_MODEL_NAME);

                if (completionModel)
                {
                    if (!OpenAICompletionModels.Contains(modelName))
                    {
                        modelName = DEFAULT_COMPLETION_MODEL_NAME;
                    }
                }
                else
                {
                    if (!OpenAIChatGPTmodels.Contains(modelName))
                    {
                        modelName = DEFAULT_CHAT_MODEL_NAME;
                    }
                }

                if (OpenAImodels.TryGetValue(modelName, out Model model))
                {
                    // Use the "model" object corresponding to the name users provided
                    return model;
                }
            }
            catch (ArgumentNullException ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error in TryGetValue: {ex.Message}", "red");
                OpenAI_Plugin.VA_Proxy.WriteToLog("The default 'ChatGPTTurbo' model will be used instead.", "blank");
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: {ex.Message}", "red");
                OpenAI_Plugin.VA_Proxy.WriteToLog("The default 'ChatGPTTurbo' model will be used instead.", "blank");
            }

            // Handle the case where the user specified an unknown or invalid model name
            return completionModel ? DEFAULT_COMPLETION_MODEL : DEFAULT_CHAT_MODEL;
        }

        /// <summary>
        /// Checks for new 16k model requests, and returns the model name in string format.
        /// </summary>
        /// <param name="completionModel">Flag indicating whether the completion model is requested.</param>
        /// <returns>The 16k ChatGPT model requested, or an empty string if none.</returns>
        //public static string CheckNewModels(bool completionModel)
        //{
        //    string modelName = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_Model") ?? string.Empty;

        //    if (completionModel)
        //        return modelName;

        //    if (OpenAIChatGPTmodels.Contains(modelName))
        //        return modelName;

        //    if (modelName.Contains("16k"))
        //    {
        //        if (modelName.EndsWith("0613"))
        //        {
        //            modelName = "gpt-3.5-turbo-16k-0613";
        //        }
        //        else
        //        {
        //            modelName = "gpt-3.5-turbo-16k";
        //        }
        //    }

        //    return modelName;
        //}



        /// <summary>
        /// A method to check 'OpenAI_MaxTokens' integer variable and return a max tokens integer no greater than the max tokens of the 
        /// supplied <see cref="OpenAIAPI"/> <see cref="Model"/>.
        /// <br>See also <see cref="OpenAImodels"/></br>
        /// </summary>
        /// <param name="gptModel">The GPT model being used.</param>
        /// <returns>An integer of max tokens to use which is no greater than the <see cref="Model"/> allows, or 512 if unset/invalid.</returns>
        public static int GetValidMaxTokens(Model gptModel)
        {
            try
            {
                int maxTokens = OpenAI_Plugin.VA_Proxy.GetInt("OpenAI_MaxTokens") ?? 0;
                if (MaxTokensByModel.TryGetValue(gptModel, out int maxAllowed) && maxTokens > maxAllowed)
                {
                    return maxAllowed;
                }
                return maxTokens > 0 ? maxTokens : 512;
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: GetValidMaxTokens exception: {ex.Message}", "red");
                return 512;
            }
        }

    }
}
