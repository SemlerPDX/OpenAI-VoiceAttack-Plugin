﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class with methods for handling image requests using OpenAI by sending and receiving
    /// data through the OpenAI_NET App which provides access to its Dall-E (.NET Core) library functions.
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
    public static class DallE
    {
        /// <summary>
        /// Generate image(s) based on a supplied prompt, and any additional parameters.
        /// </summary>
        /// <returns>Sets the OpenAI_Response text variable to a semicolon separated list of URLs to generated images.</returns>
        public static Task Generation()
        {
            try
            {
                string response = String.Empty;
                List<string> args = new List<string>
                {
                    "image.generate",
                    OpenAI_Key.API_KEY,
                    OpenAIplugin.VA_Proxy.GetText("OpenAI_ImagePrompt") ?? String.Empty,
                    OpenAIplugin.VA_Proxy.GetText("OpenAI_ImageCount") ?? String.Empty,
                    OpenAIplugin.VA_Proxy.GetText("OpenAI_ImageSize") ?? String.Empty
                };

                // Send the function request to the OpenAI_NET App
                if (!Piping.SendArgsToNamedPipe(args.ToArray()))
                {
                    throw new Exception($"Failed to send Dall-E Generation request through pipe!");
                }

                // Listen for the response from the OpenAI_NET App
                string[] responses = Piping.ListenForArgsOnNamedPipe();
                if (responses != null && !String.IsNullOrEmpty(responses[0]) && !responses[0].StartsWith("OpenAI_NET"))
                {
                    response = String.Join(";", responses);
                }
                else
                {
                    OpenAIplugin.VA_Proxy.SetBoolean("OpenAI_Error", true);
                }

                // Set the response to the VoiceAttack text variable and exit
                OpenAIplugin.VA_Proxy.SetText("OpenAI_Response", response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Dall-E Error: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Upload an image from the supplied file path and create variation(s)
        /// based on a supplied prompt, and any additional parameters.
        /// </summary>
        /// <returns>Sets the OpenAI_Response text variable to a semicolon separated list of URLs to image variations.</returns>
        public static Task Variation() { return Variation(false); }
        /// <summary>
        /// Upload an image from the supplied file path and create variation(s)
        /// based on a supplied prompt, and any additional parameters.
        /// </summary>
        /// <param name="useBytes">A boolean directing the method to upload the image using bytes.</param>
        /// <returns>Sets the OpenAI_Response text variable to a semicolon separated list of URLs to image variations.</returns>
        public static Task Variation(bool useBytes)
        {
            try
            {
                string response = String.Empty;
                string imageFunction = useBytes ? "image.variation" : "image.variation.bytes";

                string imagePath = OpenAIplugin.VA_Proxy.GetText("OpenAI_ImagePath") ?? String.Empty;

                if (String.IsNullOrEmpty(imagePath)) { throw new Exception("File path in OpenAI_ImagePath text variable is null or empty!"); }


                List<string> args = new List<string>
                {
                    imageFunction,
                    OpenAI_Key.API_KEY,
                    imagePath,
                    OpenAIplugin.VA_Proxy.GetText("OpenAI_ImageCount") ?? String.Empty,
                    OpenAIplugin.VA_Proxy.GetText("OpenAI_ImageSize") ?? String.Empty
                };

                // Send the function request to the OpenAI_NET App
                if (!Piping.SendArgsToNamedPipe(args.ToArray()))
                {
                    throw new Exception($"Failed to send Dall-E Variation request through pipe!");
                }

                // Listen for the response from the OpenAI_NET App
                string[] responses = Piping.ListenForArgsOnNamedPipe();
                if (responses != null && !String.IsNullOrEmpty(responses[0]) && !responses[0].StartsWith("OpenAI_NET"))
                {
                    response = String.Join(";", responses);
                }
                else
                {
                    OpenAIplugin.VA_Proxy.SetBoolean("OpenAI_Error", true);
                }

                OpenAIplugin.VA_Proxy.SetText("OpenAI_Response", response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Dall-E Error: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Upload an image from the supplied file path and create edits
        /// based on a supplied prompt, an optional mask file path, and any additional parameters.
        /// </summary>
        /// <returns>Sets the OpenAI_Response text variable to a semicolon separated list of URLs to image edits.</returns>
        public static Task Editing() { return Editing(false); }
        /// <summary>
        /// Upload an image from the supplied file path and create edits
        /// based on a supplied prompt, an optional mask file path, and any additional parameters.
        /// </summary>
        /// <param name="useBytes">A boolean directing the method to upload the image using bytes.</param>
        /// <returns>Sets the OpenAI_Response text variable to a semicolon separated list of URLs to image edits.</returns>
        public static Task Editing(bool useBytes)
        {
            try
            {
                string response = String.Empty;
                string imageFunction = useBytes ? "image.edit" : "image.edit.bytes";

                string imagePrompt = OpenAIplugin.VA_Proxy.GetText("OpenAI_ImagePrompt") ?? String.Empty;
                string imagePath = OpenAIplugin.VA_Proxy.GetText("OpenAI_ImagePath") ?? String.Empty;

                if (String.IsNullOrEmpty(imagePrompt)) { throw new Exception("Editing instructions in OpenAI_ImagePrompt text variable is null or empty!"); }
                if (String.IsNullOrEmpty(imagePath)) { throw new Exception("File path in OpenAI_ImagePath text variable is null or empty!"); }

                List<string> args = new List<string>
                {
                    imageFunction,
                    OpenAI_Key.API_KEY,
                    imagePrompt,
                    imagePath,
                    OpenAIplugin.VA_Proxy.GetText("OpenAI_ImageCount") ?? String.Empty,
                    OpenAIplugin.VA_Proxy.GetText("OpenAI_ImageSize") ?? String.Empty,
                    OpenAIplugin.VA_Proxy.GetText("OpenAI_ImageMaskPath") ?? String.Empty
                };

                // Send the function request to the OpenAI_NET App
                if (!Piping.SendArgsToNamedPipe(args.ToArray()))
                {
                    throw new Exception($"Failed to send Dall-E Editing request through pipe!");
                }

                // Listen for the response from the OpenAI_NET App
                string[] responses = Piping.ListenForArgsOnNamedPipe();
                if (responses != null && !String.IsNullOrEmpty(responses[0]))
                {
                    response = String.Join(";", responses);
                }
                else
                {
                    OpenAIplugin.VA_Proxy.SetBoolean("OpenAI_Error", true);
                }

                OpenAIplugin.VA_Proxy.SetText("OpenAI_Response", response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Dall-E Error: {ex.Message}");
            }
            return Task.CompletedTask;
        }

    }
}
