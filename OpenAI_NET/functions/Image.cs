using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI.Net;
using OpenAI_NET.service;

namespace OpenAI_NET
{
    /// <summary>
    /// A class providing methods to work with images using OpenAI Dall-E through its API.
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
    public static class Image
    {
        /// <summary>
        /// Generate image(s) based on a supplied prompt, and any additional parameters.
        /// </summary>
        /// <param name="args">An array of strings that contains the function,
        /// the OpenAI API Key, and any additional image options.</param>
        /// <returns>Directly returns a string array containing URL(s) to DALL-E generated
        /// image(s) through the Named Pipe to the OpenAI VoiceAttack Plugin.</returns>
        public static async Task GenerateImage(string[] args)
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

                // Set DALL-E request parameters or use defaults
                string imagePrompt = args.Length > 2 && !String.IsNullOrEmpty(args[2]) ? args[2] : String.Empty;
                Int32 imageCount = args.Length > 3 && !String.IsNullOrEmpty(args[3]) ? Int32.TryParse(args[3], out int count) ? count : 1 : 1;
                string imageSize = args.Length > 4 && !String.IsNullOrEmpty(args[4]) ? args[4] : "1024x1024";

                if (String.IsNullOrEmpty(imagePrompt)) { throw new Exception("Image Generation Error: Image User Prompt is null or empty!"); }

                if (!(imageSize == "256x256" || imageSize == "512x512" || imageSize == "1024x1024"))
                {
                    throw new Exception("Image Generation Error: Image size invalid!");
                }

                if (imageCount > 10)
                    imageCount = 10;

