using System;
using System.Net;

public class HttpRequest
{
    public string Method { get; }
    public string Url { get; }
    public string QueryParams { get; }
    public string Body { get; }

    public HttpRequest(string rawRequest)
    {
        string[] lines = rawRequest.Split("\r\n");

        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
            throw new ArgumentException("Solicitud HTTP vacía o mal formada.");

        string requestLine = lines[0];
        string[] tokens = requestLine.Split(' ');

        if (tokens.Length < 2)
            throw new ArgumentException("Línea de solicitud inválida.");

        Method = tokens[0];

        // Separar ruta y parámetros
        string fullUrl = tokens[1];
        string[] urlParts = fullUrl.Split('?');
        Url = urlParts[0];
        QueryParams = urlParts.Length > 1 ? urlParts[1] : "-";

        // Extraer cuerpo si es POST
        Body = Method == "POST"
            ? WebUtility.UrlDecode(rawRequest.Split("\r\n\r\n").LastOrDefault() ?? "-")
            : "-";
    }
}