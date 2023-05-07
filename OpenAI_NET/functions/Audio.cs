using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI.Net;
using OpenAI_NET.service;

namespace OpenAI_NET
{
    /// <summary>
    /// A class providing methods to transform audio with OpenAI Audio using its API.
    /// <br />
    /// <br>See Also:</br>
    /// <br /><see cref="OpenAI.Net.IOpenAIService"/>
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
    public static class Audio
    {
        /// <summary>
        /// Transcribe audio into text using the OpenAI API and return it to the OpenAI VoiceAttack Plugin.
        /// </summary>
        /// <param name="args">An array of strings that contains the function,
        /// the OpenAI API Key, and the path to the audio file.</param>
        /// <returns>Directly returns a string array containing the transcribed text
        /// through the Named Pipe to the OpenAI VoiceAttack Plugin.</returns>
        public static async Task Transcribe(string[] args)
        {
            try
            {
                // Set Organization ID if contained in the API key within args
                string argsOrg = String.Empty;
                if (args.Length > 0)
                {
                    if (args[1].Contains(':'))
                    {
                        string[] argsKey = args[1].Split(":");
                        args[1] = argsKey[0];
                        argsOrg = argsKey[1];
                    }
                }

                // Set the host with or without organization id
                using var host = String.IsNullOrEmpty(argsOrg)
                    ? Host.CreateDefaultBuilder(args)
                    .ConfigureServices((builder, services) =>
                    {
                        services.AddOpenAIServices(options =>
                        {
                            options.ApiKey = args[1];
                        });
                    })
                    .Build()
                    : Host.CreateDefaultBuilder(args)
                    .ConfigureServices((builder, services) =>
                    {
                        services.AddOpenAIServices(options =>
                        {
                            options.ApiKey = args[1];
                            options.OrganizationId = argsOrg;
                        });
                    })
                    .Build();

                var openAi = host.Services.GetService<IOpenAIService>()!;

                var transcription = await openAi.Audio.GetTranscription(args[2]);

                if (String.IsNullOrEmpty(transcription.Result!.Text))
                    throw new Exception($"Transcription Error - results text is null or empty!");

                // Send the transcribed text back to the OpenAI VoiceAttack Plugin
                Piping.SendArgsToNamedPipe(new[] { transcription.Result!.Text });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAI_NET Transcription Error! {ex.Message}");

                // Send the error message back to the OpenAI VoiceAttack Plugin
                throw new Exception($"Whisper Transcription Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Translates non-English audio into English using the OpenAI API,
        /// then return it to the OpenAI VoiceAttack Plugin.
        /// </summary>
        /// <param name="args">An array of strings that contains the function,
        /// the OpenAI API Key, and the path to the audio file.</param>
        /// <returns>Directly returns a string array containing the translated text
        /// through the Named Pipe to the OpenAI VoiceAttack Plugin.</returns>
        public static async Task Translate(string[] args)
        {
            try
            {
                using var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((builder, services) =>
                {
                    services.AddOpenAIServices(options =>
                    {
                        options.ApiKey = args[1];
                    });
                })
                .Build();

                var openAi = host.Services.GetService<IOpenAIService>()!;
                var translation = await openAi.Audio.GetTranslation(args[2]);

                // Send the translated text back to the OpenAI VoiceAttack Plugin
                Piping.SendArgsToNamedPipe(new[] { translation.Result!.Text, "success" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAI_NET Translation Error! {ex.Message}");

                // Send the error message back to the OpenAI VoiceAttack Plugin
                throw new Exception($"Whisper Translation Error: {ex.Message}");
            }
        }

    }
}
