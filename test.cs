using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(""Bearer"", ""sbg_vdMoGLmWzpRRKN8FP4iyyzgNEQ9oflT1"");
        var requestBody = new
        {
            model_id = ""qwen.qwen3-vl-235b-a22b"",
            messages = new object[] {
                new { role = ""user"", text = ""test"", images = new[] { new { format = ""jpeg"", data_base64 = ""aGVsbG8="" } } }
            },
            max_tokens = 2000
        };
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, ""application/json"");
        var response = await client.PostAsync(""http://apiaccess.iti.net.eg/api/v1/student/multimodal-chat"", content);
        Console.WriteLine($""Status: {response.StatusCode}"");
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}
