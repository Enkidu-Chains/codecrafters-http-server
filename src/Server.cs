using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_http_server;

string filesRoot = GetFilesRoot(args);
Directory.CreateDirectory(filesRoot);

Console.WriteLine("Logs from your program will appear here!");

var server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    Socket socket = await server.AcceptSocketAsync(); // wait for client
    await HandleConnection(socket);
    await socket.DisconnectAsync(false);
}

string GetFilesRoot(string[] args)
{
    try
    {
        int index = Array.IndexOf(args, "--directory") + 1;
        return index == 0 ? Directory.GetCurrentDirectory() : args[index];
    }
    catch (IndexOutOfRangeException e)
    {
        Console.WriteLine("The parameter for \"--directory\" was not set properly.");
        return Directory.GetCurrentDirectory();
    }
}

async Task<byte[]> Compress(byte[] input)
{
    await using var memoryStream = new MemoryStream();

    await using var compressionStream = new GZipStream(memoryStream, CompressionMode.Compress, true);
    await compressionStream.WriteAsync(input, 0, input.Length);
    await compressionStream.FlushAsync();
    compressionStream.Close();

    return memoryStream.ToArray();
}

async Task HandleConnection(Socket socket)
{
    var requestBytes = new byte[10*1024];
    int byteReceived = await socket.ReceiveAsync(requestBytes);
    
    if (byteReceived == 0)
    {
        return;
    }

    var request = new HttpRequest(requestBytes[new Range(0, byteReceived)]);
    HttpResponse response;

    if (request.Path.StartsWith("/echo/") && request.Method == "GET")
    {
        response = await HandleEcho(request);
    }
    else if (request.Path.StartsWith("/files/") && request.Method == "GET")
    {
        response = await RetrieveFile(request);
    }
    else if (request.Path.StartsWith("/files/") && request.Method == "POST")
    {
        response = await SaveFile(request);
    }
    else if (request is { Path: "/user-agent", Method: "GET" })
    {
        response = GetUserAgent(request);
    }
    else if (request is { Path: "/", Method: "GET" })
    {
        response = new HttpResponse("HTTP/1.1", 200, "OK");
    }
    else
    {
        response = new HttpResponse("HTTP/1.1", 404, "Not Found");
    }

    await socket.SendAsync(response.ToBytes());
}

HttpResponse GetUserAgent(HttpRequest httpRequest)
{
    string userAgent = httpRequest.GetHeader("User-Agent");
        
    HttpResponse httpResponse = new HttpResponse("HTTP/1.1", 200, "OK")
        .AddHeader("Content-Type", "text/plain")
        .AddHeader("Content-Length", userAgent.Length.ToString())
        .SetBody(userAgent);
    return httpResponse;
}

async Task<HttpResponse> SaveFile(HttpRequest httpRequest)
{
    Match match = Regex.Match(httpRequest.Path, @"(?<=/files/)[^\s/]+");

    string fileName = match.Value;

    await using var stream = new FileStream($"{filesRoot}/{fileName}", FileMode.Create, FileAccess.Write);
    await using var writer = new StreamWriter(stream);

    await writer.WriteAsync(httpRequest.Body);

    return new HttpResponse("HTTP/1.1", 201, "Created");
}

async Task<HttpResponse> RetrieveFile(HttpRequest httpRequest)
{
    Match match = Regex.Match(httpRequest.Path, @"(?<=/files/)[^\s/]+");
    string fileName = match.Value;
    
    try
    {
        await using var stream = new FileStream($"{filesRoot}/{fileName}", FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        string file = await reader.ReadToEndAsync();

        return new HttpResponse("HTTP/1.1", 200, "OK")
            .AddHeader("Content-Type", "application/octet-stream")
            .AddHeader("Content-Length", $"{stream.Length}")
            .SetBody(file);
    }
    catch (FileNotFoundException)
    {
        return new HttpResponse("HTTP/1.1", 404, "Not Found");
    }
}

async Task<HttpResponse> HandleEcho(HttpRequest httpRequest)
{
    Match match = Regex.Match(httpRequest.Path, @"(?<=/echo/)[^\s/]+");

    HttpResponse httpResponse = new HttpResponse("HTTP/1.1", 200, "OK")
        .AddHeader("Content-Type", "text/plain")
        .AddHeader("Content-Length", $"{match.Value.Length}");

    if (httpRequest.TryGetHeader("Accept-Encoding", out string? result) &&
        result.Split(",", StringSplitOptions.TrimEntries).Contains("gzip"))
    {
        httpResponse.SetBody(await Compress(Encoding.UTF8.GetBytes(match.Value)))
            .AddHeader("Content-Encoding", "gzip")
            .AddHeader("Content-Length", $"{httpResponse.Body.Length}");
    }
    else
    {
        httpResponse.SetBody(match.Value);
    }

    return httpResponse;
}