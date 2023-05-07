using System;
using System.Collections.Generic;
using System.Threading;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// Provides access to methods for transcribing or translating audio into text using OpenAI by sending
    /// and receiving data through the OpenAI_NET App which provides access to its Whisper API.
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
    public static class Whisper
    {
        /// <summary>
        /// A default error message that can be set or checked against a response to communicate issues.
        /// </summary>
        public static readonly string DEFAULT_ERROR_MESSAGE = "Error processing audio file.";


        /// <summary>
        /// Get user input by post-processing Dictation Audio through the Whisper API using translation or transcription into English text.
        /// </summary>
        /// <param name="operation">The type of Whisper operation to perform, either "transcription" or "translation".</param>
        /// <returns>The returned text from Whisper, or an empty string if error has occurred.</returns>
        public static string GetUserInput(string operation)
        {
            string userInput = String.Empty;
            ProcessAudio(operation);

            if (OpenAIplugin.VA_Proxy.GetBoolean("OpenAI_Error") != true)
            {
                OpenAIplugin.VA_Proxy.SetText("OpenAI_UserInput", OpenAIplugin.VA_Proxy.GetText("OpenAI_Response") ?? String.Empty);
                userInput = OpenAIplugin.VA_Proxy.GetText("OpenAI_UserInput") ?? String.Empty;
            }
            return userInput;
        }

        /// <summary>
        /// Transcribe audio into text using OpenAI Whisper.
        /// </summary>
        /// <returns>
        /// Sets the OpenAI_Response text variable to a string that contains the transcribed if successful,
        /// or an error message if the operation fails.</returns>
        public static void ProcessAudio() { ProcessAudio("transcribe"); }
        /// <summary>
        /// Transcribe audio into text or Translate non-English audio into English text using OpenAI Whisper.
        /// </summary>
        /// <param name="operation">The type of operation to perform, either "transcribe" or "translate".</param>
        /// <returns>
        /// Sets the OpenAI_Response text variable to a string that contains the transcribed or translated text if successful,
        /// or an error message if the operation fails.</returns>
        public static void ProcessAudio(string operation)
        {
            try
            {
                string audioFilePath = OpenAIplugin.VA_Proxy.GetText("OpenAI_AudioPath") ?? (OpenAIplugin.VA_Proxy.GetText("OpenAI_AudioFile") ?? Configuration.DEFAULT_AUDIO_PATH);
                string response = DEFAULT_ERROR_MESSAGE;
                bool isError = false;


                // Ensure dictation audio file exists
                int timeoutMs = 5000; // 5 seconds
                int intervalMs = 100; // 100 milliseconds
                DateTime startTime = DateTime.Now;
                while (!System.IO.File.Exists(audioFilePath))
                {
                    // Check if the timeout has been reached
                    if ((DateTime.Now - startTime).TotalMilliseconds >= timeoutMs)
                    {
                        throw new Exception("Dictation Audio File not found!");
                    }

                    Thread.Sleep(intervalMs);
                }


                List<string> args = new List<string>
                {
                    operation,
                    OpenAI_Key.API_KEY,
                    audioFilePath
                };

                // Send the function request to the OpenAI_NET App
                if (!Piping.SendArgsToNamedPipe(args.ToArray()))
                {
                    throw new Exception($"Failed to send {operation} request through pipe!");
                }

                // Listen for the response from the OpenAI_NET App
                string[] responses = Piping.ListenForArgsOnNamedPipe();
                if (responses != null && !String.IsNullOrEmpty(responses[0]))
                {
                    response = responses[0];
                    if (response.StartsWith("OpenAI_NET"))
                    {
                        isError = true;
                    }
                }
                else
                {
                    isError = true;
                }

                // Set the error flag if necessary
                if (isError)
                {
                    OpenAIplugin.VA_Proxy.SetBoolean("OpenAI_Error", true);
                }

                // Set the response to the VoiceAttack text variable and exit
                OpenAIplugin.VA_Proxy.SetText("OpenAI_Response", response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Whisper Error: {ex.Message}");
            }
        }

    }
}
