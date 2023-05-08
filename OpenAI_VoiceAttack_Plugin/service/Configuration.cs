using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Security.Principal;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing methods for plugin configuration initialization, creating the required
    /// default operational folders for this plugin at "%AppData%\Roaming\OpenAI_VoiceAttack_Plugin"
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
    public static class Configuration
    {
        /// <summary>
        /// The folder path to the configuration folder containing OpenAI VoiceAttack Plugin files and folders.
        /// <br />
        /// <br />Default Path:
        /// <br />"%AppData%\Roaming\OpenAI_VoiceAttack_Plugin"
        /// </summary>
        public static string DEFAULT_CONFIG_FOLDER = System.IO.Path.Combine(
                                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                        "OpenAI_VoiceAttack_Plugin");

        /// <summary>
        /// The folder path to the captured audio files used for OpenAI Whisper
        /// </summary>
        public static readonly string DEFAULT_AUDIO_FOLDER = System.IO.Path.Combine(DEFAULT_CONFIG_FOLDER, "whisper");

        /// <summary>
        /// The default file path to the captured audio file to use for OpenAI Whisper
        /// </summary>
        public static readonly string DEFAULT_AUDIO_PATH = System.IO.Path.Combine(DEFAULT_AUDIO_FOLDER, "dictation_audio.wav");

        /// <summary>
        /// Method to check if application has permission to write file at path specified.
        /// </summary>
        /// <param name="filePath">File path to check write permissions for.</param>
        /// <returns>True if has permissions, false if otherwise.</returns>
        /// <exception cref="SecurityException">
        /// Thrown when security error is detected. (returns false)
        /// </exception>
        private static bool OperationHasClearance(string filePath)
        {
            try
            {

                // Check if the application has elevated permissions
                bool hasElevatedPermissions = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                if (!hasElevatedPermissions) { return false; }

                FileIOPermission permission = new FileIOPermission(FileIOPermissionAccess.Write, filePath);
                permission.Demand();
                return true;
            }
            catch (SecurityException ex)
            {
                if (OpenAIplugin.DEBUG_ACTIVE == true)
                    Logging.WriteToLog_Long($"The plugin has insufficient permissions for file operations: {ex.Message}", "red");

                return false;
            }
        }

        /// <summary>
        /// A method to present a message box informing users to restart VoiceAttack following first time installation of shared assemblies.
        /// </summary>
        private static void ShowInstallationCompleteMessage()
        {
            System.Windows.MessageBox.Show("OpenAI Plugin for VoiceAttack has completed first time installation.\n" +
                "\n" +
                " You must now restart VoiceAttack one final time." +
                " You may also disable 'Run as Admin' now, if you prefer.",
                OpenAIplugin.VA_DisplayName(),
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// The OpenAI Plugin requires the following files in the Shared\Assemblies folder of VoiceAttack, run only on first time installation.<br />
        /// This method ensures these files are present before the Plugin can be used, informing users to restart one last time when complete.<br />
        /// <br />Will set <see cref="OpenAIplugin.OpenAI_PluginFirstUse"/> to <see langword="True"/> if these assembly files were not already in shared assemblies folder:
        /// <br /><br />
        /// "Microsoft.Extensions.Options.dll"<br />
        /// "Microsoft.Extensions.Http.dll"<br />
        /// "Microsoft.Extensions.Primitives.dll"<br />
        /// "System.Threading.Tasks.Extensions.dll"<br />
        /// </summary>
        public static bool CheckSharedAssemblies()
        {
            string sourcePath;
            string destinationPath;
            string rootFolder = OpenAIplugin.VA_Proxy.AppsDir;
            string sharedFolder = OpenAIplugin.VA_Proxy.AssembliesDir;

            CreateNewDirectory(sharedFolder);

            // Array of required shared assemblies for OpenAI Plugin for VoiceAttack
            bool filesMoved = false;
            string[] assemblyNames = new[]
            {
                "Microsoft.Bcl.AsyncInterfaces.dll",
                "Microsoft.Extensions.Options.dll",
                "Microsoft.Extensions.Http.dll",
                "Microsoft.Extensions.Primitives.dll",
                "System.Threading.Tasks.Extensions.dll"
            };

            // Check for and/or move files to Shared\Assemblies folder
            foreach (string assemblyName in assemblyNames)
            {
                sourcePath = Path.Combine(rootFolder, "OpenAI_Plugin", "shared", assemblyName);
                destinationPath = Path.Combine(sharedFolder, assemblyName);

                if (!File.Exists(destinationPath))
                {
                    if (OperationHasClearance(sharedFolder))
                    {
                        filesMoved = true;
                        File.Copy(sourcePath, destinationPath);
                    }
                    else
                    {
                        OpenAIplugin.VA_Proxy.WriteToLog("OpenAI Plugin Error: Must run VoiceAttack 'as Admin' for first time installation - see wiki!", "red");
                        return true;
                    }
                }
            }

            // Inform of required restart only if files were added to Shared\Assemblies folder
            if (filesMoved)
            {
                OpenAIplugin.OpenAI_PluginFirstUse = true;
                ShowInstallationCompleteMessage();
            }

            return filesMoved;
        }

        /// <summary>
        /// A method to delete the old dictation audio files after use, because the action which
        /// writes them cannot overwrite, and a new audio file is created each time, requiring cleanup.
        /// </summary>
        /// <returns>True when no audio folder exists to contain files, false if otherwise.</returns>
        public static bool DeleteOldDictationAudio()
        {
            try
            {
                if (Directory.Exists(DEFAULT_AUDIO_FOLDER))
                {
                    string[] files = Directory.GetFiles(DEFAULT_AUDIO_FOLDER);
                    if (files.Length > 0)
                    {
                        foreach (string file in files)
                        {
                            if (file.EndsWith(".wav") && OperationHasClearance(file))
                                File.Delete(file);
                        }
                    }
                    return false;
                }
            }
            catch
            {
                // ...let it slide, the plugin command also runs this in an inline function when the call ends
            }
            return true;
        }

        /// <summary>
        /// Create the folders required for OpenAI Plugin including the configuration and whisper folders
        /// </summary>
        /// <param name="directoryPath">The path of the folder to be created.</param>
        /// <exception cref="Exception">Thrown when folder does not exist after attempt, or has insufficient permission to create, or other errors.</exception>
        private static void CreateNewDirectory(string directoryPath)
        {
            try
            {
                // Check if the folder path exists
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                    if (!System.IO.Directory.Exists(directoryPath))
                    {
                        throw new Exception($"Folder does not exist after attempting to create it at path: {directoryPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failure to create new directory: {ex.Message}");
            }
        }

        /// <summary>
        /// A method to create the required operational folders for this plugin to save captured audio files used in Whisper.
        /// </summary>
        /// <param name="isFirstRun">A boolean indicating if this method should try creating config folders on first run.</param>
        /// <exception cref="Exception">Thrown if unabled to create required config folders - plugin will not be initialized.</exception>
        public static void CreateConfigFolders(bool isFirstRun)
        {
            try
            {
                string whisperFolder = System.IO.Path.Combine(DEFAULT_CONFIG_FOLDER, "whisper");

                // Try to create required config folders in AppData Roaming, throwing exceptions on failure
                if (isFirstRun)
                {
                    CreateNewDirectory(DEFAULT_CONFIG_FOLDER);
                    CreateNewDirectory(whisperFolder);
                }
                OpenAIplugin.VA_Proxy.SetText("OpenAI_Plugin_ConfigFolderPath", DEFAULT_CONFIG_FOLDER);
                OpenAIplugin.VA_Proxy.SetText("OpenAI_Plugin_WhisperFolderPath", whisperFolder);
            }
            catch (Exception ex)
            {
                OpenAIplugin.VA_Proxy.SetBoolean("OpenAI_Plugin_Initialized", false);
                throw new Exception($"Failure creating Config Folders in AppData Roaming! {ex.Message}");
            }
        }

    }
}
