using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing methods for interprocess communications
    /// between this VoiceAttack Plugin and the OpenAI_NET application.
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
    public static class Piping
    {
        private static readonly string PipeNameTHIS = "OpenAI Plugin";
        private static readonly string PipeNameIN = "OpenAI_Plugin_Pipe";
        private static readonly string PipeNameOUT = "OpenAI_NET_Pipe";

        /// <summary>
        /// A method to pipe a function request to the OpenAI_NET application,
        /// which offers access to OpenAI Whisper and Dall-E Image editing/variation.
        /// </summary>
        /// <param name="args">A string array starting with the desired function,
        /// followed by the OpenAI API Key (and optional organization id), then any required parameters (see documentation).</param>
        /// <returns>True upon success, false if otherwise.</returns>
        /// <exception cref="IOException">Thrown when an error occurs in WriteLine().</exception>
        /// <exception cref="ObjectDisposedException">Thrown when an error occurs in WriteLine().</exception>
        /// <exception cref="InvalidOperationException">Thrown when an error occurs in Connect().</exception>
        public static bool SendArgsToNamedPipe(string[] args)
        {
            if (args == null || args.Length == 0 || args.Length < 3)
            {
                throw new Exception($"The {PipeNameTHIS} arguments array must contain at least three elements.");
            }

            try
            {
                // Attach the Organization ID to the args if set
                if (!String.IsNullOrEmpty(OpenAI_Key.API_ORG))
                {
                    args[1] = $"{args[1]}:{OpenAI_Key.API_ORG}";
                }

                // Send the args over the named pipe to the OpenAI_NET companion app
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeNameOUT, PipeDirection.Out))
                {
                    if (!pipeClient.IsConnected)
                        pipeClient.Connect();

                    using (StreamWriter writer = new StreamWriter(pipeClient))
                    {
                        foreach (string arg in args)
                        {
                            if (!String.IsNullOrEmpty(arg))
                                writer.WriteLine(arg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (OpenAIplugin.DEBUG_ACTIVE == true)
                    Logging.WriteToLog_Long($"{PipeNameTHIS} Error: SendArgsToNamedPipe Exception occurred: {ex.Message}", "red");
            }
            return true;
        }

        /// <summary>
        /// A method to listen for piped function responses from the OpenAI_NET application,
        /// which offers access to OpenAI Whisper and Dall-E Image editing/variation.
        /// </summary>
        /// <returns>A string array starting with the requested function response,
        /// followed by additional response items (if any) (see documentation).</returns>
        /// <exception cref="OutOfMemoryException">Thrown when an error occurs in ReadLine().</exception>
        /// <exception cref="IOException">Thrown when an error occurs in WaitForConnection() or ReadLine().</exception>
        /// <exception cref="ObjectDisposedException">Thrown when an error occurs in WaitForConnection() or Disconnect().</exception>
        /// <exception cref="InvalidOperationException">Thrown when an error occurs in WaitForConnection() or Disconnect().</exception>
        public static string[] ListenForArgsOnNamedPipe()
        {
            List<string> args = new List<string>();
            try
            {
                // Listen for return args over the named pipe from the OpenAI_NET companion app
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeNameIN, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances))
                {

                    pipeServer.WaitForConnection();

                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        string line;
                        while ((line = reader.ReadLine() ?? String.Empty) != null && !String.IsNullOrEmpty(line))
                        {
                            args.Add(line);
                        }
                    }

                    pipeServer.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (OpenAIplugin.DEBUG_ACTIVE == true)
                    Logging.WriteToLog_Long($"{PipeNameTHIS} Error: ListenForArgsOnNamedPipe Exception occurred: {ex.Message}", "red");
            }

            return args.ToArray();
        }

    }
}
