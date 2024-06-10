using System.Text;

namespace codecrafters_http_server;

public class HttpRequest
{
    public HttpRequest(byte[] requestBytes)
    {
        string requestString = Encoding.UTF8.GetString(requestBytes);

        string[] lines = requestString.Split("\r\n");
        string[] startLine = lines[0].Split(' ');

        Method = startLine[0];
        RequestPath = startLine[1];
        Protocol = startLine[2];
        Headers = lines.Skip(1).SkipLast(2)
            .Select(line => line.Split(": "))
            .ToDictionary(headerValue => headerValue[0], headerValue => headerValue[1]);
        Body = lines.Last();
    }

    public string Method { get; private set; }

    public string RequestPath { get; private set; }
    
    public string Protocol { get; private set; }
    
    public IReadOnlyDictionary<string, string> Headers { get; private set; }

    public string Body { get; private set; }
}