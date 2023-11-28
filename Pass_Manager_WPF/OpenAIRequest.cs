using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pass_Manager_WPF
{
    internal class OpenAIRequest
    {
        public static CancellationTokenSource cts_pub = null;
        public static string responseFinish = "";

        public static async Task OpenAIRequetst(string text)
        {
            if (text.Contains("си шарп"))
            {
                text = text.Replace("си шарп", "C#");
            }
            if (text.Contains("\r"))
            {
                text = text.Replace("\r", "");
            }
            if (text.Contains("\n"))
            {
                text = text.Replace("\n", " ");
            }
            if (text.Contains("$"))
            {
                text = text.Replace("$", "");
            }
            if (text.Contains("\\"))
            {
                text = text.Replace("\\", "");
            }

            string apiKey = ""; // API Token ChatGPT
            string endpoint = "https://api.openai.com/v1/chat/completions"; // URL для запроса 

            string prompt = text; // Ваш текст для обработки

            string response = "";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestContent = new StringContent(
                    $"{{ \"model\": \"gpt-3.5-turbo\", \"messages\": [{{ \"role\": \"system\", \"content\": \"You are an omniscient assistant.\" }}, {{ \"role\": \"user\", \"content\": \"{prompt}\" }}] }}",
                    Encoding.UTF8,
                    "application/json");

                var response2 = httpClient.PostAsync(endpoint, requestContent).Result;
                var responseBody = response2.Content.ReadAsStringAsync().Result;

                dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody);
                response = jsonResponse.choices[0].message.content;
            }

            if (response.Contains("```"))
            {
                int pos = response.IndexOf("```");
                response = response.Substring(pos + 3);
                pos = response.IndexOf("```");
                response = response.Substring(0, pos);
                response = response.Replace("`", "");
            }
            if (response.Contains("csharp"))
            {
                response = response.Replace("csharp", "");
            }
               

            responseFinish = response;
        }
    }
}
