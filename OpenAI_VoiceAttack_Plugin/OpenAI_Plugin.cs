using System;
using System.Collections.Generic;
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
    public static class OpenAI_Plugin
    {
        /// <summary>
        /// The primary dynamic object which provides VoiceAttack attributes and functionality for this plugin call.
        /// </summary>
        public static dynamic VA_Proxy { get; set; }
        // Must keep a private reference to the vaProxy init object.
        private static dynamic _proxy;


        /// <summary>
        /// A global boolean property to indicate when the OpenAI API Key Form Menu is open.
        /// </summary>
        public static bool OpenAiKeyFormOpen { get; set; } = false;


        #region boolean variable changed properties
        /// <summary>
        /// A global boolean property to indicate that a continuing ChatGPT session is currently waiting for permission from the profile to continue.<br />
        /// The "OpenAI_Waiting#" VoiceAttack boolean variable value is tied to this property in <see cref="BooleanChanged"/>
        /// </summary>
        public static bool? ChatWaiting { get; set; }

        /// <summary>
        /// A global boolean property to indicate that a continuing ChatGPT session is currently active.<br />
        /// The "OpenAI_Chatting#" VoiceAttack boolean variable value is tied to this property in <see cref="BooleanChanged"/>
        /// </summary>
        public static bool? ChatActive { get; set; }

        /// <summary>
        /// A global boolean property to indicate that ChatGPT is currently listening for user input.<br />
        /// The "OpenAI_Listening#" VoiceAttack boolean variable value is tied to this property in <see cref="BooleanChanged"/>
        /// </summary>
        public static bool? ListenActive { get; set; }

        /// <summary>
        /// A global boolean property for outputting certain debug and testing messages to the VoiceAttack Event Log.<br />
        /// The "OpenAI_Debugging#" VoiceAttack boolean variable value is tied to this property in <see cref="BooleanChanged"/>
        /// </summary>
        public static bool? DebugActive { get; set; }
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
                    ChatWaiting = bTo;
                    break;
                case "OpenAI_Chatting#":
                    ChatActive = bTo;
                    if (bTo != true)
                    {
                        VA_Proxy.SetBoolean("OpenAI_Listening#", bTo);
                    }
                    break;
                case "OpenAI_Listening#":
                    ListenActive = bTo;
                    break;
                case "OpenAI_Debugging#":
                    DebugActive = bTo;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Detecting first use by the need to create plugin config folder(s) on first time initialization by VoiceAttack.<br />
        /// <br />See also: <see cref="Configuration.DEFAULT_CONFIG_FOLDER"/>
        /// </summary>
        public static bool OpenAiPluginFirstUse { get; set; } = false;
        /// <summary>
        /// Some things I would like to communicate to all users just once -SemlerPDX Apr2023
        /// </summary>
        private static void FirstTimeUseNotice()
        {
            if (!OpenAiPluginFirstUse)
            {
                return;
            }

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

                isFirstRun = !Dictation.DeletedOldDictationAudio();

                if (Configuration.SharedAssembliesMoved())
                {
                    throw new Exception("Missing required shared assemblies! VoiceAttack must be restarted to load these assemblies!");
                }

                if (OpenAiPluginFirstUse)
                {
                    return;
                }

                Assembly assembly = Assembly.GetExecutingAssembly();
                _proxy.SetText("OpenAI_Plugin_Version", assembly.GetName().Version.ToString());

                OpenAI_NET.LaunchOpenAI_NET();

                // This is technically the second run but first for config folder creation
                _proxy.SetBoolean("OpenAI_Plugin_Initialized", Configuration.CreateConfigFolders(isFirstRun));

                Logging.SetErrorLogPath();

                if (Updates.UpdateCheck())
                {
                    Updates.UpdateMessage();
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLog_Long($"OpenAI Plugin Error: Failure to initialize the plugin! {ex.Message}", "red");
            }
            finally
            {
                if (!OpenAiPluginFirstUse)
                {
                    OpenAiPluginFirstUse = isFirstRun;
                    FirstTimeUseNotice();
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
            ChatActive = false;
            ListenActive = false;
            ChatWaiting = false;
            OpenAI_NET.KillOpenAI_NET();
        }


        private static ManualResetEventSlim _asyncOperationCompleted = new ManualResetEventSlim(false);

        /// <summary>
        /// The method called when VoiceAttack encounters an 'Execute External Plugin Function' action,
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
                {
                    throw new Exception("The OpenAI Plugin has not properly initialized! See documentation!");
                }

                // If key is not yet set, this will force open the keyform - so bypass if context is keyform
                if (vaProxy.Context.ToLower() != "keyform")
                {
                    OpenAI_Key.ApiKey = OpenAI_Key.GetOpenAI_Key();
                    OpenAI_Key.ApiOrg = VA_Proxy.GetText("OpenAI_API_Org") ?? null;
                }

                // Must ensure the companion app for Whisper and Dall-E function is running
                if (!OpenAI_NET.IsRunningOpenAI_NET())
                {
                    OpenAI_NET.LaunchOpenAI_NET();
                }

                // Call async plugin context processing operation on a new thread
                var task = Task.Run(() =>
                {
                    PluginContext(vaProxy);
                });

                _asyncOperationCompleted.Wait();
                _asyncOperationCompleted.Reset();

                // Attempt to clean up any dictation audio files created in this session
                Dictation.DeletedOldDictationAudio();
            }
            catch (Exception ex)
            {
                if (VA_Proxy.Dictation.IsOn())
                {
                    VA_Proxy.Dictation.Stop();
                }

                if (DebugActive == true)
                {
                    Logging.WriteToLog_Long(ex.ToString(), "red");
                }

                Logging.WriteToLogFile(ex.ToString());
                Logging.WriteToLog_Long("OpenAI Plugin Error: See openai_errors.log for complete details", "red");

                // Must Communicate to VoiceAttack profile(s) that an error occurred here
                VA_Proxy.SetBoolean("OpenAI_Error", true);
            }
            finally
            {
                // Must Reset these Plugin Systems Variables here
                VA_Proxy.SetText("OpenAI_AudioFile", null);
                VA_Proxy.SetText("OpenAI_ImagePath", null);
                VA_Proxy.SetText("OpenAI_UserInput", null);

                if (DebugActive == true)
                {
                    VA_Proxy.WriteToLog("OpenAI Plugin: Call Finally Ended", "purple");
                }
            }
        }

        private static readonly Dictionary<string, Func<Task>> ContextActions = new Dictionary<string, Func<Task>>
        {
            { "completion", () => Completion.CompleteText() },
            { "chatgpt", () => ChatGPT.Chat() },
            { "chatgpt.session", () => ChatGPT.Chat(true) },
            { "chatgpt.raw", () => ChatGPT.Raw() },
            { "chatgpt.raw.session", () => ChatGPT.Raw(true) },
            { "whisper", () => Whisper.ProcessAudio() },
            { "whisper.transcribe", () => Whisper.ProcessAudio() },
            { "whisper.chatgpt", () => ChatGPT.Chat("transcribe") },
            { "whisper.transcribe.chatgpt", () => ChatGPT.Chat("transcribe") },
            { "whisper.chatgpt.session", () => ChatGPT.Chat(true, "transcribe") },
            { "whisper.transcribe.chatgpt.session", () => ChatGPT.Chat(true, "transcribe") },
            { "whisper.translate", () => Whisper.ProcessAudio("translate") },
            { "whisper.translate.chatgpt", () => ChatGPT.Chat("translate") },
            { "whisper.translate.chatgpt.session", () => ChatGPT.Chat(true, "translate") },
            { "dalle", () => DallE.Generation() },
            { "dalle.generation", () => DallE.Generation() },
            { "dalle.editing", () => DallE.Editing() },
            { "dalle.editing.bytes", () => DallE.Editing(true) },
            { "dalle.variation", () => DallE.Variation() },
            { "dalle.variation.bytes", () => DallE.Variation(true) },
            { "moderation", () => Moderation.Check() },
            { "moderation.check", () => Moderation.Check() },
            { "moderation.explain", () => Moderation.Explain() },
            { "file", () => Files.List() },
            { "file.upload", () => Files.Upload() },
            { "file.list", () => Files.List() },
            { "file.delete", () => Files.Delete() },
            { "embedding", () => Embedding.GetVectors() }
        };

        private static async void PluginContext(dynamic vaProxy)
        {
            string context = vaProxy.Context.ToLower();

            VA_Proxy.SetBoolean("OpenAI_Error", false);
            VA_Proxy.SetText("OpenAI_TTS_Response", string.Empty);
            VA_Proxy.SetText("OpenAI_Response", string.Empty);

            // Must exit if a ChatGPT Session is already engaged when another ChatGPT context is called (excluding raw)
            bool isChatGptSessionActive = ChatActive == true || ChatWaiting == true;
            bool isChatGptContext = context.Contains("chatgpt");
            bool isNotRawContext = !context.Contains("raw");

            if (isChatGptSessionActive && isChatGptContext && isNotRawContext)
            {
                if (DebugActive == true)
                {
                    Logging.WriteToLog_Long("OpenAI Plugin Error: A ChatGPT Session is already active when a new one was called.", "purple");
                }

                _asyncOperationCompleted.Set();
                return;
            }

            if (ContextActions.ContainsKey(context))
            {
                await ContextActions[context]();
            }

            if (context == "keyform")
            {
                KeyForm keyForm = new KeyForm();
                keyForm.ShowKeyInputForm();
            }

            if (context.EndsWith("session"))
            {
                VA_Proxy.SetBoolean("OpenAI_Chatting#", false);
            }

            _asyncOperationCompleted.Set();
        }

    }
}
