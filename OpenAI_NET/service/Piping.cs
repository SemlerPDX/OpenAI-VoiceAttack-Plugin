using System.IO.Pipes;

namespace OpenAI_NET.service
{
    /// <summary>
    /// A class providing methods for interprocess communications
    /// between this application and the OpenAI VoiceAttack Plugin.
    /// </summary>
    /// <para>
    /// <br>OpenAI_NET App for the OpenAI API VoiceAttack Plugin</br>
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
        private static readonly string PipeNameTHIS = "OpenAI_NET";
        private static readonly string PipeNameIN = "OpenAI_NET_Pipe";
        private static readonly string PipeNameOUT = "OpenAI_Plugin_Pipe";

        /// <summary>
        /// A method to pipe a function response to the OpenAI VoiceAttack Plugin.
        /// </summary>
        /// <param name="args">A string array containing the response of a function request.</param>
        /// <returns>True upon success, false if otherwise.</returns>
        /// <exception cref="IOException">Thrown when an error occurs in WriteLine().</exception>
        /// <exception cref="ObjectDisposedException">Thrown when an error occurs in WriteLine().</exception>
        /// <exception cref="InvalidOperationException">Thrown when an error occurs in Connect().</exception>
        public static bool SendArgsToNamedPipe(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentException($"The {PipeNameTHIS} arguments array must contain at least one element.", nameof(args));
            }

            try
            {
                // Send the return args over the named pipe back to the OpenAI VoiceAttack Plugin
                using NamedPipeClientStream pipeClient = new(".", PipeNameOUT, PipeDirection.Out);
                if (!pipeClient.IsConnected)
                    pipeClient.Connect();

                using StreamWriter writer = new(pipeClient);
                foreach (string arg in args)
                {
                    if (!String.IsNullOrEmpty(arg))
                        writer.WriteLine(arg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{PipeNameTHIS} Error: SendArgsToNamedPipe Exception occurred: {ex.Message}");
            }
            return true;
        }

        /// <summary>
        /// A method to listen for piped function requests from the OpenAI VoiceAttack Plugin.
        /// </summary>
        /// <returns>A string array starting with the desired function, followed by the OpenAI API Key,
        /// then any required parameters (see documentation).</returns>
        /// <exception cref="OutOfMemoryException">Thrown when an error occurs in ReadLine().</exception>
        /// <exception cref="IOException">Thrown when an error occurs in WaitForConnection() or ReadLine().</exception>
        /// <exception cref="ObjectDisposedException">Thrown when an error occurs in WaitForConnection() or Disconnect().</exception>
        /// <exception cref="InvalidOperationException">Thrown when an error occurs in WaitForConnection() or Disconnect().</exception>
        public static string[] ListenForArgsOnNamedPipe()
        {
            List<string> args = new();
            try
            {
                // Listen for function args over the named pipe from the OpenAI VoiceAttack Plugin
                using NamedPipeServerStream pipeServer = new(PipeNameIN, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances);
                pipeServer.WaitForConnection();

                using (StreamReader reader = new(pipeServer))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null && !String.IsNullOrEmpty(line))
                    {
                        args.Add(line);
                    }
                }
                pipeServer.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{PipeNameTHIS} Error: ListenForArgsOnNamedPipe Exception occurred: {ex.Message}");
            }

            return args.ToArray();
        }

    }
}
