using OpenAI_API;
using OpenAI_API.Embedding;
using OpenAI_API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        /// <summary>
        /// Send embedding request to OpenAI API to get a newline and "; " deliniated string containing returned metadata.
        /// </summary>
        /// <returns>The data will be stored in the 'OpenAI_Response' VoiceAttack text variable, empty on failure.</returns>
        public static async Task Embed()
        {
            try
            {
                string embeddingInput = OpenAIplugin.VA_Proxy.GetText("OpenAI_EmbeddingInput") ?? String.Empty;
                string response = String.Empty;

                if (String.IsNullOrEmpty(embeddingInput)) { throw new Exception("Embedding input in OpenAI_EmbeddingInput text variable is null or empty."); }

                OpenAIplugin.VA_Proxy.SetText("OpenAI_EmbeddingResponse", String.Empty);

                OpenAIAPI api = OpenAI_Key.LOAD_KEY
                    ? new OpenAIAPI(new APIAuthentication(OpenAI_Key.API_KEY, OpenAI_Key.API_ORG))
                    : new OpenAIAPI(APIAuthentication.LoadFromPath(
                        directory: OpenAI_Key.DEFAULT_KEY_FILEFOLDER,
                        filename: OpenAI_Key.DEFAULT_KEY_FILENAME,
                        searchUp: true
                ));


                var result = await api.Embeddings.CreateEmbeddingAsync(new EmbeddingRequest(
                    model: Model.AdaTextEmbedding,
                    embeddingInput
                ));

                if (result != null && result.Data != null && result.Data.Count > 0)
                {
                    var embeddingList = result.Data;
                    if (embeddingList != null && embeddingList.Any())
                    {
                        var embeddingStrings = embeddingList.Select(data => string.Join("; ", data.Embedding));
                        response = string.Join(Environment.NewLine, embeddingStrings);
                    }
                }

                // Return the metadata as a string for post processing in VoiceAttack
                OpenAIplugin.VA_Proxy.SetText("OpenAI_EmbeddingResponse", response);
            }
            catch (Exception ex)
            {
                throw new Exception($"OpenAI Plugin Error: {ex.Message}");
            }
        }

    }
}
