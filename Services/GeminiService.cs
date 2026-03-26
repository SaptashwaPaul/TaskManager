using System.Text;
using System.Text.Json;

namespace TaskManager.API.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GeminiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> GenerateTask(string input)
        {
            var apiKey = _config["Gemini:ApiKey"];

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var prompt = $@"
                            Today is {today}.

                            Convert this into structured JSON:

                            Input: {input}

                            Return ONLY valid JSON:
                            {{
                              ""title"": ""string"",
                              ""description"": ""detailed breakdown of the task"",
                              ""priority"": ""Low | Medium | High"",
                              ""dueDate"": ""YYYY-MM-DD""
                            }}

                            Rules:
                            - Use today's date as reference
                            - If user says 'tomorrow', calculate correctly
                            - Do NOT return past dates
                            - Generate meaningful description
                            - Infer priority if not given
                            ";

            var body = new
            {
                contents = new[]
                {
                    new {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsync(url, content);

            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("RAW GEMINI RESPONSE:");
            Console.WriteLine(result);

            using var doc = JsonDocument.Parse(result);

            // 🔴 Handle API errors
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var errorString = error.ToString();

                // 🔥 HANDLE QUOTA ERROR
                if (errorString.Contains("429") || errorString.Contains("RESOURCE_EXHAUSTED"))
                {
                    throw new Exception("AI quota exceeded. Please try again in a minute.");
                }

                throw new Exception("Gemini API Error: " + errorString);
            }

            // 🔴 Validate candidates
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
                || candidates.GetArrayLength() == 0)
            {
                throw new Exception("No candidates returned: " + result);
            }

            // ✅ Extract text safely
            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception("Empty response from Gemini");
            }

            // 🔥 CLEAN RESPONSE (CRITICAL FIX)
            text = text
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            Console.WriteLine("CLEANED AI JSON:");
            Console.WriteLine(text);

            return text;
        }

        public async Task<string> GeneratePlan(List<string> tasks)
        {
            var apiKey = _config["Gemini:ApiKey"];

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var taskList = string.Join("\n", tasks);

            var prompt = $@"
                            Today is {today}.

                            You are an intelligent productivity assistant.

                            Given these tasks:

                            {taskList}

                            Create a daily plan.

                            Return ONLY JSON in this format:
                            {{
                              ""plan"": [
                                {{
                                  ""time"": ""HH:mm - HH:mm"",
                                  ""task"": ""task name"",
                                  ""reason"": ""why this is scheduled here""
                                }}
                              ]
                            }}

                            Rules:
                            - Prioritize high priority tasks first
                            - Spread tasks realistically
                            - Add short breaks
                            - Do NOT include any explanation outside JSON
                            ";

            var body = new
            {
                contents = new[]
                {
                new {
                    parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsync(url, content);

            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);

            // 🔴 Handle API errors
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var errorString = error.ToString();

                // 🔥 HANDLE QUOTA ERROR
                if (errorString.Contains("429") || errorString.Contains("RESOURCE_EXHAUSTED"))
                {
                    throw new Exception("AI quota exceeded. Please try again in a minute.");
                }

                throw new Exception("Gemini API Error: " + errorString);
            }

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            // 🔥 Clean JSON
            text = text!
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            return text;
        }

        public async Task<string> ChatWithTasks(string userMessage, List<string> tasks)
        {
            var apiKey = _config["Gemini:ApiKey"];

            var taskList = string.Join("\n", tasks);

            var prompt = $@"
                            You are an AI productivity assistant.

                            Here are the user's tasks:
                            {taskList}

                            User question:
                            {userMessage}

                            Instructions:
                            - Answer based ONLY on the tasks
                            - Be helpful and concise
                            - Suggest priorities if needed
                            ";

            var body = new
            {
                contents = new[]
                {
            new {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsync(url, content);

            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);

            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var errorString = error.ToString();

                if (errorString.Contains("429") || errorString.Contains("RESOURCE_EXHAUSTED"))
                {
                    throw new Exception("AI quota exceeded. Please try again later.");
                }

                throw new Exception("Gemini API Error: " + errorString);
            }

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "No response";
        }
    }
}