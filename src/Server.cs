using System.Net;
using System.Net.Sockets;
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

        if (request.GetHeader("Accept-Encoding") == "gzip")
        {
            response.AddHeader("Content-Encoding", "gzip");
        }
        
        response.Body = $"{match.Value}";
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