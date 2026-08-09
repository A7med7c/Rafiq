using System;
using System.Text.Json;
using System.Linq;

class Program
{
    static void Main()
    {
        var _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var bodyParameters = new[] { "John Doe", "Dr. Smith", "10/08/2026", "10:00 AM" };
        var templateName = "appointments";
        var normalizedPhone = "201012345678";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = normalizedPhone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = "ar_EG" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = bodyParameters
                            .Select(p => new { type = "text", text = p })
                            .ToArray()
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        Console.WriteLine(json);
    }
}
