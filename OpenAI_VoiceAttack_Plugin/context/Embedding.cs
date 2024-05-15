using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Embedding;
using OpenAI_API.Models;
using System;
using System.Management;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class providing a method for returning Embedding Request metadata using the OpenAI API via the <see cref="OpenAIAPI"/> library.
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
    public static class Embedding
    {
        // TOP_N of 990 == 1024 - 34 tokens of results prompt content instructions (not including the user input itself).
        private static readonly int? DEFAULT_TOP_N = 990;
        private static readonly int DEFAULT_TOP_K = 3;
        private static readonly int DEFAULT_DIMENSIONALITY = 1536;
        private static readonly float? DEFAULT_SIMILARITY = 0.825F;
        private static readonly string DEFAULT_PROMPT = "If the data below is not relevant to the user input," +
                                                        " ignore it and generate an appropriate answer based " +
                                                        "solely on the user input.";

        /// <summary>
        /// NOTE: Dev Testing - property for tracking total calculations during a single operation.
        /// </summary>
        public static float CosineCalculations { get; set; } = 0;

        /// <summary>
        /// NOTE: Dev Testing - property for presenting total calculations from a single vector processing operation.
        /// </summary>
        public static string CalculationsMessage { get; set; } = string.Empty;

        /// <summary>
        /// A class to represent returned metadata from Embedding OpenAI API requests.
        /// </summary>
        public class EmbeddingsResult
        {
            /// <summary>
            /// The embeddings generated from the provided content with a dimensionality of 1,536 vectors.
            /// </summary>
            public List<float> Embeddings { get; set; }

            /// <summary>
            /// The total number of tokens consumed by the content used for embedding.
            /// </summary>
            public int TotalTokens { get; set; }
        }

        ///NOTE: See private beta notes, already moved to SQL structure, all JSON references here need updating
        /// <summary>
        /// A class to represent each entry in a JSON embeddings metadata file.
        /// </summary>
        public class EmbeddingEntry
        {
            /// <summary>
            /// The zero based index of a given entry in the JSON file.
            /// </summary>
            public int Index { get; set; }

            /// <summary>
            /// The total tokens of the text content in a given entry.
            /// </summary>
            public int TextTokens { get; set; }

            /// <summary>
            /// The corresponding local file name the text content was taken from.
            /// </summary>
            public string FileName { get; set; }

            /// <summary>
            /// The text content of a given entry.
            /// </summary>
            public string TextContent { get; set; }

            /// <summary>
            /// The total number of embedding float vectors generated for the text content (must be 1536).
            /// </summary>
            public int EmbeddingDimensionality { get; set; }

            /// <summary>
            /// A list of L2 normalized embedding float vectors generated from the text content by OpenAI Embeddings API.
            /// </summary>
            public List<float> Embedding { get; set; }
        }

        /// <summary>
        /// A list of embeddings loaded from the provided JSON file with a dimensionality of 1,536 vectors per entry.
        /// </summary>
        public static List<EmbeddingEntry> ExistingEmbeddings { get; set; }

        /// <summary>
        /// A property to contain an existing entry when generating a new JSON file.
        /// </summary>
        public static EmbeddingEntry ExistingEntry { get; set; } = new EmbeddingEntry();



        /// <summary>
        /// Send embedding request to OpenAI API to get a newline and "; " separated string containing returned Embedding vectors.
        /// <br /><br />
        /// Returns are set to the VA text variable 'OpenAI_EmbeddingResponse' as a semicolon
        /// separated list of metadata including embedding vectors, or an empty string upon failure.
        /// </summary>
        public static async Task GetVectors()
        {
            string embeddingInput = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_EmbeddingInput") ?? string.Empty;
            string response = string.Empty;

            if (string.IsNullOrEmpty(embeddingInput))
            {
                throw new Exception("Embedding input in OpenAI_EmbeddingInput text variable is null or empty.");
            }

            // Redundant clearing of VA text variable in the event of unhandled exceptions.
            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_EmbeddingResponse", string.Empty);

            OpenAIAPI api = OpenAI_Key.LoadKey
                ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.ApiKey, OpenAI_Key.ApiOrg))
                : new OpenAIAPI(
                    APIAuthentication.LoadFromPath(
                        directory: OpenAI_Key.DefaultKeyFileFolder,
                        filename: OpenAI_Key.DefaultKeyFilename,
                        searchUp: true
                    )
                );

            ///NOTE: New models from OpenAI available, see notes - replace with VA variable and default model
            var result = await api.Embeddings.CreateEmbeddingAsync(
                new EmbeddingRequest(
                    model: Model.AdaTextEmbedding,
                    embeddingInput
                )
            );

            if (result != null && result.Data != null && result.Data.Count > 0)
            {
                var embeddingList = result.Data;
                if (embeddingList != null && embeddingList.Any())
                {
                    ///NOTE: During Embeddings Feature Dev and Refactoring, check validity of "; " versus ";"
                    var embeddingStrings = embeddingList.Select(data => string.Join("; ", data.Embedding));
                    response = string.Join(Environment.NewLine, embeddingStrings);
                }
            }

            OpenAI_Plugin.VA_Proxy.SetText("OpenAI_EmbeddingResponse", response);
        }

        ///NOTE: The following section (and some variables at top of class) relate to new WIP features in development May2024
        #region beta_features_testing
        /// <summary>
        /// A method to read all contents of any file and return as string.
        /// </summary>
        /// <param name="filePath">The full path to the file, including extension.</param>
        /// <returns>A string containing all file contents, or an empty string on errors or if file does not exist.</returns>
        public static string ReadFileContents(string filePath)
        {
            string contents = string.Empty;

            try
            {
                if (File.Exists(filePath))
                {
                    contents = File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLogFile($"OpenAI Plugin Continuation on Error reading file \"{filePath}\": {ex.Message}");
            }

            return contents;
        }

        /// <summary>
        /// A method to read the embedding vectors JSON file and return an <see cref="EmbeddingEntry"/> List.
        /// </summary>
        /// <param name="jsonFilePath">The path to the JSON file containing embeddings vectors and content related to those vectors.</param>
        /// <returns>An <see cref="EmbeddingEntry"/> List of embedding vectors, text content, and file names - or an empty list upon errors.</returns>
        public static List<EmbeddingEntry> ReadJsonFile(string jsonFilePath)
        {
            List<EmbeddingEntry> entries = new List<EmbeddingEntry>();

            string json = ReadFileContents(jsonFilePath);
            if (string.IsNullOrEmpty(json))
                return entries;

            try
            {
                entries = JsonConvert.DeserializeObject<List<EmbeddingEntry>>(json);
            }
            catch (Exception ex)
            {
                Logging.WriteToLogFile($"OpenAI Plugin Continuation on Error reading json contents in file \"{jsonFilePath}\": {ex.Message}");
            }

            return entries;
        }

        ///NOTE: Is early test version of same method duplicated elsewhere in this class, see notes and import most recent version
        /// <summary>
        /// A method using cosine similarity to find the nearest embeddings in the JSON file based on the input embedding vectors, if any.
        /// </summary>
        /// <param name="embedding">The embedding vectors for which to find the nearest embeddings.</param>
        /// <param name="entries">An <see cref="EmbeddingEntry"/> List of embeddings metadata.</param>
        /// <param name="topK">The number of nearest embeddings to retrieve.</param>
        /// <param name="similarityCutoff">The optional cosine similarity threshold to exceed in order to be considered similar at all (range -1 to 1).</param>
        /// <returns>A list of nearest embedding indices in descending order of similarity, or an empty list (if no similarities found).</returns>
        //public static List<int> FindNearestEmbeddings(List<float> embedding, List<EmbeddingEntry> entries, int topK, float? similarityCutoff = 2F)
        //{
        //    List<int> nearestIndices = new List<int>();

        //    if (entries == null || entries.Count > 0)
        //        return nearestIndices;

        //    topK = topK <= 0 ? DEFAULT_TOP_K : topK;
        //    similarityCutoff = (similarityCutoff < -1 || similarityCutoff > 1) ? DEFAULT_SIMILARITY : similarityCutoff;

        //    try
        //    {
        //        Dictionary<int, float> similarityScores = new Dictionary<int, float>();

        //        foreach (var entry in entries)
        //        {
        //            float similarity = CalculateSimilarity(embedding, entry.Embedding);
        //            if (similarity <= similarityCutoff)
        //                continue;

        //            similarityScores.Add(entry.Index, similarity);
        //        }

        //        // Sorting the similarity scores in descending order is "most similar first"
        //        var sortedScores = similarityScores.OrderByDescending(x => x.Value);

        //        var nearestEntries = sortedScores.Take(topK);

        //        nearestIndices.AddRange(nearestEntries.Select(x => x.Key));
        //    }
        //    catch (Exception ex)
        //    {
        //        Logging.WriteToLogFile($"OpenAI Plugin Continuation on Error finding nearest embeddings: {ex.Message}");
        //    }

        //    /// NOTE: Dev Testing total number of similarites:
        //    Console.WriteLine($"{nearestIndices.Count} similarities found!");

        //    return nearestIndices;
        //}


        /// <summary>
        /// Get the number of parallel threads to perform vector cosine calculations upon.
        /// </summary>
        /// <returns>The number of CPU Logical Processors if available, else the number of CPU Cores.</returns>
        private static int GetProcessThreadCount()
        {
            int processThreadCount = 2;
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                    {
                        processThreadCount = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                    }
                }
            }
            catch (Exception ex)
            {
                // Simply return the core count if querying the system information fails
                processThreadCount = Environment.ProcessorCount;

                /// NOTE: Dev/Testing check exceptions thrown and when this may occur, remove this when done:
                if (OpenAI_Plugin.DebugActive == true)
                {
                    Logging.WriteToLog_Long($"OpenAI Plugin Continuation on Error - Failed to retrieve logical thread count: {ex.Message}", "orange");
                }
            }
            return processThreadCount;
        }

        /// <summary>
        /// A helper method to calculate the cosine similarity between two embedding vectors.
        /// </summary>
        /// <param name="embedding1">The first embedding vector.</param>
        /// <param name="embedding2">The second embedding vector.</param>
        /// <returns>The similarity score between the two embeddings.</returns>
        private static float CalculateSimilarity(List<float> embedding1, List<float> embedding2)
        {
            // Calculate the dot product between the two embeddings
            float dotProduct = 0F;
            for (int i = 0; i < embedding1.Count; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
            }

            float magnitude1 = (float)Math.Sqrt(embedding1.Sum(x => x * x));
            float magnitude2 = (float)Math.Sqrt(embedding2.Sum(x => x * x));

            float similarity = dotProduct / (magnitude1 * magnitude2);

            /// NOTE: Dev Testing total calculations:
            Embedding.CosineCalculations++;

            return similarity;
        }

        /// <summary>
        /// A method using cosine similarity to find the nearest embeddings (if any) in the database based on the input embedding vectors.
        /// </summary>
        /// <param name="embedding">The embedding vectors for which to find the nearest embeddings.</param>
        /// <param name="entries">An <see cref="EmbeddingEntry"/> List of embeddings metadata.</param>
        /// <param name="topK">The number of nearest embeddings to retrieve.</param>
        /// <param name="similarityCutoff">The optional cosine similarity threshold to exceed in order to be considered similar at all (range -1 to 1).</param>
        /// <returns>A list of nearest embedding indices in descending order of similarity, or an empty list (if no similarities found).</returns>
        private static List<int> FindNearestEmbeddings(List<float> embedding, List<EmbeddingEntry> entries, int topK, float? similarityCutoff = 2F)
        {
            List<int> nearestIndices = new List<int>();

            if (entries == null || entries.Count == 0)
                return nearestIndices;

            topK = topK <= 0 ? DEFAULT_TOP_K : topK;
            similarityCutoff = (similarityCutoff < -1 || similarityCutoff > 1) ? DEFAULT_SIMILARITY : similarityCutoff;

            try
            {
                ConcurrentDictionary<int, float> similarityScores = new ConcurrentDictionary<int, float>();

                // Parallel Processing is required for large databases, base chunks on Thread/Core count:
                int chunkSize = entries.Count / GetProcessThreadCount();
                int remainder = entries.Count % GetProcessThreadCount();

                var chunks = entries.Select((entry, index) => new { Entry = entry, Index = index })
                                    .GroupBy(x => x.Index / chunkSize)
                                    .Select(g => g.Select(x => x.Entry).ToList())
                                    .ToList();

                if (remainder > 0)
                {
                    int chunkIndex = 0;
                    foreach (var remainingEntry in entries.Skip(chunkSize * GetProcessThreadCount()))
                    {
                        chunks[chunkIndex].Add(remainingEntry);
                        chunkIndex = (chunkIndex + 1) % chunks.Count;
                    }
                }

                Parallel.ForEach(chunks, chunk =>
                {
                    foreach (var entry in chunk)
                    {
                        float similarity = CalculateSimilarity(embedding, entry.Embedding);
                        if (similarity <= similarityCutoff)
                            continue;

                        similarityScores.TryAdd(entry.Index, similarity);
                    }
                });

                // Sorting the similarity scores in descending order is "most similar first"
                var sortedScores = similarityScores.OrderByDescending(x => x.Value);

                var nearestEntries = sortedScores.Take(topK);

                nearestIndices.AddRange(nearestEntries.Select(x => x.Key));
            }
            catch (Exception ex)
            {
                Logging.WriteToLogFile($"OpenAI Plugin Continuation on Error finding nearest embeddings: {ex.Message}");
            }

            /// NOTE: Dev Testing total number of similarities:
            Console.WriteLine($"{nearestIndices.Count} similarities found!");

            return nearestIndices;
        }

        /// <summary>
        /// A method to send an Embedding request to the OpenAI API to get extended metadata.
        /// </summary>
        /// <param name="content">The text content to generate embeddings metadata from.</param>
        /// <returns>Returned metadata including a list of embedding vectors as floats generated from
        /// the input content and the total tokens of that content.</returns>
        /// <exception cref="Exception"></exception>
        public static async Task<EmbeddingsResult> GetMetadata(string content)
        {
            EmbeddingsResult embeddingsResult = new EmbeddingsResult();

            try
            {
                OpenAIAPI api = OpenAI_Key.LoadKey
                    ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.ApiKey, OpenAI_Key.ApiOrg))
                    : new OpenAIAPI(APIAuthentication.LoadFromPath(
                        directory: OpenAI_Key.DefaultKeyFileFolder,
                        filename: OpenAI_Key.DefaultKeyFilename,
                        searchUp: false
                ));

                ///NOTE: New models from OpenAI available, see notes - replace with VA variable and default model
                var embeddings = await api.Embeddings.CreateEmbeddingAsync(new EmbeddingRequest(
                    model: Model.AdaTextEmbedding,
                    content
                ));

                if (embeddings == null || embeddings.Data == null || embeddings.Data.Count == 0)
                    return embeddingsResult;

                var embeddingList = embeddings.Data;

                if (embeddingList?.Any() != true)
                    return embeddingsResult;

                embeddingsResult = new EmbeddingsResult
                {
                    Embeddings = embeddingList.SelectMany(data => data.Embedding).ToList(),
                    TotalTokens = embeddings.Usage.TotalTokens
                };
            }
            catch (Exception ex)
            {
                Logging.WriteToLogFile($"OpenAI Plugin Continuation on Error in GetMetadata Method: {ex.Message}");
            }

            return embeddingsResult;
        }

        /// <summary>
        /// A method to perform a custom vector processing function on user input through the JSON file,
        /// building a new user input for ChatGPT to compile a response from by using similar local
        /// embedding metadata content relevant to the user input.
        /// </summary>
        /// <param name="embeddingEntries">A list of embeddings metadata loaded from the provided JSON file.</param>
        /// <param name="embeddingContent">The new input to obtain embedding vectors for and process against existing vectors.</param>
        /// <param name="systemPrompt">The instructions provided to ChatGPT on how to respond using data and input.</param>
        /// <param name="topK">The total number of similar entries to retrieve as the body of similar data.</param>
        /// <param name="topN">The max number of tokens to use assembling the body of data, regardless of <see param="topK"/>.</param>
        /// <param name="similarityCutoff">The cosine similarity threshold to be considered similar (range -1 to 1).</param>
        /// <returns>A new input containing data for a ChatGPT to use for generating a response to the original input, or the original input.</returns>
        public static async Task<string> Processing(List<EmbeddingEntry> embeddingEntries, string embeddingContent, string systemPrompt, int topK = 0, int? topN = 0, float? similarityCutoff = 2F)
        {
            topN = topN <= 0 ? DEFAULT_TOP_N : topN;
            systemPrompt = string.IsNullOrEmpty(systemPrompt) ? DEFAULT_PROMPT : systemPrompt;

            string result = embeddingContent;

            EmbeddingsResult embeddingResult = await GetMetadata(embeddingContent);
            if (embeddingResult.Embeddings.Count == 0 || embeddingEntries.Count == 0)
                return result;

            DateTime startTime = DateTime.Now; /// NOTE: Dev Testing
            CalculationsMessage = string.Empty;
            CosineCalculations = 0;

            List<int> nearestIndices = FindNearestEmbeddings(embeddingResult.Embeddings, embeddingEntries, topK, similarityCutoff);

            if (nearestIndices.Count == 0)
                return result;

            // Build results using similar data on file as new input for a ChatGPT generated response to original user input:
            result = systemPrompt +
                    "\r\n" +
                    "USER: " + embeddingContent +
                    "\r\n" +
                    "DATA:";

            int maxTokens = 0;
            for (int i = 0; i < nearestIndices.Count && maxTokens < topN; i++)
            {
                int index = nearestIndices[i];
                result += "\r\n" + embeddingEntries[index].TextContent;
                maxTokens += embeddingEntries[index].TextTokens;
            }

            /// NOTE: Dev Testing
            CalculationsMessage = $"{CosineCalculations} Calculations performed in {(DateTime.Now - startTime).TotalMilliseconds} milliseconds.\r\n";
            Logging.WriteToLog_Long(CalculationsMessage, "pink");

            return result;
        }
        #endregion

    }
}
