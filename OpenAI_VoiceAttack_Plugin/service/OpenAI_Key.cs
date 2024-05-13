using System;
using System.IO;
using System.Linq;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing the method for obtaining the user's OpenAI API key used for API calls throughout
    /// this VoiceAttack plugin (and the OpenAI_NET companion app, for Whisper and Dall-E)
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
    public static class OpenAI_Key
    {
        /// <summary>
        /// A stand-in example of the end user's private API key to their OpenAI API account.<br />
        /// <br />See OpenAI documentation at <see href="https://platform.openai.com/docs/api-reference/authentication">openai.com/docs/api-reference</see>
        /// </summary>
        public static readonly string EXAMPLE_API_KEY = "sk-12345abcdefghijklmnopqrstuvwxyz";

        #region openai key properties
        /// <summary>
        /// The path to the folder containing the OpenAI API Key file.
        /// </summary>
        public static string DefaultKeyFileFolder { get; set; } = Configuration.DEFAULT_CONFIG_FOLDER;

        /// <summary>
        /// The name of the keyfile containing the OpenAI API Key.
        /// </summary>
        public static string DefaultKeyFilename { get; set; } = "key.openai";

        /// <summary>
        /// A boolean to indicate the method for applying the OpenAI Key to API calls.<br />
        /// True will load from file (see documentation), False will throw exceptions when no key is set.
        /// </summary>
        public static bool LoadKey { get; set; } = false;

        /// <summary>
        /// The end user's private API key for their OpenAI API account.<br />
        /// <br />See OpenAI documentation at <see href="https://platform.openai.com/docs/api-reference/authentication">openai.com/docs/api-reference</see>
        /// </summary>
        public static string ApiKey { get; set; }

        /// <summary>
        /// The end user's private API key Organization for their OpenAI API account. (OPTIONAL)<br />
        /// <br />Can be <see langword="null"/> - See OpenAI documentation at <see href="https://platform.openai.com/docs/api-reference/authentication">openai.com/docs/api-reference</see>
        /// </summary>
        public static string ApiOrg { get; set; }
        #endregion

        private static string KeyFilePath()
        {
            // Check for custom key filename
            string apiKeyFileName = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_API_Key_FileName") ?? DefaultKeyFilename;
            DefaultKeyFilename = apiKeyFileName != DefaultKeyFilename ? apiKeyFileName : DefaultKeyFilename;

            // Check for custom key file folder
            string apiKeyFolder = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_API_Key_FileFolder") ?? Configuration.DEFAULT_CONFIG_FOLDER;
            DefaultKeyFileFolder = System.IO.Directory.Exists(apiKeyFolder) && System.IO.File.Exists(System.IO.Path.Combine(apiKeyFolder, apiKeyFileName))
                ? apiKeyFolder
                : Configuration.DEFAULT_CONFIG_FOLDER;

            return System.IO.Path.Combine(DefaultKeyFileFolder, DefaultKeyFilename);
        }

        /// <summary>
        /// A method for loading the users OpenAI API key from file.
        /// </summary>
        /// <returns>The users private OpenAI API key.</returns>
        public static string LoadFromFile()
        {
            string keyFile = KeyFilePath();
            string apiKey = string.Empty;

            if (File.Exists(keyFile))
            {
                // Read the contents of the key file into the return string
                using (StreamReader reader = new StreamReader(keyFile))
                {
                    apiKey = reader.ReadToEnd().Trim();
                }

                if (!apiKey.Contains(Environment.NewLine) && !apiKey.Contains("="))
                {
                    return apiKey;
                }

                string[] apiKeyLines = apiKey.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                apiKey = apiKeyLines.FirstOrDefault(line => line.Contains("_KEY="))?.Split('=')[1];
            }
            return apiKey;
        }

        /// <summary>
        /// A method for saving the users OpenAI API Key to their key file.
        /// <br />
        /// <br />Default Path:
        /// <br />"%AppData%\Roaming\OpenAI_VoiceAttack_Plugin\key.openai"
        /// </summary>
        public static void SaveToFile(string key)
        {
            try
            {
                if (!key.StartsWith("OPENAI_API_KEY="))
                {
                    key = $"OPENAI_API_KEY={key}";
                }

                string keyFile = KeyFilePath();
                using (StreamWriter writer = new StreamWriter(keyFile, false))
                {
                    writer.Write(key);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLogFile($"Failure to save the OpenAI API Key to file! {ex.Message}");
            }
        }

        /// <summary>
        /// A method to delete the users OpenAI API Key from the file at default or custom location.
        /// </summary>
        /// <returns>True if key file was deleted or not present, false if otherwise.</returns>
        public static bool DeleteFromFile()
        {
            try
            {
                string keyFile = System.IO.Path.Combine(OpenAI_Key.DefaultKeyFileFolder, OpenAI_Key.DefaultKeyFilename);
                if (Directory.Exists(OpenAI_Key.DefaultKeyFileFolder) && File.Exists(keyFile))
                {
                    File.Delete(keyFile);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logging.WriteToLogFile($"Failure to delete the OpenAI API Key from file! {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieve the users OpenAI API Key from the 'OpenAI_API_Key' variable, or from file at default or custom location.<br />
        /// Path to file can be variable when user provides 'OpenAI_API_Key_FileName' and/or 'OpenAI_API_Key_FileFolder' text variables.
        /// </summary>
        /// <returns>The end user's private API key for their OpenAI API account.</returns>
        public static string GetOpenAI_Key()
        {
            string apiKey = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_API_Key") ?? (OpenAI_Key.ApiKey ?? string.Empty);
            
            if (!string.IsNullOrEmpty(apiKey) && apiKey != EXAMPLE_API_KEY)
            {
                return apiKey;
            }

            string filePath = KeyFilePath();
            if (string.IsNullOrEmpty(filePath))
            {
                throw new Exception("No API Key found!");
            }

            LoadKey = System.IO.File.Exists(filePath) || LoadKey;
            if (LoadKey)
            {
                apiKey = LoadFromFile() ?? string.Empty;
            }
            else
            {
                KeyForm keyForm = new KeyForm();
                keyForm.ShowKeyInputForm();
                apiKey = OpenAI_Key.ApiKey;
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("No API Key found!");
            }

            if (apiKey == EXAMPLE_API_KEY)
            {
                throw new Exception("Invalid API Key found! See documentation!");
            }

            return apiKey;
        }

    }
}
