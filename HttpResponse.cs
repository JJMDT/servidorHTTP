using System.IO.Compression;
using System.Text;

public class HttpResponse
{
    // extraHeaders: llave -> valor (por ejemplo: Location -> /)
    public static byte[] Build(int statusCode, string contentType, byte[] body, bool compress = false, System.Collections.Generic.IDictionary<string, string>? extraHeaders = null)
    {
        byte[] finalBody = compress ? Compress(body) : body;

        string statusText = statusCode switch
        {
            200 => "OK",
            302 => "Found",
            303 => "See Other",
            403 => "Forbidden",
            404 => "Not Found",
            _ => "Error"
        };

        var sb = new StringBuilder();
        sb.Append($"HTTP/1.1 {statusCode} {statusText}\r\n");
        sb.Append($"Content-Type: {contentType}\r\n");
        if (compress) sb.Append("Content-Encoding: gzip\r\n");

        if (extraHeaders != null)
        {
            foreach (var kv in extraHeaders)
            {
                sb.Append($"{kv.Key}: {kv.Value}\r\n");
            }
        }

        sb.Append($"Content-Length: {finalBody.Length}\r\n\r\n");

        byte[] headerBytes = Encoding.UTF8.GetBytes(sb.ToString());
        return headerBytes.Concat(finalBody).ToArray();
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        // GZipStream must be disposed (or closed) before reading the underlying MemoryStream
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }
}