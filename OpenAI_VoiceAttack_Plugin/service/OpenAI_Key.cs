using System;
using System.IO;

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
        #region openai key properties
        /// <summary>
        /// The path to the folder containing the OpenAI API Key file.
        /// </summary>
        public static string DEFAULT_KEY_FILEFOLDER { get; set; } = Configuration.DEFAULT_CONFIG_FOLDER;

        /// <summary>
        /// The name of the keyfile containing the OpenAI API Key.
        /// </summary>
        public static string DEFAULT_KEY_FILENAME { get; set; } = "key.openai";

        /// <summary>
        /// A boolean to indicate the method for applying the OpenAI Key to API calls.<br />
        /// True will load from file (see documentation), False will throw exceptions when no key is set.
        /// </summary>
        public static bool LOAD_KEY { get; set; } = false;

        /// <summary>
        /// The end user's private API key for their OpenAI API account.<br />
        /// <br />See OpenAI documentation at <see href="https://platform.openai.com/docs/api-reference/authentication">openai.com/docs/api-reference</see>
        /// </summary>
        public static string API_KEY { get; set; }

        /// <summary>
        /// A stand-in example of the end user's private API key to their OpenAI API account.<br />
        /// <br />See OpenAI documentation at <see href="https://platform.openai.com/docs/api-reference/authentication">openai.com/docs/api-reference</see>
        /// </summary>
        public static readonly string EXAMPLE_API_KEY = "sk-12345abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// The end user's private API key Organization for their OpenAI API account. (OPTIONAL)<br />
        /// <br />Can be <see langword="null"/> - See OpenAI documentation at <see href="https://platform.openai.com/docs/api-reference/authentication">openai.com/docs/api-reference</see>
        /// </summary>
        public static string API_ORG { get; set; }
        #endregion

        private static string KeyFilePath()
        {
            try
            {
                // Check for custom key filename
                string apiKeyFileName = OpenAIplugin.VA_Proxy.GetText("OpenAI_API_Key_FileName") ?? DEFAULT_KEY_FILENAME;
                DEFAULT_KEY_FILENAME = apiKeyFileName != DEFAULT_KEY_FILENAME ? apiKeyFileName : DEFAULT_KEY_FILENAME;

                // Check for custom key file folder
                string apiKeyFolder = OpenAIplugin.VA_Proxy.GetText("OpenAI_API_Key_FileFolder") ?? Configuration.DEFAULT_CONFIG_FOLDER;
                DEFAULT_KEY_FILEFOLDER = System.IO.Directory.Exists(apiKeyFolder) && System.IO.File.Exists(System.IO.Path.Combine(apiKeyFolder, apiKeyFileName))
                    ? apiKeyFolder
                    : Configuration.DEFAULT_CONFIG_FOLDER;

                return System.IO.Path.Combine(DEFAULT_KEY_FILEFOLDER, DEFAULT_KEY_FILENAME);
            }
            catch
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// A method for loading the users OpenAI API key from file.
        /// </summary>
        /// <returns>The users private OpenAI API key.</returns>
        public static string LoadFromFile()
        {
            try
            {
                string keyFile = KeyFilePath();
                string apiKey = String.Empty;
                if (File.Exists(keyFile))
                {
                    // Read the contents of the key file into the return string
                    using (StreamReader reader = new StreamReader(keyFile))
                    {
                        string key = reader.ReadToEnd().Trim();
                        apiKey = key;
                    }

                    if (apiKey.Contains(Environment.NewLine))
                    {
                        string[] apiKeyLines = apiKey.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                        foreach (string line in apiKeyLines)
                        {
                            if (line.Contains("_KEY="))
                            {
                                string[] apiKeyVar = line.Split('=');
                                apiKey = apiKeyVar[1];
                            }
                        }
                    }
                    else
                    {
                        // Parse out the key (default format OPENAI_API_KEY=key)
                        if (apiKey.Contains("_KEY="))
                        {
                            string[] apiKeyVar = apiKey.Split('=');
                            apiKey = apiKeyVar[1];
                        }
                    }

                }
                return apiKey;
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: Failure to read the OpenAI API Key from file! {ex.Message}", "red");
                return String.Empty;
            }
        }

        /// <summary>
        /// A method for saving the users OpenAI API Key to their key file.
        /// <br />
        /// <br />Default Path:
        /// <br />"%AppData%\Roaming\OpenAI_VoiceAttack_Plugin\key.openai"
        /// </summary>
        public static bool SaveToFile(string key)
        {
            try
            {
                if (!key.StartsWith("OPENAI_API_KEY="))
                    key = $"OPENAI_API_KEY={key}";

                string keyFile = KeyFilePath();
                using (StreamWriter writer = new StreamWriter(keyFile, false))
                {
                    writer.Write(key);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: Failure to write to OpenAI API Key to file! {ex.Message}", "red");
                return false;
            }
        }

        /// <summary>
        /// A method to delete the old dictation audio files after use, because the action which
        /// writes them cannot overwrite, and a new audio file is created each time, requiring cleanup.
        /// </summary>
        public static bool DeleteFromFile()
        {
            try
            {
                string keyFile = System.IO.Path.Combine(OpenAI_Key.DEFAULT_KEY_FILEFOLDER, OpenAI_Key.DEFAULT_KEY_FILENAME);
                if (Directory.Exists(OpenAI_Key.DEFAULT_KEY_FILEFOLDER))
                {
                    if (File.Exists(keyFile))
                    {
                        File.Delete(keyFile);

                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: Failure to delete the OpenAI API Key from file! {ex.Message}", "red");
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
            try
            {
                // Check first for already provided (valid) key
                string apiKey = OpenAIplugin.VA_Proxy.GetText("OpenAI_API_Key") ?? (OpenAI_Key.API_KEY ?? String.Empty);
                if (String.IsNullOrEmpty(apiKey) || apiKey == EXAMPLE_API_KEY)
                {

                    string filePath = KeyFilePath();
                    if (String.IsNullOrEmpty(filePath)) { throw new Exception("No API Key found!"); }

                    LOAD_KEY = System.IO.File.Exists(filePath) || LOAD_KEY;
                    if (LOAD_KEY)
                    {
                        apiKey = LoadFromFile() ?? String.Empty;
                    }
                    else
                    {
                        KeyForm keyForm = new KeyForm();
                        keyForm.ShowKeyInputForm();
                        keyForm = null;
                        apiKey = OpenAI_Key.API_KEY;
                    }
                }

                if (String.IsNullOrEmpty(apiKey)) { throw new Exception("No API Key found!"); }
                if (apiKey == EXAMPLE_API_KEY) { throw new Exception("Invalid API Key found! See documentation!"); }

                return apiKey;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

    }
}
