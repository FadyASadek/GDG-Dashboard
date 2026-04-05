using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using Google.GenAI;
using Google.GenAI.Types;

namespace GDG_DashBoard.BLL.Services.Ai;

public class GeminiCvParserService : ICvParserService
{
    private readonly IConfiguration _config;

    public GeminiCvParserService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> ParseCvToJsonAsync(IFormFile cvFile)
    {
        if (cvFile == null || cvFile.Length == 0)
            throw new ArgumentException("No file uploaded.");

        string extractedText = string.Empty;
        
        using (var ms = new MemoryStream())
        {
            await cvFile.CopyToAsync(ms);
            ms.Position = 0;
            
            using (var pdfDocument = PdfDocument.Open(ms))
            {
                foreach (var page in pdfDocument.GetPages())
                {
                    extractedText += page.Text + "\n";
                }
            }
        }

        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("Gemini API Key is missing in Configuration.");
        }

        // The official SDK depends on the environment variable
        System.Environment.SetEnvironmentVariable("GOOGLE_API_KEY", apiKey);
        
        var client = new Client();

        string prompt = $@"Act as an expert HR parser. I am providing you with the extracted text from a CV. 
Strictly return ONLY a raw JSON object matching this exact schema. Do not include markdown formatting like ```json or any explanation.

{{
  ""name"": """",
  ""email"": """",
  ""phone"": """",
  ""location"": """",
  ""professional_summary"": """",
  ""education"": [{{ ""degree"": """", ""university"": """", ""year"": """" }}],
  ""professional_experience"": [{{ ""title"": """", ""company"": """", ""period"": """", ""description"": """" }}],
  ""projects"": [{{ ""name"": """", ""description"": """", ""period"": """" }}],
  ""technical_skills"": [],
  ""soft_skills"": []
}}

Extracted CV Text:
{extractedText}";

        try
        {
            var response = await client.Models.GenerateContentAsync(
                model: "gemini-3-flash-preview", 
                contents: prompt
            );

            var rawParsedText = response.Candidates?[0]?.Content?.Parts?[0]?.Text;

            if (string.IsNullOrWhiteSpace(rawParsedText)) 
                throw new Exception("Gemini returned empty text.");

            rawParsedText = rawParsedText.Trim();
            if (rawParsedText.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                rawParsedText = rawParsedText.Substring(7);
            }
            else if (rawParsedText.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                rawParsedText = rawParsedText.Substring(3);
            }

            if (rawParsedText.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                rawParsedText = rawParsedText.Substring(0, rawParsedText.Length - 3);
            }

            return rawParsedText.Trim();
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("503") || ex.Message.Contains("quota") || ex.Message.Contains("429") || ex.Message.Contains("demand"))
            {
                throw new Exception("Google AI is currently experiencing high traffic. Please try again in 30 seconds.");
            }
            throw;
        }
    }
}
