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
        private static readonly string PIPE_NAME_THIS = "OpenAI_NET";
        private static readonly string PIPE_NAME_IN = "OpenAI_NET_Pipe";
        private static readonly string PIPE_NAME_OUT = "OpenAI_Plugin_Pipe";

        private static void WriteLines(NamedPipeClientStream pipeClient, string[] args)
        {
            using (StreamWriter writer = new StreamWriter(pipeClient))
            {
                foreach (string arg in args)
                {
                    writer.WriteLine(arg);
                }
            }
        }

        /// <summary>
        /// A method to pipe a function response to the OpenAI VoiceAttack Plugin.
        /// </summary>
        /// <param name="args">A string array containing the response of a function request.</param>
        /// <returns>True upon success, false if otherwise.</returns>
        public static bool SendArgsToNamedPipe(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentException($"The {PIPE_NAME_THIS} arguments array must contain at least one element.", nameof(args));
            }

            using (NamedPipeClientStream pipeClient = new(".", PIPE_NAME_OUT, PipeDirection.Out))
            {
                if (!pipeClient.IsConnected)
                {
                    pipeClient.Connect();
                }

                WriteLines(pipeClient, args);
            }

            return true;
        }

        private static List<string> ReadLines(NamedPipeServerStream pipeServer, List<string> args)
        {
            using (StreamReader reader = new StreamReader(pipeServer))
            {
                string line;
                while ((line = reader.ReadLine() ?? string.Empty) != null && !string.IsNullOrEmpty(line))
                {
                    args.Add(line);
                }
            }

            return args;
        }

        /// <summary>
        /// A method to listen for piped function requests from the OpenAI VoiceAttack Plugin.
        /// </summary>
        /// <returns>A string array starting with the desired function, followed by the OpenAI API Key,
        /// then any required parameters (see documentation).</returns>
        public static string[] ListenForArgsOnNamedPipe()
        {
            List<string> args = new List<string>();

            using (NamedPipeServerStream pipeServer = new(PIPE_NAME_IN, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances))
            {
                pipeServer.WaitForConnection();

                ReadLines(pipeServer, args);
            }

            return args.ToArray();
        }

    }
}
