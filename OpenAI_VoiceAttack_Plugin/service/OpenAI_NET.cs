using System;
using System.Diagnostics;
using System.IO;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing methods for launching the OpenAI_NET companion app which
    /// provides Whisper and Dall-E interprocess functions to this VoiceAttack Plugin.
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
    public static class OpenAI_NET
    {

        /// <summary>
        /// Check if the OpenAI_NET companion app is running and therefore listening for Whisper and DALL-E requests.
        /// </summary>
        /// <returns>True if the OpenAI_NET.exe is running, false if otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown when an error occurs in GetProcessesByName().</exception>
        public static bool IsRunningOpenAI_NET()
        {
            Process[] processes = Process.GetProcessesByName("OpenAI_NET");
            return (processes.Length > 0);
        }

        /// <summary>
        /// Launch the OpenAI_NET companion app, which listens for Whisper and DALL-E requests.
        /// </summary>
        public static void LaunchOpenAI_NET()
        {
            string filePath = System.IO.Path.Combine(OpenAI_Plugin.VA_Proxy.AppsDir, @"OpenAI_Plugin\OpenAI_NET.exe");
            Process.Start(filePath);
        }

        /// <summary>
        /// Terminate the OpenAI_NET.exe process(es) or fail silently.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when an error occurs in GetProcessesByName() or Kill().</exception>
        /// <exception cref="NotSupportedException">Thrown when an error occurs in Kill().</exception>
        public static void KillOpenAI_NET()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("OpenAI_NET");
                foreach (Process process in processes)
                {
                    process.Kill();
                }
            }
            catch
            {
                //...let it slide and get out of Dodge
            }
        }

    }
}
