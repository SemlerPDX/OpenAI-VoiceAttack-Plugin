using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing methods to work with files in the OpenAI account
    /// belonging to the supplied API Key using the <see cref="OpenAIAPI"/> library.
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
    public static class Files
    {
        /// <summary>
        /// Upload a file to the OpenAI account of the supplied API Key with an optional custom purpose, default is 'fine-tuning'.
        /// <br />Set the OpenAI_FilePurpose text variable with the intendend purpose of the uploaded documents. (see documentation)
        /// <br />Use "fine-tune" for Fine-tuning (default, if not set).
        /// This allows <see cref="OpenAIAPI"/> to validate the format of the uploaded file.
        /// </summary>
        /// <returns>Sets the OpenAI_Response text variable to the OpenAI API id of the uploaded file (if successful) or blank if otherwise,
        /// <br />and a success/failure message in the OpenAI_TTS_Response text variable.</returns>
        /// <exception cref="HttpRequestException">Thrown when an error occurs while getting files data using GetFilesAsync().</exception>
        public static async Task Upload()
        {
            try
            {
                string filePath = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_FilePath") ?? string.Empty;
                string filePurpose = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_FilePurpose") ?? string.Empty;
                string successMessage = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_TTS_UploadSuccess") ?? "The requested file has been uploaded.";
                string failureMessage = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_TTS_UploadFailure") ?? "The requested file was not found, or an error has occurred while uploading.";

                if (string.IsNullOrEmpty(filePath)) { throw new Exception("File path in OpenAI_FilePath text variable is null or empty!"); }

                List<string> filesList = new List<string>();
                string files = string.Empty;

                OpenAIAPI api = OpenAI_Key.LoadKey
                    ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.ApiKey, OpenAI_Key.ApiOrg))
                    : new OpenAIAPI(APIAuthentication.LoadFromPath(
                        directory: OpenAI_Key.DefaultKeyFileFolder,
                        filename: OpenAI_Key.DefaultKeyFilename,
                        searchUp: true
                ));

                var response = string.IsNullOrEmpty(filePurpose)
                    ? await api.Files.UploadFileAsync(filePath)
                    : await api.Files.UploadFileAsync(filePath, filePurpose);

                string message = response != null
                    ? successMessage
                    : failureMessage;

                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_TTS_Response", message);
                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Response", response.Id ?? string.Empty);
                OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_Error", response == null);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"OpenAI Plugin File Upload Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"OpenAI Plugin File Upload Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a list of all files uploaded to the OpenAI account of the supplied API Key.
        /// </summary>
        /// <returns>Sets the OpenAI_Response text variable to a string containing
        /// a semicolon separated list of files, or an empty string if none exist (or otherwise).</returns>
        /// <exception cref="HttpRequestException">Thrown when an error occurs while getting files data using GetFilesAsync().</exception>
        public static async Task List()
        {
            try
            {
                List<string> filesList = new List<string>();
                string files = string.Empty;
                string successMessage = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_TTS_ListSuccess") ?? "The list of files has been assembled.";
                string failureMessage = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_TTS_ListFailure") ?? "There are no files found in this account.";

                OpenAIAPI api = OpenAI_Key.LoadKey
                    ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.ApiKey, OpenAI_Key.ApiOrg))
                    : new OpenAIAPI(APIAuthentication.LoadFromPath(
                        directory: OpenAI_Key.DefaultKeyFileFolder,
                        filename: OpenAI_Key.DefaultKeyFilename,
                        searchUp: true
                ));

                var response = await api.Files.GetFilesAsync();

                foreach (var file in response)
                {
                    filesList.Add(file.Name);
                }

                if (filesList.Count > 0)
                {
                    files = string.Join(";", filesList);
                }

                string message = !string.IsNullOrEmpty(files)
                    ? successMessage
                    : failureMessage;

                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_TTS_Response", message);
                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Response", files);
                OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_Error", string.IsNullOrEmpty(files));
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"OpenAI Plugin File List Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"OpenAI Plugin File List Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a file from the OpenAI account of the supplied API Key
        /// which matches the supplied file name.
        /// </summary>
        /// <returns>Sets the OpenAI_Response text variable to the OpenAI API id of the deleted file (if successful) or blank if otherwise,
        /// <br />and a success/failure message in the OpenAI_TTS_Response text variable.</returns>
        /// <exception cref="HttpRequestException">Thrown when an error occurs while getting files data using GetFilesAsync().</exception>
        public static async Task Delete()
        {
            try
            {
                string successMessage = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_TTS_DeleteSuccess") ?? "The requested file has been deleted.";
                string failureMessage = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_TTS_DeleteFailure") ?? "The requested file was not found, or an error has occurred.";
                string message = failureMessage;
                string fileName = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_FileName") ?? string.Empty;

                if (string.IsNullOrEmpty(fileName)) { throw new Exception("File name in OpenAI_FileName text variable is null or empty!"); }

                OpenAIAPI api = OpenAI_Key.LoadKey
                    ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.ApiKey, OpenAI_Key.ApiOrg))
                    : new OpenAIAPI(APIAuthentication.LoadFromPath(
                        directory: OpenAI_Key.DefaultKeyFileFolder,
                        filename: OpenAI_Key.DefaultKeyFilename,
                        searchUp: true
                ));

                var response = await api.Files.GetFilesAsync();

                foreach (var file in response)
                {
                    if (file.Name == fileName)
                    {
                        var deleteResponse = await api.Files.DeleteFileAsync(file.Id);
                        if (deleteResponse.Deleted)
                        {
                            message = successMessage;
                        }
                    }
                }

                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_TTS_Response", message);
                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_Response", fileName);
                OpenAI_Plugin.VA_Proxy.SetBoolean("OpenAI_Error", message == failureMessage);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"OpenAI Plugin File Delete Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"OpenAI Plugin File Delete Error: {ex.Message}");
            }
        }

    }
}
