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
    public static class Dictation
    {
        private static readonly string DEFAULT_DICTATION_START_COMMAND = "((OpenAI DictationStart))";
        private static readonly string DEFAULT_DICTATION_STOP_COMMAND = "((OpenAI DictationStop))";

        /// <summary>
        /// A boolean to indicate that <see cref="Dictation.GetUserInput(bool, bool)"/> has ended with no user input.
        /// </summary>
        public static bool GetInputTimeout { get; set; } = false;

        /// <summary>
        /// The list of file names produced from a single dictation audio session.
        /// </summary>
        public static List<string> DictationAudioFileNames { get; set; } = new List<string>();


        /// <summary>
        /// A method to delete the old dictation audio files after use, because the action which
        /// writes them cannot overwrite, and a new audio file is created each time, requiring cleanup.
        /// </summary>
        /// <returns>True when audio folder exists and files deleted, false if otherwise.</returns>
        public static bool DeletedOldDictationAudio()
        {
            try
            {
                if (!Directory.Exists(Configuration.DEFAULT_AUDIO_FOLDER))
                    return false;

                string[] files = Directory.GetFiles(Configuration.DEFAULT_AUDIO_FOLDER);

                if (files.Length == 0)
                    return false;

                foreach (string file in files.Where(f => f.EndsWith(".wav")))
                {
                    File.Delete(file);
                }

                return true;
            }
            catch
            {
                // ...let it slide, the plugin command also runs this in an inline function when the call ends.
            }

            return false;
        }

        /// <summary>
        /// Generate a new audio file for the very last dictation audio capture (if dictation audio exists),
        /// using a new random GUID as the name, and storing the path to that file in the 'OpenAI_AudioFile' text variable.
        /// </summary>
        private static void SaveCapturedAudio()
        {
            string newDictation = OpenAI_Plugin.VA_Proxy.ParseTokens("{DICTATION}") ?? string.Empty;
            if (string.IsNullOrEmpty(newDictation))
            {
                return;
            }

            string fileName = Guid.NewGuid().ToString();
            string audioFilePath = System.IO.Path.Combine(Configuration.DEFAULT_AUDIO_FOLDER, fileName + ".wav");

            DictationAudioFileNames.Add(audioFilePath);

            using (MemoryStream ms = OpenAI_Plugin.VA_Proxy.Utility.CapturedAudio(5))
            {
                if (ms == null)
                {
                    return;
                }

                using (FileStream fileStream = new FileStream(audioFilePath, FileMode.Create))
                {
                    ms.WriteTo(fileStream);
                }
            }

            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_AudioFile", audioFilePath);
            OpenAI_Plugin.VA_Proxy.Dictation.ClearBuffer(false, out string _);
        }

        /// <summary>
        /// Read the input WAV file and append its data to the output WAV file.
        /// </summary>
        /// <param name="waveFileWriter">The wave file writer used to write data to the output WAV file.</param>
        /// <param name="waveFormat">The wave format of the input and output WAV files.</param>
        /// <param name="inputFilePath">The file path to the input WAV file.</param>
        /// <exception cref="InvalidOperationException">Thrown when the input WAV file has a different format than the output WAV file.</exception>
        private static void WriteWavFiles(WaveFileWriter waveFileWriter, WaveFormat waveFormat, string inputFilePath)
        {
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

        /// <summary>
        /// Combines multiple input WAV files into a single output WAV file.
        /// </summary>
        /// <param name="inputFilePaths">A list of file paths to the input WAV files to be combined.</param>
        /// <exception cref="InvalidOperationException">Thrown when the input WAV files have different formats.</exception>
        public static void CombineWavFiles(List<string> inputFilePaths)
        {
            if (inputFilePaths.Count <= 1)
            {  
                return;
            }

            WaveFormat waveFormat = null;
            using (var waveFileReader = new WaveFileReader(inputFilePaths[0]))
            {
                waveFormat = waveFileReader.WaveFormat;
            }

            string fileName = Guid.NewGuid().ToString();
            string outputFilePath = System.IO.Path.Combine(Configuration.DEFAULT_AUDIO_FOLDER, fileName + ".wav");

            using (var waveFileWriter = new WaveFileWriter(outputFilePath, waveFormat))
            {
                foreach (var inputFilePath in inputFilePaths)
                {
                    WriteWavFiles(waveFileWriter, waveFormat, inputFilePath);
                }
            }

            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_AudioFile", outputFilePath);
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
        /// but that command does not exist, nor does the default example command, named in 'DEFAULT_DICTATION_STOP_COMMAND',
        /// and also the plugin method to generate and save Dictation audio to file have failed as well.
        /// </exception>
        public static string GetUserInput(bool generateAudioFile, bool sayPreListen)
        {
            OpenAI_Plugin.VA_Proxy.Dictation.ClearBuffer(false, out string _);

            bool stopListeningMode = OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_DisableListenMode") ?? false;
            string lastSpokenCMD = OpenAI_Plugin.VA_Proxy.ParseTokens("{LASTSPOKENCMD}");

            // Check if default OR user custom 'Dictation Start' command exists in VoiceAttack profile.
            string listenCommand = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_Command_DictationStart") ?? DEFAULT_DICTATION_START_COMMAND;
            if (!OpenAI_Plugin.VA_Proxy.Command.Exists(listenCommand) && OpenAI_Plugin.VA_Proxy.Command.Exists(DEFAULT_DICTATION_START_COMMAND))
            {
                listenCommand = DEFAULT_DICTATION_START_COMMAND;
            }

            // Run custom profile based listen method for this phase of GetInput.
            if (OpenAI_Plugin.VA_Proxy.Command.Exists(listenCommand))
            {
                // Execute command in VoiceAttack and wait here for it to complete.
                OpenAI_Plugin.VA_Proxy.Command.Execute(listenCommand, true);

                // Post-Dictation break check to end a chat session and exit if the 'stop chatting' command is detected.
                bool chatSession = OpenAI_Plugin.ChatActive ?? false;
                if (!chatSession || OpenAI_Plugin._stopVariableToMonitor)
                {
                    return string.Empty;
                }
            }

            // Set default OR user custom 'Dictation Stop' if command exists in VoiceAttack profile, or empty string if none.
            string dictationCommand = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_Command_DictationStop") ?? DEFAULT_DICTATION_STOP_COMMAND;
            if (!OpenAI_Plugin.VA_Proxy.Command.Exists(dictationCommand))
            {
                dictationCommand = OpenAI_Plugin.VA_Proxy.Command.Exists(DEFAULT_DICTATION_STOP_COMMAND) ? DEFAULT_DICTATION_STOP_COMMAND : string.Empty;
            }

            string userInput = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_UserInput") ?? string.Empty;
            if (string.IsNullOrEmpty(userInput))
            {
                userInput = OpenAI_Plugin.VA_Proxy.ParseTokens("{DICTATION}") ?? string.Empty;
            }

            // Returns early if custom profile DictationStart command had executed and set the userInput to be used.
            if (!string.IsNullOrEmpty(userInput) && OpenAI_Plugin.VA_Proxy.Command.Exists(listenCommand))
            {
                return userInput;
            }

            // Beginning default plugin DictationStart phase.
            if (sayPreListen && GetInputTimeout)
            {
                ChatGPT.ProvideFeedback("OpenAI_TTS_PreListen", true);
            }

            if (OpenAI_Plugin.VA_Proxy.Dictation.IsOn())
            {
                OpenAI_Plugin.VA_Proxy.Dictation.Stop();
                OpenAI_Plugin.VA_Proxy.Dictation.ClearBuffer(false, out string _);
            }

            if (!OpenAI_Plugin.VA_Proxy.Dictation.Start(out string _))
            {
                return userInput;
            }
            
            OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_Listening#", true);

            // Initial timeout allows speaker to form thoughts before speaking, converting seconds to MS if supplied.
            int initialTimeoutMs = OpenAI_Plugin.VA_Proxy.GetInt("OpenAI_ListenTimeout_InitialSeconds") ?? 0;
            initialTimeoutMs = initialTimeoutMs == 0
                ? 5000
                : initialTimeoutMs * 1000;

            bool isInitialTimeout = true;

            // Standard timeout is active after speaker begins, converting seconds to MS if supplied, and detects end of speech.
            int timeoutMs = OpenAI_Plugin.VA_Proxy.GetInt("OpenAI_ListenTimeout_Seconds") ?? 0;
            timeoutMs = timeoutMs == 0
                ? 1800
                : timeoutMs * 1000;

            int listenTimeoutMs = initialTimeoutMs;

            GetInputTimeout = false;

            DateTime startTime = DateTime.Now;
            DateTime initialStartTime = DateTime.Now;

            int intervalMs = 100;
            int writeInterval = 5000 / intervalMs;
            int index = 0;

            DictationAudioFileNames.Clear();
            string lastDictation = string.Empty;
            List<string> appendedDictation = new List<string>();

            while (OpenAI_Plugin.ListenActive == true && OpenAI_Plugin.VA_Proxy.Dictation.IsOn() && !OpenAI_Plugin._stopVariableToMonitor)
            {
                // Check if a non-session call was merely to an existing VoiceAttack command, and exit silently.
                if (OpenAI_Plugin.ChatActive != true && lastSpokenCMD != OpenAI_Plugin.VA_Proxy.ParseTokens("{LASTSPOKENCMD}"))
                {
                    OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_Error", true);
                    GetInputTimeout = true;
                    break;
                }

                string newDictation = OpenAI_Plugin.VA_Proxy.ParseTokens("{DICTATION}") ?? string.Empty;
                if (!string.IsNullOrEmpty(newDictation) && newDictation != lastDictation)
                {
                    lastDictation = newDictation;
                    appendedDictation.Add(newDictation);

                    if (generateAudioFile)
                    {
                        SaveCapturedAudio();
                    }
                }

                // Check if the timeout has been reached or active chat manually ended.
                bool notListening = OpenAI_Plugin.ListenActive ?? false;
                bool speechActive = OpenAI_Plugin.VA_Proxy.State.SpeechActive();
                bool timeoutReached = (DateTime.Now - startTime).TotalMilliseconds >= listenTimeoutMs;

                if (notListening || (!speechActive && timeoutReached))
                {
                    break;
                }

                // Modified from Pfeil's awesome 'Dictation until silence' post on VoiceAttack forums.
                bool initialTimeoutReached = (DateTime.Now - initialStartTime).TotalMilliseconds >= initialTimeoutMs;
                if (speechActive || (isInitialTimeout && initialTimeoutReached))
                {
                    isInitialTimeout = false;
                    listenTimeoutMs = timeoutMs;
                    startTime = DateTime.Now;
                }

                Thread.Sleep(intervalMs);

                index++;
                bool isDebugWriteInterval = index % writeInterval == 0;
                if (OpenAI_Plugin.DebugActive == true && isDebugWriteInterval)
                {
                    OpenAI_Plugin.VA_Proxy.WriteToLog("OpenAI ChatGPT is still waiting for user input to continue...", "yellow");
                }

            }

            if (!GetInputTimeout)
            {
                appendedDictation.Add(OpenAI_Plugin.VA_Proxy.ParseTokens("{DICTATION}") ?? string.Empty);
                userInput = string.Join(" ", appendedDictation).TrimEnd();
                GetInputTimeout = string.IsNullOrWhiteSpace(userInput) || OpenAI_Plugin.ListenActive != true;
            }

            if (OpenAI_Plugin.VA_Proxy.Dictation.IsOn())
            {
                OpenAI_Plugin.VA_Proxy.Dictation.Stop();
            }

            OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_Listening#", false);

            GetInputTimeout = GetInputTimeout || OpenAI_Plugin._stopVariableToMonitor;

            // Must ensure userInput is empty if timed out or manually ended.
            if (GetInputTimeout)
            {
                userInput = string.Empty;
            }

            if (stopListeningMode && GetInputTimeout)
            {
                OpenAI_Plugin.VA_Proxy.State.SetListeningEnabled(false);
            }

            if (string.IsNullOrEmpty(userInput) || OpenAI_Plugin._stopVariableToMonitor)
            {
                return userInput;
            }


            if (OpenAI_Plugin.VA_Proxy.Command.Exists(dictationCommand))
            {
                // Execute command in VoiceAttack and wait here for it to complete.
                OpenAI_Plugin.VA_Proxy.Command.Execute(dictationCommand, true);
            }
            else
            {
                if (generateAudioFile)
                {
                    SaveCapturedAudio();
                    CombineWavFiles(DictationAudioFileNames);
                }
            }

            OpenAI_Plugin._stopVariableToMonitor = false;
            return userInput;
        }

    }
}
