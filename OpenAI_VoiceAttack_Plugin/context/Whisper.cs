using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        public static readonly string DefaultErrorMessage = "Error processing audio file.";


        /// <summary>
        /// Get user input by post-processing Dictation Audio through the Whisper API using translation or transcription into English text.
        /// </summary>
        /// <param name="operation">The type of Whisper operation to perform, either "transcription" or "translation".</param>
        /// <returns>The returned text from Whisper, or an empty string if error has occurred.</returns>
        public static async Task<string> GetUserInputAsync(string operation)
        {
            string userInput = string.Empty;

            await ProcessAudio(operation);

            if (OpenAI_Plugin.VA_Proxy.GetBoolean("OpenAI_Error") != true)
            {
                userInput = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_Response") ?? string.Empty;
                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_UserInput", userInput);
            }

            return userInput;
        }

        /// <summary>
        /// Transcribe audio into text using OpenAI Whisper.
        /// <br /><br />
        /// Returns are set to the VA text variable 'OpenAI_Response' as a string that contains
        /// the transcribed text if successful, or an error message if the operation fails.
        /// </summary>
        public static Task ProcessAudio() { return ProcessAudio("transcribe"); }
        /// <summary>
        /// Transcribe audio into text or Translate non-English audio into English text using OpenAI Whisper.
        /// <br /><br />
        /// Returns are set to the VA text variable 'OpenAI_Response' as a string that contains
        /// the transcribed text if successful, or an error message if the operation fails.
        /// </summary>
        /// <param name="operation">The type of operation to perform, either "transcribe" or "translate".</param>
        public static Task ProcessAudio(string operation)
        {
            string audioFilePath = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_AudioPath")
                                    ?? (OpenAI_Plugin.VA_Proxy.GetText("OpenAI_AudioFile")
                                        ?? Configuration.DEFAULT_AUDIO_PATH);

            string response = DefaultErrorMessage;
            
            // Default is 5 second timeout for audio file to appear.
            int timeoutMs = 5000;
            int intervalMs = 100;
            DateTime startTime = DateTime.Now;

            while (!System.IO.File.Exists(audioFilePath))
            {
                if ((DateTime.Now - startTime).TotalMilliseconds >= timeoutMs)
                {
                    throw new Exception("Timeout - Dictation Audio File not found!");
                }

                Thread.Sleep(intervalMs);
            }

            List<string> args = new List<string>
            {
                operation,
                OpenAI_Key.ApiKey,
                audioFilePath
            };

            if (!Piping.SendArgsToNamedPipe(args.ToArray()))
            {
                throw new Exception($"Failed to pipe {operation} request to OpenAI_NET App!");
            }

            string[] responses = Piping.ListenForArgsOnNamedPipe();

            bool isError = (responses == null || string.IsNullOrEmpty(responses[0]));
            if (!isError)
            {
                response = responses[0];
                isError = response.StartsWith("OpenAI_NET");
            }

            OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_Error", isError);
            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Response", response);

            return Task.CompletedTask;
        }

    }
}
