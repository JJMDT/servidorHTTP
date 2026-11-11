using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

internal class Program
{
    private static async Task Main(string[] args)
    {
        string configText = File.ReadAllText("config.json");
        var config = JsonSerializer.Deserialize<ServerConfig>(configText);

        if (config == null)
        {
            Console.WriteLine("Error: no se pudo leer la configuración.");
            return;
        }

        var server = new HttpServer(config.Port, config.RootDirectory);
        await server.StartAsync();
    }
}