namespace OpenAI_NET.service
{
    /// <summary>
    /// A class for methods providing a means to log data to an external log file.
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
        private static readonly string DEFAULT_LOG_FOLDER = Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                            "OpenAI_VoiceAttack_Plugin");
        private static string ERROR_LOG_PATH { get; set; } = Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                                            DEFAULT_LOG_NAME);

        /// <summary>
        /// A method to set the path for error logging during this session.
        /// </summary>
        public static void SetErrorLogPath()
        {
            try
            {
                // Use the default AppData Roaming folder with a sub-folder if an exception occurs
                if (Directory.Exists(DEFAULT_LOG_FOLDER))
                {
                    ERROR_LOG_PATH = Path.Combine(DEFAULT_LOG_FOLDER, DEFAULT_LOG_NAME + ".log");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAI_NET Error: {ex.Message}");
            }
        }

        private static bool CheckAndRotateErrorsLog(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    FileInfo? file = new(path);
                    if (file.Length > LOG_MAX_BYTES)
                    {
                        string timestamp = DateTime.Now.ToString("MMddyyyyHHmmss");
                        string newFilePath = System.IO.Path.Combine(DEFAULT_LOG_FOLDER, DEFAULT_LOG_NAME + "_" + timestamp + ".log");
                        File.Copy(path, newFilePath);
                        File.Delete(path);
                    }
                    file = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAI_NET Error: Error rotating oversized errors log: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// A method for logging error messages to file.
        /// </summary>
        /// <param name="logMessage">The message to be appended to the log file.</param>
        public static void WriteToLogFile(string logMessage)
        {
            try
            {
                if (CheckAndRotateErrorsLog(ERROR_LOG_PATH))
                {
                    using StreamWriter writer = new(ERROR_LOG_PATH, true);
                    writer.WriteLine($"OpenAI_NET Error at {DateTime.Now}:");
                    writer.WriteLine(logMessage);
                    writer.WriteLine("==========================================================================");
                    writer.WriteLine(string.Empty);
                }
                else
                {
                    throw new Exception("OpenAI_NET.Logging.CheckAndRotateErrorsLog() returned false");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAI_NET Error: unable to write to errors log file! Log Message: {logMessage}  Failure Reason:{ex.Message}");
            }
        }

    }
}
