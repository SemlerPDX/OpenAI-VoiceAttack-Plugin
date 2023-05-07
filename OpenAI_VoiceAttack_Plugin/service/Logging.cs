using System;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;

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
        private static readonly long LOG_MAX_BYTES = 104857600L; // 100 MB max log size
        private static readonly string DEFAULT_LOG_NAME = "openai_errors";
        private static string ERROR_LOG_PATH { get; set; } = Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                                            DEFAULT_LOG_NAME + ".log");

        /// <summary>
        /// A method to write very long messages to the VoiceAttack Event Log, wrapping text
        /// which exceeds the minimum width of the window to a new event log entry on a new line.
        /// </summary>
        /// <param name="longString">The text string containing the message to write to the Event Log without truncation.</param>
        /// <param name="colorString">The color of the square to the left of each Event Log entry.</param>
        public static void WriteToLog_Long(string longString, string colorString)
        {
            try
            {
                int maxWidth = 91;
                string[] lines = longString.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    if (line.Length <= maxWidth)
                    {
                        OpenAIplugin.VA_Proxy.WriteToLog(line, colorString);
                        continue;
                    }

                    int index = 0;
                    while (index < line.Length)
                    {
                        try
                        {
                            int length = Math.Min(maxWidth, line.Length - index);

                            if (length == maxWidth)
                            {
                                // Check if the line should wrap at a whitespace
                                int lastSpaceIndex = line.LastIndexOf(' ', index + length, length);
                                if (lastSpaceIndex != -1 && lastSpaceIndex != index + length)
                                {
                                    length = lastSpaceIndex - index;
                                }
                            }

                            OpenAIplugin.VA_Proxy.WriteToLog(line.Substring(index, length), colorString);
                            index += length;

                            // Ignore whitespace at the beginning of a new line
                            while (index < line.Length && char.IsWhiteSpace(line[index]))
                            {
                                index++;
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Catch ArgumentOutOfRangeException and continue the loop
                            if (OpenAIplugin.DEBUG_ACTIVE == true)
                                OpenAIplugin.VA_Proxy.WriteToLog("OpenAI Plugin Error: ArgumentOutOfRangeException caught at WriteToLog_Long() method", "red");

                            continue;
                        }
                    }
                }
            }
            catch
            {
                if (OpenAIplugin.DEBUG_ACTIVE == true)
                    OpenAIplugin.VA_Proxy.WriteToLog("OpenAI Plugin Error: Failure ignored at WriteToLog_Long() method", "red");
            }
        }

        /// <summary>
        /// A method to set the path for error logging during this session.
        /// </summary>
        public static void SetErrorLogPath()
        {
            try
            {
                // Use the default AppData Roaming folder with a sub-folder if an exception occurs
                if (Directory.Exists(Configuration.DEFAULT_CONFIG_FOLDER))
                {
                    ERROR_LOG_PATH = Path.Combine(Configuration.DEFAULT_CONFIG_FOLDER, DEFAULT_LOG_NAME + ".log");
                }
            }
            catch (Exception ex)
            {
                WriteToLog_Long($"OpenAI Plugin Error: {ex.Message}", "red");
            }
            finally
            {
                OpenAIplugin.VA_Proxy.SetText("OpenAI_Plugin_ErrorsLogPath", ERROR_LOG_PATH);
            }
        }

        private static bool CheckAndRotateErrorsLog(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    FileInfo file = new FileInfo(path);
                    if (file.Length > LOG_MAX_BYTES)
                    {
                        string timestamp = DateTime.Now.ToString("MMddyyyyHHmmss");
                        string newFilePath = System.IO.Path.Combine(Configuration.DEFAULT_CONFIG_FOLDER, DEFAULT_LOG_NAME + "_" + timestamp + ".log");
                        File.Copy(path, newFilePath);
                        File.Delete(path);
                    }
                    file = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteToLog_Long($"OpenAI Plugin Error: Error rotating oversized errors log: {ex.Message}", "red");
                return false;
            }
        }

        /// <summary>
        /// A method for logging OpenAI Plugin error messages to the errors log file.
        /// <br />
        /// <br />Default Path:
        /// <br />"%AppData%\Roaming\OpenAI_VoiceAttack_Plugin\openai_errors.log"
        /// </summary>
        /// <param name="logMessage">The message to be appended to the log file.</param>
        public static void WriteToLogFile(string logMessage)
        {
            try
            {
                if (CheckAndRotateErrorsLog(ERROR_LOG_PATH))
                {
                    using (StreamWriter writer = new StreamWriter(ERROR_LOG_PATH, true))
                    {
                        writer.WriteLine("==========================================================================");
                        writer.WriteLine($"OpenAI Plugin Error at {DateTime.Now}:");
                        writer.WriteLine(logMessage);
                        writer.WriteLine("==========================================================================");
                        writer.WriteLine(string.Empty);
                    }
                }
                else
                {
                    throw new Exception("OpenAIplugin.Logging.CheckAndRotateErrorsLog() returned false");
                }
            }
            catch (Exception ex)
            {
                WriteToLog_Long($"OpenAI Plugin Error: unable to write to errors log file! Log Message: {logMessage}  Failure Reason:{ex.Message}", "red");
            }
        }

    }
}
