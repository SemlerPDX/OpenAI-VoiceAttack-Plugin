using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Embedding;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Shapes;
using static OpenAI_VoiceAttack_Plugin.Embedding;
//using static OpenAI_VoiceAttack_Plugin.Embedding;
using static System.Windows.Forms.AxHost;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing methods for handling ChatGPT requests, optionally gathering input and/or 
    /// speaking responses with Text-to-Speech in VoiceAttack, using the OpenAI API via the <see cref="OpenAIAPI"/> library.
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
    public static class ChatGPT
    {
        private static readonly string DEFAULT_REPROCESS_FLAG = "((reprocess))";
        private static readonly string DEFAULT_SPEECH_COMMAND = "((OpenAI Speech))";
        private static readonly string DEFAULT_SOUND_COMMAND = "((OpenAI Speech))";
        private static readonly string DEFAULT_USER_INPUT_EXAMPLE = "Is a duck billed platypus venomous?";
        private static readonly string DEFAULT_CHATBOT_OUTPUT_EXAMPLE = "Yes, but only the male platypus has venomous spurs on its hind legs. " +
                                                            "These spurs are used for self-defense and can deliver a potent venom that causes extreme pain and swelling.";

        private static readonly string DEFAULT_SYSTEM_PROMPT = "As ChatGPT in TTS mode, I'll provide brief and accurate responses to your input suitable for text-to-speech use. " +
                                                            "I'll round any decimals to one place for simplicity. " +
                                                            "To ensure a faster response, I'll limit the length of my answers, but I'll try to be as complete as possible. " +
                                                            "I won't provide code examples or any other formatted text that won't work well with text-to-speech.";

        //private static bool GetInputTimeout { get; set; } = false;



        /// <summary>
        /// Get the name of an existing command from the supplied input VoiceAttack text variable name,
        /// or use the default command name.
        /// <br /><br />
        /// Will always throw an exception if the command does not exist.
        /// </summary>
        /// <param name="commandVariable">An optional VoiceAttack text variable name which contains an alternate command name to execute.</param>
        /// <param name="defaultCommandName">The default command name stored in the plugin codebase.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Thrown when no valid command exists to return the command name of.</exception>
        private static string GetExistingCommand(string commandVariable, string defaultCommandName) { return GetExistingCommand(commandVariable, defaultCommandName, true); }
        /// <summary>
        /// Get the name of an existing command from the supplied input VoiceAttack text variable name,
        /// or use the default command name.
        /// <br /><br />
        /// Will optionally throw an exception if the command does not exist when the <paramref name="throwExceptions"/> parameter is set to <see langword="true"/>.
        /// </summary>
        /// <param name="commandVariable">An optional VoiceAttack text variable name which contains an alternate command name to execute.</param>
        /// <param name="defaultCommandName">The default command name stored in the plugin codebase.</param>
        /// <param name="throwExceptions">A boolean to indicate if this method should throw exepctions when no valid command exists in VoiceAttack.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Thrown when no valid command exists to return the command name of, if allowed to throw Exceptions.</exception>
        private static string GetExistingCommand(string commandVariable, string defaultCommandName, bool throwExceptions)
        {
            // Ensure this command exists in VoiceAttack profile:
            string thisCommand = OpenAI_Plugin.VA_Proxy.GetText(commandVariable) ?? defaultCommandName;
            if (!OpenAI_Plugin.VA_Proxy.Command.Exists(thisCommand))
            {
                if (OpenAI_Plugin.VA_Proxy.Command.Exists(defaultCommandName))
                {
                    thisCommand = defaultCommandName;
                }
                else
                {
                    if (throwExceptions)
                        throw new Exception($"No valid '{defaultCommandName}' command available for ChatGPT plugin call!");
                }
            }
            return thisCommand;
        }

        /// <summary>
        /// Provide user feedback in the form of speech or sound by executing an existing command
        /// in a VoiceAttack profile, and do not wait for it to complete.
        /// </summary>
        /// <param name="feedbackVariable">The VoiceAttack text variable containing a speech phrase or audio file path.</param>
        public static void ProvideFeedback(string feedbackVariable) { ProvideFeedback(feedbackVariable, false); }
        /// <summary>
        /// Provide user feedback in the form of speech or sound by executing an existing command
        /// in a VoiceAttack profile, and wait for it to complete before continuing.
        /// </summary>
        /// <param name="feedbackVariable">The VoiceAttack text variable containing a speech phrase or audio file path.</param>
        /// <param name="waitForComplete">Wait for the VoiceAttack command to complete when <see langword="true"/>, or not if <see langword="false"/>.</param>
        public static void ProvideFeedback(string feedbackVariable, bool waitForComplete)
        {
            // Optional speech/sound before capturing input through Dictation text and audio capture,
            // OR before processing chat session input through OpenAI API (which can be slow!)
            string feedbackValue = OpenAI_Plugin.VA_Proxy.GetText(feedbackVariable) ?? string.Empty;
            bool isAudioFile = false;

            if (!string.IsNullOrEmpty(feedbackValue))
            {
                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_TTS_Response", feedbackValue);

                try
                {
                    if (File.Exists(feedbackValue))
                    {
                        isAudioFile = true;
                    }
                }
                catch
                {
                    // ...is not file path
                }

                if (isAudioFile)
                {
                    OpenAI_Plugin.VA_Proxy.Command.Execute(GetExistingCommand("OpenAI_Command_Sound", DEFAULT_SOUND_COMMAND), waitForComplete);
                }
                else
                {
                    OpenAI_Plugin.VA_Proxy.Command.Execute(GetExistingCommand("OpenAI_Command_Speech", DEFAULT_SPEECH_COMMAND), waitForComplete);
                }
            }
        }

        /// <summary>
        /// Execute an existing command from the supplied input VoiceAttack text variable name,
        /// to be used as an External Responder command or function following a ChatGPT response.
        /// </summary>
        /// <exception cref="Exception">Thrown when told to execute a command which does not exist in the active profile or any referenced profile.</exception>
        private static void SendToExternalCommand(bool canSend)
        {
            if (!canSend)
                return;

            // Ensure External ChatGPT Session return processing command exists in VoiceAttack profile:
            string externalCommand = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_Command_ExternalResponder") ?? "";

            if (!string.IsNullOrEmpty(externalCommand) && OpenAI_Plugin.VA_Proxy.Command.Exists(externalCommand))
            {
                OpenAI_Plugin.VA_Proxy.Command.Execute(externalCommand, true); // waits here for it to complete...
            }
            else
            {
                throw new Exception($"'OpenAI_ExternalResponder' is TRUE but NO valid 'OpenAI_Command_ExternalResponder' command available to handle chat session returns!");
            }
        }

        /// <summary>
        /// Enter a sleep loop for a continuing ChatGPT session. Depending on user options,
        /// this may or may not respect the 'Stop all commands' button or action in VoiceAttack.
        /// <br /><br />
        /// This 'wait to continue' phase will naturally exit when the "OpenAI_ChatWaiting#" boolean becomes <see langword="false"/>.
        /// </summary>
        private static void WaitForExternalContinue(bool canWait)
        {
            if (!canWait)
                return;

            int intervalMs = 100; // 100 milliseconds seems decent enough, can change here without concern of effect below

            OpenAI_Plugin._stopVariableToMonitor = false;
            OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_ChatWaiting#", true);

            // Check if wait should respect VoiceAttack stop button (inverted bool)
            bool? allowStopExit = !OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_ExternalContinue_Unstoppable") ?? null;
            allowStopExit = allowStopExit ?? true; // default will respect the stop button and end the plugin call

            int index = 0;
            int writeInterval = 5000 / intervalMs; // calculate the 5 second interval for debugging output
            do
            {
                Thread.Sleep(intervalMs);

                // Write 'still waiting' message every 5 seconds if debugging
                index++;
                if (OpenAI_Plugin.DebugActive == true && index % writeInterval == 0)
                    OpenAI_Plugin.VA_Proxy.WriteToLog("OpenAI ChatGPT is still waiting for profile permission to continue...", "yellow");

                // Allow for Stop Button exit
                if (allowStopExit == true && OpenAI_Plugin._stopVariableToMonitor)
                    break;
            }
            while (OpenAI_Plugin.ChatWaiting == true);

            // Reset the stop flag only for long running waits in unstoppable state
            if (allowStopExit != true)
                OpenAI_Plugin._stopVariableToMonitor = false;

            // Reset the Waiting boolean after stop exits or otherwise
            if (OpenAI_Plugin.ChatWaiting == true)
                OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_ChatWaiting#", false);

        }

        /// <summary>
        /// A post-process method on ChatGPT responses to (hopefully) parse out any hyperlinks
        /// from potential text-to-speech responses.
        /// </summary>
        /// <param name="response">The ChatGPT response with any hyperlinks culled out to a separate return text variable in VoiceAttack.</param>
        /// <returns>The input response with hyperlinks culled out.<br />
        /// Also sets the 'OpenAI_ResponseLinks' text variable to a semicolon deliniated list of any hyperlinks culled.</returns>
        private static string ParseHyperlinks(string response)
        {
            List<string> responseBuilder = new List<string>();
            List<string> linksBuilder = new List<string>();

            // ChatGPT returns multiline responses separted by "\n" only
            string[] responses = response.Split(new string[] { "\n" }, StringSplitOptions.None); // we must allow line breaks
            foreach (string line in responses)
            {
                string[] words = line.Split(' ');
                string newLine = string.Empty;
                foreach (string word in words)
                {
                    if (!word.StartsWith("http") && !word.StartsWith("www"))
                    {
                        newLine += $"{word} ";
                        continue;
                    }
                    // Replace this 'word' with some TTS note, else its absense will sound odd in speech
                    newLine += $" (see links list). ";
                    responseBuilder.Add(newLine);
                    newLine = string.Empty;

                    string hyperlink = word.Trim().TrimEnd('.');
                    linksBuilder.Add(hyperlink);
                }
                responseBuilder.Add(newLine);
            }

            response = string.Join(" ", responseBuilder).TrimEnd().Replace("  ", " ");
            if (linksBuilder.Count > 0)
                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_ResponseLinks", string.Join(";", linksBuilder));

            return response;
        }

        /// <summary>
        /// A post-process method on ChatGPT responses to (hopefully) parse out any code blocks
        /// from potential text-to-speech responses.
        /// </summary>
        /// <param name="response">The ChatGPT response with any code blocks culled out to a separate return text variable in VoiceAttack.</param>
        /// <returns>The input response with code blocks culled out.<br />
        /// Also sets the 'OpenAI_ResponseCode' text variable to a newline deliniated list of any code blocks culled.</returns>
        private static string ParseCodeBlocks(string response)
        {
            List<string> responseBuilder = new List<string>();
            List<string> codeBuilder = new List<string>();

            bool inCodeBlock = false;

            // ChatGPT returns multiline responses separted by "\n" only
            string[] responses = response.Split(new string[] { "\n" }, StringSplitOptions.None); // we must allow line breaks
            foreach (string line in responses)
            {
                if (line.StartsWith("`"))
                {
                    inCodeBlock = !inCodeBlock;
                    continue;
                }

                if (!inCodeBlock)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string lineFinal = line;
                        responseBuilder.Add(lineFinal.Replace("\u0023", " sharp") + "\r\n");
                    }
                }
                else
                {
                    codeBuilder.Add(line + "\r\n");
                    if (line.EndsWith("`"))
                        inCodeBlock = false;
                }
            }

            response = string.Join(" ", responseBuilder).TrimEnd().Replace("\r\n\r\n", "\r\n");
            if (codeBuilder.Count > 0)
                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_ResponseCode", string.Join(" ", codeBuilder));

            return response;
        }

        /// <summary>
        /// Send a single raw chat request to the OpenAI API using the supplied input (required), using the specified parameters (optional).<br />
        /// Expects user input already in 'OpenAI_UserInput' text variable, and for user to process return on their own.
        /// </summary>
        /// <returns>Sets the 'OpenAI_Response' text variable to the ChatGPT output from OpenAI.</returns>
        /// <exception cref="Exception">Thrown up the stack to Logging.</exception>
        public static async Task Raw() { await Raw(false); }
        /// <summary>
        /// Send a chat session request to the OpenAI API using the supplied input (required), and any specified parameters (optional).<br />
        /// Expects user input already in 'OpenAI_UserInput' text variable, and for user to process return on their own and supply new input at each loop iteration until ended.<br />
        /// Designed to be used with the 'OpenAI_ExternalContinue' as <see langword="true"/>. User manually sets 'OpenAI_Waiting#' boolean <see langword="false"/> to trigger next loop iteration.
        /// </summary>
        /// <param name="chatSession">A boolean indicating that the conversation should continue until the 'OpenAI_Chatting' boolean becomes <see langword="null"/> or <see langword="false"/>.</param>
        /// <returns>Sets the 'OpenAI_Response' text variable to the ChatGPT output from OpenAI at each loop iteration.</returns>
        /// <exception cref="Exception">Thrown up the stack to Logging.</exception>
        public static async Task Raw(bool chatSession)
        {
            string userInput = string.Empty;
            string lastInput = string.Empty;
            string response = string.Empty;
            string systemMessage = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_SystemPrompt") ?? DEFAULT_SYSTEM_PROMPT;
            bool dataSessionBypass = false;

            if (string.IsNullOrEmpty(userInput)) { throw new Exception("User prompt in OpenAI_UserInput text variable is null or empty."); }

            // Set Chat Session Variable 'OpenAI_Chatting#' to TRUE will set OpenAI_Plugin.ChatActive to TRUE as well,
            // This will allow a 'Stop Chatting' command to exit a chat session loop by setting this FALSE
            if (OpenAI_Plugin.ChatActive != true)
            {
                OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_Chatting#", chatSession); // Only set this when not already active, for concurrent raw ChatGPT data calls during active sessions
            }
            else
            {
                dataSessionBypass = true;
            }
            bool sendToEXT = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_ExternalResponder") ?? false; // Use Custom User External Command to process responses
            bool sendToWAIT = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_ExternalContinue") ?? false; // Wait after response for profile to signal 'continue'
            sendToWAIT = chatSession && sendToWAIT; // can't be true outside a session, see 'OpenAI_Chatting#' comments above

            // Set the OpenAI GPT Model and any/all user options and create a new Chat Conversation:
            Model userModel = ModelsGPT.GetOpenAI_Model();

            // Ensure max tokens in 'OpenAI_MaxTokens' is within range for the selected model, uses 512 if not set/invalid
            int userMaxTokens = ModelsGPT.GetValidMaxTokens(userModel);

            decimal getTemp = OpenAI_Plugin.VA_Proxy.GetDecimal("OpenAI_Temperature") ?? 0.2M;
            decimal getTopP = OpenAI_Plugin.VA_Proxy.GetDecimal("OpenAI_TopP") ?? 0M;
            decimal getFreqP = OpenAI_Plugin.VA_Proxy.GetDecimal("OpenAI_FrequencyPenalty") ?? 0M;
            decimal getPresP = OpenAI_Plugin.VA_Proxy.GetDecimal("OpenAI_PresencePenalty") ?? 0M;

            string getStopSequences = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_StopSequences") ?? string.Empty;
            string[] userStopSequences = null;
            if (!string.IsNullOrEmpty(getStopSequences))
            {
                userStopSequences = getStopSequences.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }

            int? userNumChoices = OpenAI_Plugin.VA_Proxy.GetInt("OpenAI_NumChoicesPerMessage") ?? null;
            double userTemperature = (double)getTemp;
            double userTopP = (double)getTopP;
            double userFrequencyPenalty = (double)getFreqP;
            double userPresencePenalty = (double)getPresP;

            OpenAIAPI api = OpenAI_Key.LoadKey
                ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.ApiKey, OpenAI_Key.ApiOrg))
                : new OpenAIAPI(APIAuthentication.LoadFromPath(
                    directory: OpenAI_Key.DefaultKeyFileFolder,
                    filename: OpenAI_Key.DefaultKeyFilename,
                    searchUp: true
            ));

            var chat = api.Chat.CreateConversation(new ChatRequest()
            {
                Model = userModel,
                Temperature = userTemperature,
                MaxTokens = userMaxTokens,
                TopP = userTopP,
                FrequencyPenalty = userFrequencyPenalty,
                PresencePenalty = userPresencePenalty,
                NumChoicesPerMessage = userNumChoices,
                MultipleStopSequences = userStopSequences
            });

            // Provide instruction as System (if any)
            if (!string.IsNullOrEmpty(systemMessage))
            {
                chat.AppendSystemMessage(systemMessage);
            }

            // Provide input/output example for system refinement
            string exampleInput = OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_UserInput_Example") ?? DEFAULT_USER_INPUT_EXAMPLE;
            string exampleOutput = OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_ChatbotOutput_Example") ?? DEFAULT_CHATBOT_OUTPUT_EXAMPLE;
            if (systemMessage == DEFAULT_SYSTEM_PROMPT)
            {
                chat.AppendUserInput(DEFAULT_USER_INPUT_EXAMPLE);
                chat.AppendExampleChatbotOutput(DEFAULT_CHATBOT_OUTPUT_EXAMPLE);
            }
            else
            {
                for (int i = 1; i <= 5; i++)
                {
                    exampleInput = OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_UserInput_Example{i}") ?? exampleInput;
                    exampleOutput = OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_ChatbotOutput_Example{i}") ?? exampleOutput;
                    if (!string.IsNullOrWhiteSpace(exampleInput) && !string.IsNullOrWhiteSpace(exampleOutput))
                    {
                        chat.AppendUserInput(exampleInput);
                        chat.AppendExampleChatbotOutput(exampleOutput);
                    }
                    exampleInput = string.Empty;
                    exampleOutput = string.Empty;
                }
            }

            do
            {
                if (!string.IsNullOrEmpty(userInput) && userInput != lastInput)
                {
                    chat.AppendUserInput(userInput);

                    response = await chat.GetResponseFromChatbotAsync();

                    OpenAI_Plugin.VA_Proxy.SetText("OpenAI_UserInput_Last", userInput);
                    OpenAI_Plugin.VA_Proxy.SetText("OpenAI_UserInput", string.Empty);

                    if (!string.IsNullOrEmpty(response))
                    {
                        OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Response", response);

                        // Optional custom user flow command to handle response
                        SendToExternalCommand(sendToEXT); // may potentially provide next user input here by setting 'OpenAI_UserInput'
                    }

                    lastInput = userInput;
                }

                // Optional custom user flow boolean to continue - waits here until 'OpenAI_ChatWaiting#' becomes false (or stop-all commands)
                WaitForExternalContinue(sendToWAIT); // may potentially provide next user input here by setting 'OpenAI_UserInput'

                userInput = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_UserInput") ?? string.Empty;

                // Bypass will be active for concurrent raw ChatGPT data calls during already active ChatGPT sessions
                chatSession = !dataSessionBypass && (OpenAI_Plugin.ChatActive ?? false);

            }
            while (chatSession && !OpenAI_Plugin._stopVariableToMonitor);
        }

        /// <summary>
        /// Send a single chat request to the OpenAI API using the supplied input (if provided), using the specified parameters (optional).<br />
        /// Valid responses will execute a Text-to-Speech command in VoiceAttack to speak the response if allowed.<br />
        /// Will begin the default or custom Listen and Dictation methods or commands if no input is provided.
        /// </summary>
        /// <returns>Sets the OpenAI_Response text variable to the ChatGPT output from OpenAI.</returns>
        /// <exception cref="Exception">Thrown up the stack to Logging.</exception>
        public static async Task Chat() { await Chat(false, null); }
        /// <summary>
        /// Send a chat session request to the OpenAI API using the supplied input (if provided), and any specified parameters (optional).<br />
        /// Valid responses will execute a Text-to-Speech command in VoiceAttack to speak the response (if allowed) at each loop iteration until ended.<br />
        /// Will begin the default or custom Listen and Dictation methods or commands if no input is provided.
        /// </summary>
        /// <param name="chatSession">A boolean indicating that the conversation should continue until the 'OpenAI_Chatting' boolean becomes null or false.</param>
        /// <exception cref="Exception">Thrown up the stack to Logging.</exception>
        public static async Task Chat(bool chatSession) { await Chat(chatSession, null); }
        /// <summary>
        /// Send a single chat request to the OpenAI API using Whisper to process the supplied input (if provided), with any specified parameters (optional).<br />
        /// Valid responses will execute a Text-to-Speech command in VoiceAttack to speak the response if allowed.<br />
        /// Will begin the default or custom Listen and Dictation methods or commands if no input is provided.
        /// </summary>
        /// <param name="operation">The type of Whisper operation to perform, either "transcribe" or "translate".</param>
        /// <returns>Sets the OpenAI_Response text variable to the ChatGPT output from OpenAI.</returns>
        /// <exception cref="Exception">Thrown up the stack to Logging.</exception>
        public static async Task Chat(string operation) { await Chat(false, operation); }
        /// <summary>
        /// Send a chat session request to the OpenAI API using Whisper to process supplied input (if provided), using the specified parameters (optional).<br />
        /// Valid responses will execute a Text-to-Speech command in VoiceAttack to speak the response (if allowed) at each loop iteration until ended.<br />
        /// Will begin the default or custom Listen and Dictation methods or commands if no input is provided.
        /// </summary>
        /// <param name="chatSession">A boolean indicating that the conversation should continue until the 'OpenAI_Chatting' boolean becomes <see langword="null"/> or <see langword="false"/>.</param>
        /// <param name="operation">The type of Whisper operation to perform, either "transcribe" or "translate".</param>
        /// <exception cref="Exception">Thrown up the stack to Logging.</exception>
        public static async Task Chat(bool chatSession, string operation)
        {
            bool useWhisper = !string.IsNullOrEmpty(operation);
            string lastSpokenCMD = OpenAI_Plugin.VA_Proxy.ParseTokens("{LASTSPOKENCMD}");
            string lastInput = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_UserInput_Last") ?? string.Empty;
            string userInput = string.Empty;
            string response = string.Empty;

            // Must Exit a Chat request if already chatting and/or waiting to continue to avoid race conditions on input/output variables
            if (OpenAI_Plugin.ChatWaiting == true)
            {
                throw new Exception("Unable to initiate new Chat context plugin call, chat is already active and waiting to continue." + 
                                    " Use Raw.Chat context for concurrent ChatGPT requests durring active Chat sessions.");
            }
            if (OpenAI_Plugin.ChatActive == true) { throw new Exception("Unable to initiate new Chat context plugin call, chat is already active. Use Raw.Chat context instead."); }

            // Must reset to true here at top of any new Chat call. When false, can bypass the 'wait to continue' phase of a session back to GetInput
            Dictation.GetInputTimeout = true; // This bool can only become false when (int) 'OpenAI_ListenTimeout_Seconds' > 0, else is always 'true'

            // Set Chat Session Variable 'OpenAI_Chatting#' to TRUE will set OpenAI_Plugin.ChatActive to TRUE as well,
            // This will allow a 'Stop Chatting' command to exit a chat session loop by setting this FALSE
            OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_Chatting#", chatSession);


            string systemMessage = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_SystemPrompt") ?? DEFAULT_SYSTEM_PROMPT;
            bool sendToLog = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_LogChat") ?? true;  // VoiceAttack Event Logging of user inputs & chatbot responses
            bool sendToTTS = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_SpeakChat") ?? true; // Use VoiceAttack 'say' command in 'OpenAI_Command_Speech' for responses
            bool sendToEXT = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_ExternalResponder") ?? false; // Use Custom User External Command to process responses
            bool sendToWAIT = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_ExternalContinue") ?? false; // Wait after response and GetInput Timeout for profile to signal 'continue'
            bool sendToWAITexclusive = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_ExternalContinue_Exclusive") ?? false; // Always wait after response for profile to signal 'continue'
            string logInputPretext = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_LogChat_InputPretext") ?? "Asking ChatGPT:"; // Only used when sendToLog is true
            string logOutputPretext = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_LogChat_OutputPretext") ?? "ChatGPT Reply:"; // Only used when sendToLog is true
            bool sayPreListen = !string.IsNullOrEmpty(OpenAI_Plugin.VA_Proxy.GetText("OpenAI_TTS_PreListen") ?? string.Empty);

            // Ensure Text-to-Speech command exists in VoiceAttack profile - exception will be thrown and plugin call will exit upon failure here:
            string speechCommand = GetExistingCommand("OpenAI_Command_Speech", DEFAULT_SPEECH_COMMAND, sendToTTS); // default TTS command phrase: "((OpenAI Speech))"


            userInput = Dictation.GetUserInput(useWhisper, sayPreListen);

            if (useWhisper && !string.IsNullOrEmpty(userInput) && userInput != DEFAULT_REPROCESS_FLAG)
                userInput = await Whisper.GetUserInputAsync(operation);

            if (string.IsNullOrEmpty(userInput)) { throw new Exception("User prompt in OpenAI_UserInput text variable is null or empty."); }

            // Optional speech before processing input through OpenAI API (which can be slow!)
            if (userInput != DEFAULT_REPROCESS_FLAG)
                ProvideFeedback("OpenAI_TTS_PreProcess");

            Model userModel = ModelsGPT.GetOpenAI_Model();
            int userMaxTokens = ModelsGPT.GetValidMaxTokens(userModel);

            decimal getTemp = OpenAI_Plugin.VA_Proxy.GetDecimal("OpenAI_Temperature") ?? 0.2M;
            decimal getTopP = OpenAI_Plugin.VA_Proxy.GetDecimal("OpenAI_TopP") ?? 0M;
            decimal getFreqP = OpenAI_Plugin.VA_Proxy.GetDecimal("OpenAI_FrequencyPenalty") ?? 0M;
            decimal getPresP = OpenAI_Plugin.VA_Proxy.GetDecimal("OpenAI_PresencePenalty") ?? 0M;

            string getStopSequences = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_StopSequences") ?? string.Empty;
            string[] userStopSequences = null;
            if (!string.IsNullOrEmpty(getStopSequences))
            {
                userStopSequences = getStopSequences.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }

            int? userNumChoices = OpenAI_Plugin.VA_Proxy.GetInt("OpenAI_NumChoicesPerMessage") ?? null;
            double userTemperature = (double)getTemp;
            double userTopP = (double)getTopP;
            double userFrequencyPenalty = (double)getFreqP;
            double userPresencePenalty = (double)getPresP;

            OpenAIAPI api = OpenAI_Key.LoadKey
                ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.ApiKey, OpenAI_Key.ApiOrg))
                : new OpenAIAPI(APIAuthentication.LoadFromPath(
                    directory: OpenAI_Key.DefaultKeyFileFolder,
                    filename: OpenAI_Key.DefaultKeyFilename,
                    searchUp: true
            ));

            var chat = api.Chat.CreateConversation(new ChatRequest()
            {
                Model = userModel,
                Temperature = userTemperature,
                MaxTokens = userMaxTokens,
                TopP = userTopP,
                FrequencyPenalty = userFrequencyPenalty,
                PresencePenalty = userPresencePenalty,
                NumChoicesPerMessage = userNumChoices,
                MultipleStopSequences = userStopSequences
            });

            if (!string.IsNullOrEmpty(systemMessage))
            {
                chat.AppendSystemMessage(systemMessage);
            }

            string exampleInput = OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_UserInput_Example1") ?? string.Empty;
            string exampleOutput = OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_ChatbotOutput_Example1") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(exampleInput) && string.IsNullOrWhiteSpace(exampleOutput))
            {
                exampleInput = systemMessage == DEFAULT_SYSTEM_PROMPT
                    ? OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_UserInput_Example") ?? DEFAULT_USER_INPUT_EXAMPLE
                    : OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_UserInput_Example") ?? string.Empty;

                exampleOutput = systemMessage == DEFAULT_SYSTEM_PROMPT
                    ? OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_ChatbotOutput_Example") ?? DEFAULT_CHATBOT_OUTPUT_EXAMPLE
                    : OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_ChatbotOutput_Example") ?? string.Empty;

                if (!string.IsNullOrEmpty(exampleInput) && !string.IsNullOrEmpty(exampleOutput))
                {
                    chat.AppendUserInput(DEFAULT_USER_INPUT_EXAMPLE);
                    chat.AppendExampleChatbotOutput(DEFAULT_CHATBOT_OUTPUT_EXAMPLE);
                }
            }
            else
            {
                int i = 0;
                do
                {
                    i++;
                    exampleInput = OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_UserInput_Example{i}") ?? string.Empty;
                    exampleOutput = OpenAI_Plugin.VA_Proxy.GetText($"OpenAI_ChatbotOutput_Example{i}") ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(exampleInput) || string.IsNullOrWhiteSpace(exampleOutput))
                        break;

                    chat.AppendUserInput(exampleInput);
                    chat.AppendExampleChatbotOutput(exampleOutput);
                }
                while (!OpenAI_Plugin._stopVariableToMonitor);

                if (OpenAI_Plugin.DebugActive == true)
                    Logging.WriteToLog_Long($"OpenAI Plugin: {i} input/output messages have been loaded to this ChatGPT Session", "orange");

            }

            // If reprocessing on first run, this is merely to load past conversations and initiate an already waiting session:
            if (userInput == DEFAULT_REPROCESS_FLAG)
                userInput = string.Empty;

            do
            {
                /// NOTE: Dev Testing NEW PreProcessInput phase - this will go at the top of the ChatGPT method, here only while building:
                string processingCommand = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_Command_PreProcessInput") ?? string.Empty;
                bool processingCustom = !string.IsNullOrEmpty(processingCommand) && OpenAI_Plugin.VA_Proxy.Command.Exists(processingCommand);
                float? processingSimilarity = (float?)OpenAI_Plugin.VA_Proxy.GetDecimal("OpenAI_PreProcessSimilarity");
                int processingCount = (int)(OpenAI_Plugin.VA_Proxy.GetInt("OpenAI_PreProcessSimilarityCount") ?? 0);
                int? processingVolume = (int?)OpenAI_Plugin.VA_Proxy.GetInt("OpenAI_PreProcessSimilarityVolume");
                string processingPrompt = OpenAI_Plugin.VA_Proxy.GetInt("OpenAI_PreProcessSimilarityPrompt") ?? string.Empty;
                bool sendToEXEC = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_PreProcessExecute") ?? false;
                bool sendToJSON = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_PreProcessInput") ?? false;
                if (sendToJSON)
                {
                    Embedding.ExistingEmbeddings.Clear();

                    string vectorsFilePath = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_PreProcessJSON") ?? string.Empty;
                    if (!string.IsNullOrEmpty(vectorsFilePath))
                    {
                        // Deserialize the JSON file to a list of EmbeddingEntry objects for use in Session loop
                        Embedding.ExistingEmbeddings = Embedding.ReadJsonFile(vectorsFilePath);
                    }

                    if (Embedding.ExistingEmbeddings.Count == 0)
                    {
                        throw new Exception($"OpenAI_PreProcessInput is TRUE but no valid embedding vectors file found at: {vectorsFilePath}");
                    }
                }

                // Peform optional pre-processing of input - first new action inside do-while session loop:
                string userInputText = userInput;
                if (!(Dictation.GetInputTimeout || string.IsNullOrEmpty(userInput) || OpenAI_Plugin.VA_Proxy.Command.Exists(userInput)))
                {
                    // Optional pre-processing of user input using embedding vectors cosine similarity:
                    if (sendToJSON)
                    {
                        userInput = await Embedding.Processing(
                            embeddingEntries: Embedding.ExistingEmbeddings,
                            similarityCutoff: processingSimilarity,
                            systemPrompt: processingPrompt,
                            embeddingContent: userInput,
                            topN: processingVolume,
                            topK: processingCount
                        );
                    }

                    // Optional custom pre-processing of user input after whisper and/or embedding vector pre-processing above:
                    if (processingCustom)
                    {
                        OpenAI_Plugin.VA_Proxy.Command.Execute(processingCommand, true); // waits here for it to complete...
                        userInput = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_UserInput") ?? string.Empty;
                    }

                    // Optional execution of pre-processed user input as existing command in VoiceAttack:
                    if (sendToEXEC && !string.IsNullOrEmpty(userInput) && OpenAI_Plugin.VA_Proxy.Command.Exists(userInput))
                    {
                        OpenAI_Plugin.VA_Proxy.Command.Execute(userInput, true); // waits here for it to complete...
                    }
                }

                // For a perpetual background chat session configuration, ignore existing user commands and return to waiting
                if (!OpenAI_Plugin.VA_Proxy.Command.Exists(userInput))
                {
                    if (string.IsNullOrEmpty(userInput) || (!string.IsNullOrEmpty(userInput) && userInput == lastInput))
                    {
                        // Exit or return to a Waiting Loop when input is same as last or emtpy
                        Dictation.GetInputTimeout = true;
                        OpenAI_Plugin.VA_Proxy.SetText("OpenAI_TTS_Response", DEFAULT_REPROCESS_FLAG);
                        OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Response", string.Empty);
                    }
                    else
                    {
                        // Write the user input to VoiceAttack Event Log (if enabled)
                        if (sendToLog)
                        {
                            Logging.WriteToLog_Long($"{logInputPretext} {userInputText}", "yellow");
                        }

                        // Reset Response Post-Process text variables here
                        OpenAI_Plugin.VA_Proxy.SetText("OpenAI_ResponseCode", null);
                        OpenAI_Plugin.VA_Proxy.SetText("OpenAI_ResponseLinks", null);

                        // Send the user input to ChatGPT
                        chat.AppendUserInput(userInput);
                        lastInput = userInput;

                        // Get the response from ChatGPT
                        response = await chat.GetResponseFromChatbotAsync();

                        if (!string.IsNullOrEmpty(response))
                        {
                            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Response", response);

                            // Attempt to post-process for TTS response by separating out code blocks (if properly contained in "`" symbols)
                            if (response.Contains("`"))
                            {
                                response = ParseCodeBlocks(response);
                            }

                            // Attempt to post-process for TTS response by separating out hyperlinks (if containing any "https:/" URL prefixes)
                            if (response.Contains("https:/") || response.Contains("www."))
                            {
                                response = ParseHyperlinks(response);
                            }

                            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_TTS_Response", response);
                        }
                    }
                    OpenAI_Plugin.VA_Proxy.SetText("OpenAI_UserInput_Last", userInput);
                    OpenAI_Plugin.VA_Proxy.SetText("OpenAI_UserInput", string.Empty);

                    // Pre-Speech break check for 'stop' during wait for chatbot response
                    if (OpenAI_Plugin._stopVariableToMonitor)
                        break;

                    // Write the ChatGPT response to VoiceAttack Event Log (if enabled)
                    if (sendToLog && !Dictation.GetInputTimeout)
                        Logging.WriteToLog_Long($"{logOutputPretext} {response}", "green");

                    // Use text-to-speech to say the response using the OpenAI Plugin VoiceAttack Profile required function command,
                    // default TTS command phrase: "((OpenAI Speech))"
                    if (sendToTTS)
                        OpenAI_Plugin.VA_Proxy.Command.Execute(speechCommand, true);

                    // Use custom user flow command to handle response
                    SendToExternalCommand(sendToEXT && !Dictation.GetInputTimeout); // may potentially provide next user input here by setting 'OpenAI_UserInput'

                }
                else
                {
                    // An Unrecognized command (low confidence?) fixed by Whisper and exists, ignore the user input
                    // ...later, this could be a place to re-execute if it wasn't executed? For a future version....

                    Dictation.GetInputTimeout = true; // If command exists after transcription, consider it as a timeout for now
                }

                // Reset this var in case they spoke a 'Stop Talking' command, but not 'Stop Chat Session' command
                lastSpokenCMD = OpenAI_Plugin.VA_Proxy.ParseTokens("{LASTSPOKENCMD}");

                // Post-Speech break check for non-session context or 'stop all commands' action
                if (!chatSession || OpenAI_Plugin._stopVariableToMonitor)
                    break;

                // Use custom user flow boolean to continue - waits here until 'OpenAI_ChatWaiting#' becomes false (or stop-all commands, if not 'unstoppable')
                WaitForExternalContinue(sendToWAIT && sendToWAITexclusive || sendToWAIT && Dictation.GetInputTimeout); // may potentially provide next user input here by setting 'OpenAI_UserInput'

                if (sendToWAIT && (OpenAI_Plugin.ChatActive != true || OpenAI_Plugin._stopVariableToMonitor))
                    break;


                userInput = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_UserInput") ?? string.Empty;
                userInput = (!sendToEXT || (sendToEXT && string.IsNullOrEmpty(userInput)))
                                ? Dictation.GetUserInput(useWhisper, sayPreListen)
                                : userInput;

                if (OpenAI_Plugin.ChatActive != true || OpenAI_Plugin._stopVariableToMonitor || (!chatSession && lastSpokenCMD != OpenAI_Plugin.VA_Proxy.ParseTokens("{LASTSPOKENCMD}")))
                    break;


                if (userInput == lastInput)
                {
                    Dictation.GetInputTimeout = true;
                }

                // Optional speech before processing chat session input through OpenAI API (which can be slow!)
                if (!Dictation.GetInputTimeout)
                    ProvideFeedback("OpenAI_TTS_PreProcess");

                if (useWhisper && !Dictation.GetInputTimeout)
                    userInput = await Whisper.GetUserInputAsync(operation);

                if (userInput.StartsWith("OpenAI_NET"))
                {
                    Dictation.GetInputTimeout = true;
                }

                if (Dictation.GetInputTimeout)
                    userInput = string.Empty;

                if (string.IsNullOrEmpty(userInput) && !Dictation.GetInputTimeout) { throw new Exception("User prompt in OpenAI_UserInput text variable is null or empty."); }

                chatSession = OpenAI_Plugin.ChatActive ?? false;
            }
            while (chatSession && !OpenAI_Plugin._stopVariableToMonitor);
        }

    }
}