                var openAi = host.Services.GetService<IOpenAIService>();
                if (openAi != null)
                {
                    var response = await openAi.Images.Generate(imagePrompt, imageCount, imageSize);

                    if (response.IsSuccess)
                    {
                        List<string> Results = response.Result!.Data.Select(i => i.Url).ToList();

                        // Send the URL(s) of the image(s) back to the OpenAI VoiceAttack Plugin
                        Piping.SendArgsToNamedPipe(Results.ToArray());
                    }
                    else
                    {
                        Console.WriteLine($"{response.ErrorMessage}");

                        // Send the error message back to the OpenAI VoiceAttack Plugin
                        throw new Exception($"Image Generation Error: {response.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                // Send the error message back to the OpenAI VoiceAttack Plugin
                throw new Exception($"Dall-E Exception: {ex.Message}");
            }

        }

        /// <summary>
        /// Upload an image from the supplied file path and create variation(s)
        /// based on a supplied prompt, and any additional parameters.
        /// </summary>
        /// <param name="args">An array of strings that contains the function,
        /// the OpenAI API Key, and any additional image options.</param>
        /// <returns>Directly returns a string array containing URL(s) to DALL-E variated
        /// image(s) through the Named Pipe to the OpenAI VoiceAttack Plugin.</returns>
        public static async Task VariateImage(string[] args) { await VariateImage(args, false); }
        /// <summary>
        /// Upload an image from the supplied file path and create variation(s)
        /// based on a supplied prompt, and any additional parameters.
        /// </summary>
        /// <param name="args">An array of strings that contains the function,
        /// the OpenAI API Key, and any additional image options.</param>
        /// <param name="useBytes">A boolean directing the method to upload the image using bytes.</param>
        /// <returns>Directly returns a string array containing URL(s) to DALL-E variated
        /// image(s) through the Named Pipe to the OpenAI VoiceAttack Plugin.</returns>
        public static async Task VariateImage(string[] args, bool useBytes)
        {
            try
            {
                using var host = Host.CreateDefaultBuilder()
                .ConfigureServices((builder, services) =>
                {
                    services.AddOpenAIServices(options =>
                    {
                        options.ApiKey = args[1];
                    });
                })
                .Build();

                // Set DALL-E request parameters or use defaults
                string imagePath = args.Length > 2 && !String.IsNullOrEmpty(args[2]) ? args[2] : String.Empty;
                Int32 imageCount = args.Length > 3 && !String.IsNullOrEmpty(args[3]) ? Int32.TryParse(args[3], out int count) ? count : 1 : 1;
                string imageSize = args.Length > 4 && !String.IsNullOrEmpty(args[4]) ? args[4] : "1024x1024";

                if (String.IsNullOrEmpty(imagePath)) { throw new Exception("Image Variation Error: File Path to Image is null or empty!"); }
                if (!(imageSize == "256x256" || imageSize == "512x512" || imageSize == "1024x1024"))
                {
                    throw new Exception("Image Variation Error: Image size invalid!");
                }

                if (imageCount > 10)
                    imageCount = 10;

                var openAi = host.Services.GetService<IOpenAIService>();
                if (openAi != null)
                {
                    var response = useBytes
                      ? await openAi.Images.Variation(File.ReadAllBytes(imagePath), o =>
                      {
                          o.N = imageCount;
                          o.Size = imageSize;
                      })
                      : await openAi.Images.Variation(imagePath, o =>
                      {
                          o.N = imageCount;
                          o.Size = imageSize;
                      });

                    if (response.IsSuccess)
                    {
                        List<string> Results = response.Result!.Data.Select(i => i.Url).ToList();

                        // Send the URL(s) of the image(s) back to the OpenAI VoiceAttack Plugin
                        Piping.SendArgsToNamedPipe(Results.ToArray());
                    }
                    else
                    {
                        Console.WriteLine($"{response.ErrorMessage}");

                        // Send the error message back to the OpenAI VoiceAttack Plugin
                        throw new Exception($"Image Variation Error: {response.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                // Send the error message back to the OpenAI VoiceAttack Plugin
                throw new Exception($"Dall-E Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Upload an image from the supplied file path and create edits
        /// based on a supplied prompt, an optional mask file path, and any additional parameters.
        /// </summary>
        /// <param name="args">An array of strings that contains the function,
        /// the OpenAI API Key, and any additional image options.</param>
        /// <returns>Directly returns a string array containing URL(s) to DALL-E edited
        /// image(s) through the Named Pipe to the OpenAI VoiceAttack Plugin.</returns>
        public static async Task EditImage(string[] args) { await EditImage(args, false); }
        /// <summary>
        /// Upload an image from the supplied file path and create edits
        /// based on a supplied prompt, an optional mask file path, and any additional parameters.
        /// </summary>
        /// <param name="args">An array of strings that contains the function,
        /// the OpenAI API Key, and any additional image options.</param>
        /// <param name="useBytes">A boolean directing the method to upload the image using bytes.</param>
        /// <returns>Directly returns a string array containing URL(s) to DALL-E edited
        /// image(s) through the Named Pipe to the OpenAI VoiceAttack Plugin.</returns>
        public static async Task EditImage(string[] args, bool useBytes)
        {
            try
            {
                using var host = Host.CreateDefaultBuilder()
                .ConfigureServices((builder, services) =>
                {
                    services.AddOpenAIServices(options =>
                    {
                        options.ApiKey = args[1];
                    });
                })
                .Build();

                // Set DALL-E request parameters or use defaults
                string imagePrompt = args.Length > 2 && !String.IsNullOrEmpty(args[2]) ? args[2] : String.Empty;
                string imagePath = args.Length > 3 && !String.IsNullOrEmpty(args[3]) ? args[3] : String.Empty;
                Int32 imageCount = args.Length > 4 && !String.IsNullOrEmpty(args[4]) ? Int32.TryParse(args[4], out int count) ? count : 1 : 1;
                string imageSize = args.Length > 5 && !String.IsNullOrEmpty(args[5]) ? args[5] : "1024x1024";
                string maskPath = args.Length > 6 && !String.IsNullOrEmpty(args[6]) ? args[6] : String.Empty;

                if (String.IsNullOrEmpty(imagePrompt)) { throw new Exception("Image Edit Error: Image User Prompt is null or empty!"); }
                if (String.IsNullOrEmpty(imagePath)) { throw new Exception("Image Edit Error: File Path to Image is null or empty!"); }


                if (!(imageSize == "256x256" || imageSize == "512x512" || imageSize == "1024x1024"))
                {
                    throw new Exception("Image Editing Error: Image size invalid!");
                }

                if (imageCount > 10)
                    imageCount = 10;

                var openAi = host.Services.GetService<IOpenAIService>();
                if (openAi != null)
                {
                    var response = useBytes
                      ? String.IsNullOrEmpty(maskPath)
                        ? await openAi.Images.Edit(imagePrompt, File.ReadAllBytes(imagePath), o =>
                        {
                            o.N = imageCount;
                            o.Size = imageSize;
                        })
                        : await openAi.Images.Edit(imagePrompt, File.ReadAllBytes(imagePath), File.ReadAllBytes(maskPath), o =>
                        {
                            o.N = imageCount;
                            o.Size = imageSize;
                        })
                      : String.IsNullOrEmpty(maskPath)
                        ? await openAi.Images.Edit(imagePrompt, imagePath, o =>
                        {
                            o.N = imageCount;
                            o.Size = imageSize;
                        })
                        : await openAi.Images.Edit(imagePrompt, imagePath, maskPath, o =>
                        {
                            o.N = imageCount;
                            o.Size = imageSize;
                        });

                    if (response.IsSuccess)
                    {
                        List<string> Results = response.Result!.Data.Select(i => i.Url).ToList();

                        // Send the URL(s) of the image(s) back to the OpenAI VoiceAttack Plugin
                        Piping.SendArgsToNamedPipe(Results.ToArray());
                    }
                    else
                    {
                        Console.WriteLine($"{response.ErrorMessage}");

                        // Send the error message back to the OpenAI VoiceAttack Plugin
                        throw new Exception($"Image Editing Error: {response.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                // Send the error message back to the OpenAI VoiceAttack Plugin
                throw new Exception($"Dall-E Exception: {ex.Message}");
            }
        }

    }
}
