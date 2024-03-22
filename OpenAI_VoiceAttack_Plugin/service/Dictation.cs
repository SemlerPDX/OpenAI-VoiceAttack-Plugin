using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Wave;
using System.Threading;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing methods for getting and storing User Input for the <see cref="ChatGPT"/> class.
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
    public static class Dictation
    {
        private static readonly string DEFAULT_DICTATIONSTART_COMMAND = "((OpenAI DictationStart))";
        private static readonly string DEFAULT_DICTATIONSTOP_COMMAND = "((OpenAI DictationStop))";

        /// <summary>
        /// A boolean to indicate that <see cref="Dictation.GetUserInput(bool, bool)"/> has ended with no user input.
        /// </summary>
        public static bool GetInputTimeout { get; set; } = false;

        /// <summary>
        /// The list of file names produced from a single dictation audio session.
        /// </summary>
        public static List<string> DictationAudioFileNames { get; set; } = new List<string>();


        /// <summary>
        /// Generate a new audio file for the very last dictation audio capture (if dictation audio exists),
        /// using a new random GUID as the name, and storing the path to that file in the 'OpenAI_AudioFile' text variable.
        /// </summary>
        private static void SaveCapturedAudio()
        {
            // If empty, there is no dictation audio captured to save
            string newDictation = OpenAIplugin.VA_Proxy.ParseTokens("{DICTATION}") ?? String.Empty;
            if (String.IsNullOrEmpty(newDictation)) { return; }

            // Generate new random file name for the dictation audio capture
            string fileName = Guid.NewGuid().ToString();
            string audioFilePath = System.IO.Path.Combine(Configuration.DEFAULT_AUDIO_FOLDER, fileName + ".wav");

            // Store this variable for the CombineWavFiles method:
            DictationAudioFileNames.Add(audioFilePath);

            using (MemoryStream ms = OpenAIplugin.VA_Proxy.Utility.CapturedAudio(5))
            {
                if (ms == null)
                    return;

                using (FileStream fileStream = new FileStream(audioFilePath, FileMode.Create))
                {
                    ms.WriteTo(fileStream);
                }
            }

            // Store this variable for the Whisper Class as the current audio file target:
            OpenAIplugin.VA_Proxy.SetText("OpenAI_AudioFile", audioFilePath); // made null after plugin completes

            // Clear Dictation Buffer for the next inputs
            OpenAIplugin.VA_Proxy.Dictation.ClearBuffer(false, out string _);
        }


        /// <summary>
        /// Combines multiple input WAV files into a single output WAV file.
        /// </summary>
        /// <param name="inputFilePaths">A list of file paths to the input WAV files to be combined.</param>
        /// <exception cref="InvalidOperationException">Thrown when the input WAV files have different formats.</exception>
        public static void CombineWavFiles(List<string> inputFilePaths)
        {
            if (inputFilePaths.Count > 1)
            {
                // Get the format of the first input file
                WaveFormat waveFormat = null;
                using (var waveFileReader = new WaveFileReader(inputFilePaths[0]))
                {
                    waveFormat = waveFileReader.WaveFormat;
                }

                // Generate new random file name for the dictation audio capture
                string fileName = Guid.NewGuid().ToString();
                string outputFilePath = System.IO.Path.Combine(Configuration.DEFAULT_AUDIO_FOLDER, fileName + ".wav");

                // Create a new wave file writer with the same format as the input files
                using (var waveFileWriter = new WaveFileWriter(outputFilePath, waveFormat))
                {
                    foreach (var inputFilePath in inputFilePaths)
                    {
                        // Read each input file and append its data to the output file
                        using (var waveFileReader = new WaveFileReader(inputFilePath))
                        {
                            if (!waveFileReader.WaveFormat.Equals(waveFormat))
                            {
                                throw new InvalidOperationException("Input files must have same format.");
                            }

                            byte[] buffer = new byte[waveFileReader.Length];
                            int bytesRead = waveFileReader.Read(buffer, 0, buffer.Length);
                            waveFileWriter.Write(buffer, 0, bytesRead);
                        }
                    }
                }

                // Store this new variable for the Whisper Class as the combined audio file target:
                OpenAIplugin.VA_Proxy.SetText("OpenAI_AudioFile", outputFilePath); // made null ('Not set') after plugin completes
            }
        }



        /// <summary>
        /// The method for the 'GetInput' phase of a ChatGPT plugin context call. When the <paramref name="sayPreListen"/> parameter is set to <see langword="true"/>,<br />
        /// and also the 'OpenAI_TTS_PreListen' text variable contains a speech phrase or path to an audio file, it will execute a VoiceAttack<br />
        /// profile command to speak a pre-listen phrase, or play a pre-listen sound, before gathering the user input from VoiceAttack Dictation<br />
        /// in text and (optionally) audio file format, either through plugin methods, or through executing custom user command names stored in<br />
        /// the 'OpenAI_Command_DictationStart' and/or 'OpenAI_Command_DictationStop' text variables. If the 'OpenAI_UserInput' text variable already has input, that will be returned.
        /// </summary>
        /// <param name="generateAudioFile">A boolean to indicate whether to generate a new audio file of the last Dictation,
        /// and set the path to that file in the 'OpenAI_AudioFile' text variable for Whisper transcription/translation later.</param>
        /// <param name="sayPreListen">A boolean to indicate if user feedback should be provided before listening begins.</param>
        /// <returns>The raw text input gathered, either from an existing value set in 'OpenAI_UserInput', or from VoiceAttack Dictation Buffer.</returns>
        /// <exception cref="Exception">
        /// Thrown when user has set the name of a VoiceAttack command to the 'OpenAI_Command_DictationStop' text variable,
        /// but that command does not exist, nor does the default example command, named in 'DEFAULT_DICTATIONSTOP_COMMAND',
        /// and also the plugin method to generate and save Dictation audio to file have failed as well.
        /// </exception>
        public static string GetUserInput(bool generateAudioFile, bool sayPreListen)
        {
            // Clear Dictation Buffer and Start Dictation Mode
            OpenAIplugin.VA_Proxy.Dictation.ClearBuffer(false, out string _);

            // Check if users want to stop the VoiceAttack global 'Listening Mode' below when complete
            bool stopListeningMode = OpenAIplugin.VA_Proxy.GetBoolean("OpenAI_DisableListenMode") ?? false;

            // Check if default OR user custom 'Dictation Start' command exists in VoiceAttack profile:
            string lastSpokenCMD = OpenAIplugin.VA_Proxy.ParseTokens("{LASTSPOKENCMD}");
            string listenCommand = OpenAIplugin.VA_Proxy.GetText("OpenAI_Command_DictationStart") ?? DEFAULT_DICTATIONSTART_COMMAND;
            if (!OpenAIplugin.VA_Proxy.Command.Exists(listenCommand) && OpenAIplugin.VA_Proxy.Command.Exists(DEFAULT_DICTATIONSTART_COMMAND))
                listenCommand = DEFAULT_DICTATIONSTART_COMMAND;

            // Run custom profile based listen method for this phase of GetInput:
            if (OpenAIplugin.VA_Proxy.Command.Exists(listenCommand))
            {
                OpenAIplugin.VA_Proxy.Command.Execute(listenCommand, true); // waits here for it to complete...
                // Post-Dictation break check to end a chat session and exit if the 'stop chatting' command is detected:
                bool chatSession = OpenAIplugin.CHAT_ACTIVE ?? false;
                if (!chatSession || OpenAIplugin._stopVariableToMonitor)
                    return String.Empty;
            }

            // Set default OR user custom 'Dictation Stop' if command exists in VoiceAttack profile, or empty string if none:
            string dictationCommand = OpenAIplugin.VA_Proxy.GetText("OpenAI_Command_DictationStop") ?? DEFAULT_DICTATIONSTOP_COMMAND;
            if (!OpenAIplugin.VA_Proxy.Command.Exists(dictationCommand))
                dictationCommand = OpenAIplugin.VA_Proxy.Command.Exists(DEFAULT_DICTATIONSTOP_COMMAND) ? DEFAULT_DICTATIONSTOP_COMMAND : String.Empty;

            // Check for manually provided input or otherwise produced {DICTATION} token value (if any):
            string userInput = OpenAIplugin.VA_Proxy.GetText("OpenAI_UserInput") ?? String.Empty;
            if (String.IsNullOrEmpty(userInput))
                userInput = OpenAIplugin.VA_Proxy.ParseTokens("{DICTATION}") ?? String.Empty;

            // Fall-back input method for minimalist profile approach option:
            if (String.IsNullOrEmpty(userInput) && !OpenAIplugin.VA_Proxy.Command.Exists(listenCommand))
            {
                // Optional speech before capturing input through Dictation text and audio capture
                if (sayPreListen && GetInputTimeout)
                    ChatGPT.ProvideFeedback("OpenAI_TTS_PreListen", true);


                // End Dictation Mode
                if (OpenAIplugin.VA_Proxy.Dictation.IsOn())
                {
                    OpenAIplugin.VA_Proxy.Dictation.Stop();

                    // Clear Dictation Buffer before starting again
                    OpenAIplugin.VA_Proxy.Dictation.ClearBuffer(false, out string _);
                }

                if (OpenAIplugin.VA_Proxy.Dictation.Start(out string _))
                {
                    OpenAIplugin.VA_Proxy.SetBoolean("OpenAI_Listening#", true);

                    // Get custom user input timeout (convert to ms) - if none or zero, default used
                    int timeoutMs = OpenAIplugin.VA_Proxy.GetInt("OpenAI_ListenTimeout_Seconds") ?? 0;
                    timeoutMs = timeoutMs == 0
                        ? 1800 // default of 1.8 seconds of no speech active to timeout
                        : timeoutMs *= 1000; // convert custom user timeout into milliseconds

                    // Set initial timeout (convert to ms) to allow speaker to form thoughts before speaking
                    int initialTimeoutMs = OpenAIplugin.VA_Proxy.GetInt("OpenAI_ListenTimeout_InitialSeconds") ?? 0;
                    initialTimeoutMs = initialTimeoutMs == 0
                        ? 5000 // default of 1.8 seconds of no speech active to timeout
                        : initialTimeoutMs *= 1000; // convert custom user timeout into milliseconds
                    bool isInitialTimeout = true;

                    int listenTimeoutMs = initialTimeoutMs;

                    // Sets property to bypass this continuous back-and-forth conversational feature when no timeout possible
                    GetInputTimeout = false;

                    DateTime startTime = DateTime.Now;
                    DateTime initialStartTime = DateTime.Now;

                    int intervalMs = 100; // 100 milliseconds
                    int writeInterval = 5000 / intervalMs; // calculate the 5 second interval for debugging output
                    int index = 0;

                    // Reset and declare Dictation containers
                    DictationAudioFileNames.Clear();
                    string lastDictation = String.Empty;
                    List<string> appendedDictation = new List<string>();

                    while (OpenAIplugin.LISTEN_ACTIVE == true && OpenAIplugin.VA_Proxy.Dictation.IsOn() && !OpenAIplugin._stopVariableToMonitor)
                    {
                        // Check if a non-session call was merely to an existing VoiceAttack command, and exit silently
                        if (OpenAIplugin.CHAT_ACTIVE != true && lastSpokenCMD != OpenAIplugin.VA_Proxy.ParseTokens("{LASTSPOKENCMD}"))
                        {
                            OpenAIplugin.VA_Proxy.SetBoolean("OpenAI_Error", true);
                            GetInputTimeout = true;
                            break;
                        }

                        string newDictation = OpenAIplugin.VA_Proxy.ParseTokens("{DICTATION}") ?? String.Empty;
                        if (!String.IsNullOrEmpty(newDictation) && newDictation != lastDictation)
                        {
                            lastDictation = newDictation;
                            appendedDictation.Add(newDictation);

                            // Save Dictation Audio for Whisper
                            if (generateAudioFile)
                                SaveCapturedAudio();

                        }

                        // Check if the timeout has been reached or active chat manually ended
                        if (OpenAIplugin.LISTEN_ACTIVE != true || !OpenAIplugin.VA_Proxy.State.SpeechActive() && ((DateTime.Now - startTime).TotalMilliseconds >= listenTimeoutMs))
                        {
                            break;
                        }

                        // Modified from Pfeil's awesome 'Dictation until silence' post on VoiceAttack forums:
                        if (OpenAIplugin.VA_Proxy.State.SpeechActive() || (isInitialTimeout && (DateTime.Now - initialStartTime).TotalMilliseconds >= initialTimeoutMs))
                        {
                            isInitialTimeout = false;
                            listenTimeoutMs = timeoutMs;
                            startTime = DateTime.Now;
                        }

                        // Sleep for this interval and check for new user input
                        Thread.Sleep(intervalMs);


                        // Write 'still waiting' message every 5 seconds if debugging
                        index++;
                        if (OpenAIplugin.DEBUG_ACTIVE == true && index % writeInterval == 0)
                            OpenAIplugin.VA_Proxy.WriteToLog("OpenAI ChatGPT is still waiting for user input to continue...", "yellow");

                    }

                    if (!GetInputTimeout)
                    {
                        appendedDictation.Add(OpenAIplugin.VA_Proxy.ParseTokens("{DICTATION}") ?? String.Empty);
                        userInput = string.Join(" ", appendedDictation).TrimEnd();
                        GetInputTimeout = String.IsNullOrWhiteSpace(userInput) || OpenAIplugin.LISTEN_ACTIVE != true;
                    }

                    // End Dictation Mode
                    if (OpenAIplugin.VA_Proxy.Dictation.IsOn())
                        OpenAIplugin.VA_Proxy.Dictation.Stop();

                    // Must be reset to false when listening is ended
                    OpenAIplugin.VA_Proxy.SetBoolean("OpenAI_Listening#", false);

                    // Factor in a stop button press as equal to a timeout
                    GetInputTimeout = GetInputTimeout || OpenAIplugin._stopVariableToMonitor;

                    // Must ensure is empty if manually ended
                    if (GetInputTimeout)
                        userInput = String.Empty;

                    // End VoiceAttack Global Listening on timeout if requested
                    if (stopListeningMode && GetInputTimeout)
                        OpenAIplugin.VA_Proxy.State.SetListeningEnabled(false);

                    if (!String.IsNullOrEmpty(userInput) && !OpenAIplugin._stopVariableToMonitor)
                    {
                        // Check if default OR user custom 'Dictation Stop' command exists in VoiceAttack profile:
                        if (OpenAIplugin.VA_Proxy.Command.Exists(dictationCommand))
                        {
                            OpenAIplugin.VA_Proxy.Command.Execute(dictationCommand, true); // waits here for it to complete...
                        }
                        else
                        {
                            try
                            {
                                // Fall-back method for minimalist profile approach option:
                                if (generateAudioFile)
                                {
                                    // Save final dictation audio to file (if any)
                                    SaveCapturedAudio();

                                    // Combine all audio files
                                    CombineWavFiles(DictationAudioFileNames);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"No valid '{DEFAULT_DICTATIONSTOP_COMMAND}' command available for chat session: {ex.Message}");
                            }
                        }
                    }

                    OpenAIplugin._stopVariableToMonitor = false;
                }
            }
            return userInput;
        }

    }
}
