using System;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class for methods providing a means to log data to the VoiceAttack Event Log, or elsewhere.
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
    public static class Logging
    {
        private static readonly int MAX_WIDTH = 91;
        private static readonly long LOG_MAX_BYTES = 104857600L;
        private static readonly string DEFAULT_LOG_NAME = "openai_errors";

        private static string ErrorLogPath { get; set; } = Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                                            DEFAULT_LOG_NAME + ".log");

        private static int WriteLine(string line, string color, int index)
        {
            int length = Math.Min(MAX_WIDTH, line.Length - index);

            if (length == MAX_WIDTH)
            {
                // Check if the line should wrap at a whitespace
                int lastSpaceIndex = line.LastIndexOf(' ', index + length, length);
                if (lastSpaceIndex != -1 && lastSpaceIndex != index + length)
                {
                    length = lastSpaceIndex - index;
                }
            }

            OpenAI_Plugin.VA_Proxy.WriteToLog(line.Substring(index, length), color);
            index += length;

            // Ignore whitespace at the beginning of a new line
            while (index < line.Length && char.IsWhiteSpace(line[index]))
            {
                index++;
            }

            return index;
        }

        private static void WriteLines(string[] lines, string color)
        {
            foreach (string line in lines)
            {
                if (line.Length <= MAX_WIDTH)
                {
                    OpenAI_Plugin.VA_Proxy.WriteToLog(line, color);
                    continue;
                }

                int index = 0;
                while (index < line.Length)
                {
                    index = WriteLine(line, color, index);
                }
            }
        }

        /// <summary>
        /// A method to write very long messages to the VoiceAttack Event Log, wrapping text
        /// which exceeds the minimum width of the window to a new event log entry on a new line.
        /// </summary>
        /// <param name="message">The text string containing the message to write to the Event Log without truncation.</param>
        /// <param name="color">The color of the square to the left of each Event Log entry.</param>
        public static void WriteToLog_Long(string message, string color)
        {
            try
            {
                string[] lines = message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                WriteLines(lines, color);
            }
            catch
            {
                if (OpenAI_Plugin.DebugActive == true)
                {
                    OpenAI_Plugin.VA_Proxy.WriteToLog("OpenAI Plugin Errors: Failure ignored at WriteToLog_Long() method", "red");
                }
            }
        }

        /// <summary>
        /// A method to set the path for error logging during this session.
        /// </summary>
        public static void SetErrorLogPath()
        {
            // Uses the default CommonDocuments "public" folder if AppData Roaming config folder does not exist
            if (Directory.Exists(Configuration.DEFAULT_CONFIG_FOLDER))
            {
                ErrorLogPath = Path.Combine(Configuration.DEFAULT_CONFIG_FOLDER, DEFAULT_LOG_NAME + ".log");
            }

            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Plugin_ErrorsLogPath", ErrorLogPath);
        }

        private static void CheckAndRotateErrorsLog(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            FileInfo file = new FileInfo(path);
            if (file.Length < LOG_MAX_BYTES)
            {
                return;
            }

            string timestamp = DateTime.Now.ToString("MMddyyyyHHmmss");
            string newFilePath = System.IO.Path.Combine(Configuration.DEFAULT_CONFIG_FOLDER, DEFAULT_LOG_NAME + "_" + timestamp + ".log");
            File.Copy(path, newFilePath);
            File.Delete(path);
        }

        /// <summary>
        /// A method for logging OpenAI Plugin error messages to the errors log file.
        /// <br />
        /// <br />Default Path:
        /// <br />"%AppData%\Roaming\OpenAI_VoiceAttack_Plugin\openai_errors.log"
        /// </summary>
        /// <param name="message">The message to be appended to the log file.</param>
        public static void WriteToLogFile(string message)
        {
            try
            {
                CheckAndRotateErrorsLog(ErrorLogPath);

                using (StreamWriter writer = new StreamWriter(ErrorLogPath, true))
                {
                    writer.WriteLine("==========================================================================");
                    writer.WriteLine($"OpenAI Plugin Error at {DateTime.Now}:");
                    writer.WriteLine(message);
                    writer.WriteLine("==========================================================================");
                    writer.WriteLine(string.Empty);
                }
            }
            catch (Exception ex)
            {
                WriteToLog_Long($"OpenAI Plugin Errors: unable to write to errors log file! Log Message: {message}  Failure Reason:{ex.Message}", "red");
            }
        }

    }
}
