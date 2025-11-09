using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

public class HttpServer
{
    private readonly int _port;
    private readonly string _rootDirectory;
    private Socket? _listener;
    private static readonly object _logLock = new object();

    public HttpServer(int port, string rootDirectory)
    {
        _port = port;
        _rootDirectory = rootDirectory;
    }

    // Mapea extensiones simples a content-types. Mantenerlo pequeño y fácil de explicar.
    private string GetContentType(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".html" => "text/html",
            ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream",
        };
    }

    public async Task StartAsync()
    {
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(IPAddress.Any, _port));
        _listener.Listen(100); // backlog

        Console.WriteLine($"Servidor escuchando en puerto {_port}... http://localhost:{_port}");

        while (true)
        {
            Socket clientSocket = await _listener.AcceptAsync();
            _ = HandleClientRequestAsync(clientSocket); // concurrente
        }
    }

    private async Task HandleClientRequestAsync(Socket clientSocket)
    {
        string sessionId = Guid.NewGuid().ToString().Substring(0, 8); // ejemplo: "a1b2c3d4"
        var buffer = new byte[8192];
        int received = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
        string requestText = Encoding.UTF8.GetString(buffer, 0, received);
        var request = new HttpRequest(requestText);

        string method = request.Method;
        string url = request.Url;
        string queryParams = request.QueryParams;
        string body = request.Body;

        string path = url == "/" ? "/index.html" : url;
        string filePath = Path.Combine(_rootDirectory, path.TrimStart('/'));

        var remote = clientSocket.RemoteEndPoint as IPEndPoint;
        string clientIp = remote?.Address.ToString() ?? "unknown";

        //  Validación de seguridad: evitar acceso fuera de wwwroot
        string fullPath = Path.GetFullPath(filePath);
        string rootPath = Path.GetFullPath(_rootDirectory);

        if (!fullPath.StartsWith(rootPath))
        {
            string forbidden = "<h1>403 Forbidden</h1>";
            byte[] response = HttpResponse.Build(403, "text/html", Encoding.UTF8.GetBytes(forbidden));
            await clientSocket.SendAsync(response, SocketFlags.None);
            LogRequest(clientIp, method, url, queryParams, body, "-", 403, sessionId);
            clientSocket.Close();
            return;
        }

        if (method == "POST")
        {
            Console.WriteLine($"POST Body: {body}");

            // Registrar la petición POST
            LogRequest(clientIp, method, url, queryParams, body, "-", 200, sessionId);

            // Respuesta simple: 302 Found con Location: / para que el cliente haga GET a index
            string redirect = "HTTP/1.1 302 Found\r\nLocation: /\r\nContent-Length: 0\r\n\r\n";
            byte[] redirectBytes = Encoding.UTF8.GetBytes(redirect);
            await clientSocket.SendAsync(redirectBytes, SocketFlags.None);

            clientSocket.Close();
            return;
        }

        if (File.Exists(filePath))
        {
            byte[] content = File.ReadAllBytes(filePath);

            // Determinar Content-Type por extensión
            string contentType = GetContentType(filePath);

            // Comprimir solo tipos de texto para simplificar
            bool shouldCompress = contentType.StartsWith("text/") || contentType == "application/javascript";

            byte[] response = HttpResponse.Build(200, contentType, content, compress: shouldCompress,filePathForLog:path);
            await clientSocket.SendAsync(response, SocketFlags.None);

            LogRequest(clientIp, method, url, queryParams, body, filePath, 200, sessionId);
        }
        else
        {
            string notFoundPath = Path.Combine(_rootDirectory, "404.html");
            string notFoundContent = File.Exists(notFoundPath) ? File.ReadAllText(notFoundPath) : "<h1>404 Not Found</h1>";

            byte[] response = HttpResponse.Build(404, "text/html", Encoding.UTF8.GetBytes(notFoundContent));
            await clientSocket.SendAsync(response, SocketFlags.None);

            string served = File.Exists(notFoundPath) ? notFoundPath : "-";
            LogRequest(clientIp, method, url, queryParams, body, served, 404, sessionId);
        }

        clientSocket.Close();
    }

private void LogRequest(string ip, string method, string url, string query, string body, string servedPath, int status, string sessionId)
    {
        string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string date = DateTime.Now.ToString("yyyy-MM-dd");

        string log =
$@"
Fecha:       {dateTime}
IP:          {ip}
Método:      {method}
Recurso:     {url}
Query:       {query}
Body:        {body}
Servido:     {servedPath}
Status:      {status}
------------------------------

";

        string logFile = Path.Combine("logs", $"log_{date}.txt");
        Directory.CreateDirectory("logs");

        lock (_logLock)
        {
            File.AppendAllText(logFile, log);
        }
    }
    
}
