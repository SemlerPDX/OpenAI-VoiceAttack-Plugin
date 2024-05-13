using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Security.Principal;
using System.Linq;

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
        public static readonly string DEFAULT_CONFIG_FOLDER = System.IO.Path.Combine(
                                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                        "OpenAI_VoiceAttack_Plugin");

        /// <summary>
        /// The folder path to the captured audio files used for OpenAI Whisper.
        /// </summary>
        public static readonly string DEFAULT_AUDIO_FOLDER = System.IO.Path.Combine(DEFAULT_CONFIG_FOLDER, "whisper");

        /// <summary>
        /// The default file path to the captured audio file to use for OpenAI Whisper.
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
                bool hasElevatedPermissions = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                if (!hasElevatedPermissions)
                {
                    return false;
                }

                FileIOPermission permission = new FileIOPermission(FileIOPermissionAccess.Write, filePath);
                permission.Demand();
                return true;
            }
            catch (SecurityException ex)
            {
                if (OpenAI_Plugin.DebugActive == true)
                {
                    Logging.WriteToLog_Long($"The plugin has insufficient permissions for file operations: {ex.Message}", "red");
                }

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
                OpenAI_Plugin.VA_DisplayName(),
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// The OpenAI Plugin requires the following files in the Shared\Assemblies folder of VoiceAttack, run only on first time installation.<br />
        /// This method ensures these files are present before the Plugin can be used, informing users to restart one last time when complete.<br />
        /// <br />Will set <see cref="OpenAI_Plugin.OpenAiPluginFirstUse"/> to <see langword="True"/> if these assembly files were not already in shared assemblies folder:
        /// <br /><br />
        /// "Microsoft.Bcl.AsyncInterfaces.dll"<br />
        /// "Microsoft.Extensions.Options.dll"<br />
        /// "Microsoft.Extensions.Http.dll"<br />
        /// "Microsoft.Extensions.Primitives.dll"<br />
        /// "System.Threading.Tasks.Extensions.dll"<br />
        /// </summary>
        /// <returns>True when shared assemblies must be handled, false if all files are already in the folder.</returns>
        public static bool SharedAssembliesMoved()
        {
            string sourcePath;
            string destinationPath;
            string rootFolder = OpenAI_Plugin.VA_Proxy.AppsDir;
            string sharedFolder = OpenAI_Plugin.VA_Proxy.AssembliesDir;

            CreateNewDirectory(sharedFolder, true);

            bool filesMoved = false;
            string[] assemblyNames = new[]
            {
                "Microsoft.Bcl.AsyncInterfaces.dll",
                "Microsoft.Extensions.Options.dll",
                "Microsoft.Extensions.Http.dll",
                "Microsoft.Extensions.Primitives.dll",
                "System.Threading.Tasks.Extensions.dll"
            };

            foreach (string assemblyName in assemblyNames)
            {
                sourcePath = Path.Combine(rootFolder, "OpenAI_Plugin", "shared", assemblyName);
                destinationPath = Path.Combine(sharedFolder, assemblyName);

                if (File.Exists(destinationPath))
                {
                    continue;
                }

                if (!OperationHasClearance(sharedFolder))
                {
                    OpenAI_Plugin.VA_Proxy.WriteToLog("OpenAI Plugin Error: Must run VoiceAttack 'as Admin' for first time installation - see wiki!", "red");
                    return true;
                }

                File.Copy(sourcePath, destinationPath);
                filesMoved = true;
            }

            // Inform of required VoiceAttack restart only if files were added to Shared\Assemblies folder.
            if (filesMoved)
            {
                OpenAI_Plugin.OpenAiPluginFirstUse = true;
                ShowInstallationCompleteMessage();
            }

            return filesMoved;
        }

        /// <summary>
        /// Create the folders required for OpenAI Plugin including the configuration and whisper folders.
        /// </summary>
        /// <param name="directoryPath">The path of the folder to be created.</param>
        /// <exception cref="Exception">Thrown when folder does not exist after attempt, or has insufficient permission to create, or other errors.</exception>
        private static void CreateNewDirectory(string directoryPath) { CreateNewDirectory(directoryPath, false); }
        /// <summary>
        /// Create the folders required for OpenAI Plugin including the configuration and whisper folders, or shared assemblies folder which would require elevated permissions.
        /// </summary>
        /// <param name="directoryPath">The path of the folder to be created.</param>
        /// <param name="asAdmin">A boolean to indicate whether elevated permissions should be required to create a directory at the provided path.</param>
        /// <exception cref="Exception">Thrown when folder does not exist after attempt, or has insufficient permission to create, or other errors.</exception>
        private static void CreateNewDirectory(string directoryPath, bool asAdmin)
        {
            if (System.IO.Directory.Exists(directoryPath))
            {
                return;
            }

            if (asAdmin && !OperationHasClearance(directoryPath))
            {
                OpenAI_Plugin.VA_Proxy.WriteToLog("OpenAI Plugin Error: Must run VoiceAttack 'as Admin' for first time installation - see wiki!", "red");
                throw new Exception($"OpenAI Plugin Error: Must run VoiceAttack 'as Admin' for first time installation - see wiki!");
            }

            System.IO.Directory.CreateDirectory(directoryPath);
            if (!System.IO.Directory.Exists(directoryPath))
            {
                throw new Exception($"Folder does not exist after attempting to create it at path: {directoryPath}");
            }
        }

        /// <summary>
        /// A method to create the required operational folders for this plugin to save captured audio files used in Whisper, etc.
        /// </summary>
        /// <param name="isFirstRun">A boolean indicating if this method should try creating config folders on first run.</param>
        /// <exception cref="Exception">Thrown if unabled to create required config folders - plugin will not be initialized.</exception>
        public static bool CreateConfigFolders(bool isFirstRun)
        {
            string whisperFolder = System.IO.Path.Combine(DEFAULT_CONFIG_FOLDER, "whisper");

            if (isFirstRun)
            {
                CreateNewDirectory(DEFAULT_CONFIG_FOLDER);
                CreateNewDirectory(whisperFolder);
            }
            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Plugin_ConfigFolderPath", DEFAULT_CONFIG_FOLDER);
            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Plugin_WhisperFolderPath", whisperFolder);
            
            return true;
        }

    }
}
