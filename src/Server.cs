using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_http_server;

string filesRoot = GetFilesRoot(args);
Directory.CreateDirectory(filesRoot);

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
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
        Match match = Regex.Match(request.Path, @"(?<=/echo/)[^\s/]+");

        response = new HttpResponse("HTTP/1.1", 200, "OK")
            .AddHeader("Content-Type", "text/plain")
            .AddHeader("Content-Length", $"{match.Value.Length}");

        if (request.TryGetHeader("Accept-Encoding", out string? result) &&
            result.Split(",", StringSplitOptions.TrimEntries).Contains("gzip"))
        {
            response.Body = await CompressString(match.Value);
            response.AddHeader("Content-Encoding", "gzip")
                .AddHeader("Content-Length", $"{response.Body.Length}");
        }
        else
        {
            response.Body = match.Value;
        }
    }
    else if (request.Path.StartsWith("/files/") && request.Method == "GET")
    {
        Match match = Regex.Match(request.Path, @"(?<=/files/)[^\s/]+");

        string fileName = match.Value;

        try
        {
            await using var stream = new FileStream($"{filesRoot}/{fileName}", FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            string file = await reader.ReadToEndAsync();

            response = new HttpResponse("HTTP/1.1", 200, "OK")
                .AddHeader("Content-Type", "application/octet-stream")
                .AddHeader("Content-Length", $"{stream.Length}")
                .SetBody(file);
        }
        catch (FileNotFoundException)
        {
            response = new HttpResponse("HTTP/1.1", 404, "Not Found");
        }
    }
    else if (request.Path.StartsWith("/files/") && request.Method == "POST")
    {
        Match match = Regex.Match(request.Path, @"(?<=/files/)[^\s/]+");

        string fileName = match.Value;

        await using var stream = new FileStream($"{filesRoot}/{fileName}", FileMode.Create, FileAccess.Write);
        await using var writer = new StreamWriter(stream);

        await writer.WriteAsync(request.Body);

        response = new HttpResponse("HTTP/1.1", 201, "Created");
    }
    else if (request is { Path: "/user-agent", Method: "GET" })
    {
        string userAgent = request.GetHeader("User-Agent");
        
        response = new HttpResponse("HTTP/1.1", 200, "OK")
            .AddHeader("Content-Type", "text/plain")
            .AddHeader("Content-Length", userAgent.Length.ToString())
            .SetBody(userAgent);
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

async Task<byte[]> Compress(byte[] input)
{
    await using var memoryStream = new MemoryStream();

    byte[] lengthBytes = BitConverter.GetBytes(input.Length);
    await memoryStream.WriteAsync(lengthBytes.AsMemory(0, 4));

    await using var compressionStream = new GZipStream(memoryStream, CompressionMode.Compress);
    await compressionStream.WriteAsync(input);
    await compressionStream.FlushAsync();

    return memoryStream.ToArray();
}

async Task<string> CompressString(string input)
{
    byte[] encoded = Encoding.UTF8.GetBytes(input);
    byte[] compressed = await Compress(encoded);
    return Convert.ToBase64String(compressed);
}

async Task<byte[]> Decompress(byte[] input)
{
    await using var memoryStream = new MemoryStream(input);

    var lengthBytes = new byte[4];
    _ = await memoryStream.ReadAsync(lengthBytes.AsMemory(0, 4));
    
    var length = BitConverter.ToInt32(lengthBytes);
    var result = new byte[length];

    await using var compressionStream = new GZipStream(memoryStream, CompressionMode.Decompress);
    _ = await compressionStream.ReadAsync(result);

    return result;
}

async Task<string> DecompressString(string input)
{
    byte[] compressed = Convert.FromBase64String(input);
    byte[] decompressed = await Decompress(compressed);
    return Encoding.UTF8.GetString(decompressed);
}