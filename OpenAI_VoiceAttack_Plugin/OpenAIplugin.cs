using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// OpenAI API VoiceAttack Plugin - by SemlerPDX April2023
    /// <br>Copyright (C) 2023 Aaron Semler</br>
    /// <br><see href="https://github.com/SemlerPDX">github.com/SemlerPDX</see></br>
    /// <br><see href="https://veterans-gaming.com/semlerpdx-avcs">veterans-gaming.com/semlerpdx-avcs</see></br>
    /// <para>
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
    /// </summary>
    /// <features>
    ///   A plugin for VoiceAttack to provide access to the OpenAI API including most common functions:
    ///     -Can use dictation text or audio from VoiceAttack as input prompts.
    ///     -Can return responses specifically tailored for text-to-speech in VoiceAttack.
    ///     -Can perform completion tasks on provided input with options for selecting GPT model, etc.
    ///     -Can process audio via transcription or translation into (English) text using OpenAI Whisper.
    ///     -Can generate or work with images using the OpenAI Dall-E, returning a list of URL's.
    ///     -Can review provided input using OpenAI Moderation and return a list of any flagged categories.
    ///     -Can upload, list, or delete files for fine-tuning of the OpenAI GPT models, or other purposes.
    /// </features>
    public static class OpenAIplugin
    {
        /// <summary>
        /// The primary dynamic object which provides VoiceAttack attributes and functionality for this plugin call.
        /// </summary>
        public static dynamic VA_Proxy { get; set; }
        private static dynamic _proxy; //note - keeping a private reference to the vaProxy init object


        /// <summary>
        /// A global boolean property to indicate when the OpenAI API Key Form Menu is open.
        /// </summary>
        public static bool OpenAI_KeyFormOpen { get; set; } = false;


        #region boolean changed properties
        /// <summary>
        /// A global boolean property to indicate that a continuing ChatGPT session is currently waiting for permission from the profile to continue.<br />
        /// The "OpenAI_Waiting#" VoiceAttack boolean variable value is tied to this property in <see cref="BooleanChanged"/>
        /// </summary>
        public static bool? CHAT_WAITING { get; set; }

        /// <summary>
        /// A global boolean property to indicate that a continuing ChatGPT session is currently active.<br />
        /// The "OpenAI_Chatting#" VoiceAttack boolean variable value is tied to this property in <see cref="BooleanChanged"/>
        /// </summary>
        public static bool? CHAT_ACTIVE { get; set; }

        /// <summary>
        /// A global boolean property to indicate that ChatGPT is currently listening for user input.<br />
        /// The "OpenAI_Listening#" VoiceAttack boolean variable value is tied to this property in <see cref="BooleanChanged"/>
        /// </summary>
        public static bool? LISTEN_ACTIVE { get; set; }

        /// <summary>
        /// A global boolean property for outputting certain debug and testing messages to the VoiceAttack Event Log.<br />
        /// The "OpenAI_Debugging#" VoiceAttack boolean variable value is tied to this property in <see cref="BooleanChanged"/>
        /// </summary>
        public static bool? DEBUG_ACTIVE { get; set; }
        #endregion

        /// <summary>
        /// An event handler method monitoring VoiceAttack boolean variable changes during async OpenAI Plugin operations.
        /// </summary>
        /// <param name="Name">The name of the VoiceAttack boolean variable which changed.</param>
        /// <param name="bFrom">The former value of the VoiceAttack boolean.</param>
        /// <param name="bTo">The new value of the VoiceAttack boolean.</param>
        /// <param name="InternalID">The internal ID of the VoiceAttack boolean variable.</param>
        public static void BooleanChanged(String Name, Boolean? bFrom, Boolean? bTo, Guid? InternalID)
        {
            switch (Name)
            {
                case "OpenAI_ChatWaiting#":
                    CHAT_WAITING = bTo;
                    break;
                case "OpenAI_Chatting#":
                    CHAT_ACTIVE = bTo;
                    if (bTo != true)
                        VA_Proxy.SetBoolean("OpenAI_Listening#", bTo);
                    break;
                case "OpenAI_Listening#":
                    LISTEN_ACTIVE = bTo;
                    break;
                case "OpenAI_Debugging#":
                    DEBUG_ACTIVE = bTo;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Detecting first use by the need to create plugin config folder(s) on first time initialization by VoiceAttack.<br />
        /// <br />See also: <see cref="Configuration.DEFAULT_CONFIG_FOLDER"/>
        /// </summary>
        public static bool OpenAI_PluginFirstUse { get; set; } = false;
        /// <summary>
        /// Some things I would like to communicate to all users just once -SemlerPDX Apr2023
        /// </summary>
        private static void FirstTimeUseNotice()
        {
            try
            {
                System.Windows.MessageBox.Show(
                    "Thank you for checking out OpenAI Plugin for VoiceAttack!\n" +
                    "\n" +
                    "This pop-up will never appear again, just a brief reminder " +
                    "that this plugin is private to you and your own OpenAI account. " +
                    "Once this plugin is installed, VoiceAttack will never need to be " +
                    "run 'as admin' for this plugin to function!\n" +
                    "\n" +
                    "Your private OpenAI API Key must be provided in some way for your " +
                    "plugin calls to OpenAI, and those calls cost money.\n" +
                    "\n" +
                    "Keep it secret. Keep it safe.\n" +
                    "\n" +
                    "The extensive testing for this plugin included hundreds of calls " +
                    "using my own OpenAI API Key and has not risen above \n$1 (USD) in usage cost. " +
                    "I only want you all to know to keep your API Key private, delete " +
                    "and replace it if you are concerned about misuse, and review your " +
                    "usage frequently at:\n https://platform.openai.com/account/usage\n" +
                    "\n    -SemlerPDX    (Apr2023)\n" +
                    "\n" +
                    "\n" +
                    VA_DisplayInfo().Replace("OpenAI API Plugin - ", "OpenAI API Plugin\n").Replace("{NEWLINE}", "\n"),
                    VA_DisplayName(),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                Logging.WriteToLogFile($"Error during OpenAI Plugin First Time Use Notice: {ex.Message}");
            }
        }

        /// <summary>
        /// Method to return a string displayed in dropdowns as well as in the log file to indicate the name of this plugin.
        /// </summary>
        /// <returns>A string containing the display name for this plugin.</returns>
        public static string VA_DisplayName()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return $"OpenAI API for VoiceAttack v{assembly.GetName().Version}";
        }

        /// <summary>
        /// Method to return extended info that we might want to give to the user.
        /// </summary>
        /// <returns>A string containing extended plugin info.</returns>
        public static string VA_DisplayInfo()
        {
            return "OpenAI API Plugin - True Artificial Intelligence for VoiceAttack Profiles{NEWLINE}Freeware [GPLv3] Copyright © 2023 SemlerPDX  veterans-gaming.com/avcs";
        }

        /// <summary>
        /// Method to return a unique ID generated by the author so VoiceAttack can identify and use the plugin.
        /// </summary>
        /// <returns>The GUID of this plugin.</returns>
        public static Guid VA_Id()
        {
            return new Guid("{4F917206-3E0B-D23A-CC65-8BB97DB1A7FA}");
        }

        /// <summary>
        /// A bool which will be true if commands were stopped
        /// </summary>
        public static Boolean _stopVariableToMonitor = false;

        /// <summary>
        /// Method providing a function which is called from VoiceAttack when the 'stop all commands'
        /// button is pressed, or a 'stop all commands' action is called.
        /// </summary>
        public static void VA_StopCommand()
        {
            _stopVariableToMonitor = true;
        }

        /// <summary>
        /// Method containing initialization operations. Called once on VoiceAttack load (asynchronously).
        /// </summary>
        /// <param name="vaProxy">The dynamic object which provides VoiceAttack attributes and functionality for this plugin.</param>
        public static void VA_Init1(dynamic vaProxy)
        {
            VA_Proxy = vaProxy; // must provide value at init and invoke
            _proxy = vaProxy; //note - keeping a private reference to the vaProxy init object
            bool isFirstRun = false;
            try
            {
                _proxy.BooleanVariableChanged += new Action<String, Boolean?, Boolean?, Guid?>(BooleanChanged);

                _proxy.WriteToLog(VA_DisplayInfo(), "blue");

                isFirstRun = Configuration.DeleteOldDictationAudio();

                if (isFirstRun && Configuration.CheckSharedAssemblies()) { throw new Exception("Missing required shared assemblies! VoiceAttack must be restarted to load these assemblies!"); }

                if (!OpenAI_PluginFirstUse)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _proxy.SetText("OpenAI_Plugin_Version", assembly.GetName().Version.ToString());

                    _proxy.SetBoolean("OpenAI_Plugin_Initialized", OpenAI_NET.LaunchOpenAI_NET());
                    try
                    {
                        // This is technically the second run but first for config folder creation
                        Configuration.CreateConfigFolders(isFirstRun);
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteToLog_Long($"OpenAI Plugin Error: Failure to create required Config Folders in AppData Roaming! {ex.Message}", "red");
                    }
                    Logging.SetErrorLogPath();

                    // Simple notification message if update found
                    if (Updates.UpdateCheck())
                    {
                        Updates.UpdateMessage();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: Failure to initialize the plugin! {ex.Message}", "red");
            }
            finally
            {
                if (!OpenAI_PluginFirstUse)
                {
                    OpenAI_PluginFirstUse = isFirstRun;
                    if (OpenAI_PluginFirstUse)
                    {
                        FirstTimeUseNotice();
                    }
                }
            }
        }


        /// <summary>
        /// Method providing a function which is called from VoiceAttack when it is closing (normally).
        /// </summary>
        /// <param name="vaProxy">The dynamic object which provides VoiceAttack attributes and functionality for this plugin.</param>
        public static void VA_Exit1(dynamic vaProxy)
        {
            _stopVariableToMonitor = true;
            CHAT_ACTIVE = false;
            LISTEN_ACTIVE = false;
            CHAT_WAITING = false;
            OpenAI_NET.KillOpenAI_NET();
        }


        private static ManualResetEventSlim _asyncOperationCompleted = new ManualResetEventSlim(false);

        /// <summary>
        /// The method called when VoiceAttack encounters an, 'Execute External Plugin Function' action,
        /// and this plugin is indicated to be called.
        /// </summary>
        /// <param name="vaProxy">The dynamic object which provides VoiceAttack attributes and functionality for this plugin.</param>
        public static void VA_Invoke1(dynamic vaProxy)
        {
            VA_Proxy = vaProxy;
            _stopVariableToMonitor = false;

            try
            {
                if (VA_Proxy.GetBoolean("OpenAI_Plugin_Initialized") != true)
                    throw new Exception("The OpenAI Plugin has not properly initialized! See documentation!");


                // Exit this call here if the OpenAI API Key is invalid, unless the Context is to set it via KeyForm
                if (vaProxy.Context.ToLower() != "keyform")
                {
                    OpenAI_Key.API_KEY = OpenAI_Key.GetOpenAI_Key();
                    OpenAI_Key.API_ORG = VA_Proxy.GetText("OpenAI_API_Org") ?? null;
                }

                // Ensure the companion app for Whisper and Dall-E function is running
                if (!OpenAI_NET.IsRunningOpenAI_NET())
                    OpenAI_NET.LaunchOpenAI_NET();

                // Call async plugin context processing operation on a new thread
                var task = Task.Run(() =>
                {
                    PluginContext(vaProxy);
                });

                // Wait for the async operation to complete
                _asyncOperationCompleted.Wait();

                // Reset the ManualResetEventSlim for the next async operation
                _asyncOperationCompleted.Reset();

                // Attempt to clean up any dictation audio files created in this session
                Configuration.DeleteOldDictationAudio();
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Errors: {ex.Message}", "red");
            }
        }

        private static async void PluginContext(dynamic vaProxy)
        {
            string context = vaProxy.Context.ToLower();

            VA_Proxy.SetBoolean("OpenAI_Error", false);
            VA_Proxy.SetText("OpenAI_TTS_Response", String.Empty);
            VA_Proxy.SetText("OpenAI_Response", String.Empty);

            // Get out if a ChatGPT session is already engaged when another ChatGPT context is called (excluding raw)
            if ((CHAT_ACTIVE == true || CHAT_WAITING == true) && context.Contains("chatgpt") && !context.Contains("raw"))
            {
                // Dev/Testing Flow Output
                if (DEBUG_ACTIVE == true)
                    Logging.WriteToLog_Long("OpenAI Plugin: Call Finally Ended - a ChatGPT session is already active when a new one was called", "purple");

                // Signal that this async operation is complete
                _asyncOperationCompleted.Set();
                return;
            }

            try
            {
                switch (context)
                {
                    case "completion":
                        await Completion.CompleteText();
                        break;
                    case "chatgpt":
                        await ChatGPT.Chat();
                        break;
                    case "chatgpt.session":
                        await ChatGPT.Chat(true);
                        break;
                    case "chatgpt.raw":
                        await ChatGPT.Raw();
                        break;
                    case "chatgpt.raw.session":
                        await ChatGPT.Raw(true);
                        break;

                    case "whisper":
                        Whisper.ProcessAudio();
                        break;
                    case "whisper.transcribe":
                        Whisper.ProcessAudio();
                        break;
                    case "whisper.chatgpt":
                        await ChatGPT.Chat("transcribe");
                        break;
                    case "whisper.transcribe.chatgpt":
                        await ChatGPT.Chat("transcribe");
                        break;
                    case "whisper.chatgpt.session":
                        await ChatGPT.Chat(true, "transcribe");
                        break;
                    case "whisper.transcribe.chatgpt.session":
                        await ChatGPT.Chat(true, "transcribe");
                        break;

                    case "whisper.translate":
                        Whisper.ProcessAudio("translate");
                        break;
                    case "whisper.translate.chatgpt":
                        await ChatGPT.Chat("translate");
                        break;
                    case "whisper.translate.chatgpt.session":
                        await ChatGPT.Chat(true, "translate");
                        break;

                    case "dalle":
                        await DallE.Generation();
                        break;
                    case "dalle.generation":
                        await DallE.Generation();
                        break;
                    case "dalle.editing":
                        await DallE.Editing();
                        break;
                    case "dalle.editing.bytes":
                        await DallE.Editing(true);
                        break;
                    case "dalle.variation":
                        await DallE.Variation();
                        break;
                    case "dalle.variation.bytes":
                        await DallE.Variation(true);
                        break;

                    case "moderation":
                        await Moderation.Check();
                        break;
                    case "moderation.check":
                        await Moderation.Check();
                        break;
                    case "moderation.explain":
                        await Moderation.Explain();
                        break;

                    case "file":
                        await Files.List();
                        break;
                    case "file.upload":
                        await Files.Upload();
                        break;
                    case "file.list":
                        await Files.List();
                        break;
                    case "file.delete":
                        await Files.Delete();
                        break;

                    case "embedding":
                        await Embedding.Embed();
                        break;

                    case "keyform":
                        KeyForm keyForm = new KeyForm();
                        keyForm.ShowKeyInputForm();
                        keyForm = null;
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                // End Dictation Mode if ON
                if (VA_Proxy.Dictation.IsOn())
                    VA_Proxy.Dictation.Stop();

                // Dev/Testing Errors Output in VoiceAttack Event Log
                if (DEBUG_ACTIVE == true)
                    Logging.WriteToLog_Long(ex.ToString(), "red");

                // Send the message to the errors log
                Logging.WriteToLogFile(ex.ToString());

                // Communicate to VoiceAttack profile(s) that an error occurred
                VA_Proxy.SetBoolean("OpenAI_Error", true);
            }
            finally
            {
                // Reset Plugin Systems Variables
                VA_Proxy.SetText("OpenAI_AudioFile", null);
                VA_Proxy.SetText("OpenAI_ImagePath", null);
                VA_Proxy.SetText("OpenAI_UserInput", null);

                if (context.EndsWith("session"))
                    VA_Proxy.SetBoolean("OpenAI_Chatting#", false);

                // Dev/Testing Flow Output
                if (DEBUG_ACTIVE == true)
                    VA_Proxy.WriteToLog("OpenAI Plugin: Call Finally Ended", "purple");

                // Signal that this async operation is complete
                _asyncOperationCompleted.Set();
            }
        }

    }
}
