using System.Net.Sockets;
using System.Text;

namespace codecrafters_http_server;

public class HttpResponse
{
    public HttpResponse(string protocol, int statusCode, string statusText)
    {
        Protocol = protocol;
        StatusCode = statusCode;
        StatusText = statusText;
        Headers = new Dictionary<string, string>();
    }
    
    public string Protocol { get; private set; }

    public int StatusCode { get; private set; }

    public string StatusText { get; private set; }

    public IReadOnlyDictionary<string, string> Headers { get; private set; }

    public byte[] Body { get; set; }

    public HttpResponse AddHeader(string key, string value)
    {
        var headers = (Dictionary<string, string>)Headers;

        if (!headers.TryAdd(key, value))
        {
            headers[key] = value;
        }
        
        return this;
    }

    public HttpResponse SetBody(byte[] body)
    {
        Body = body;
        return this;
    }


    public HttpResponse SetBody(string body)
    {
        Body = Encoding.UTF8.GetBytes(body);
        return this;
    }
    
    public byte[] ToBytes()
    {
        var responseStringBuilder = new StringBuilder($"{Protocol} {StatusCode} {StatusText}\r\n");

        foreach (KeyValuePair<string,string> header in Headers)
        {
            responseStringBuilder.Append($"{header.Key}: {header.Value}\r\n");
        }

        responseStringBuilder.Append("\r\n");

        byte[] bytes = Encoding.UTF8.GetBytes(responseStringBuilder.ToString());
        return [..bytes, ..Body];
    }
}