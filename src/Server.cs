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
}

string GetFilesRoot(string[] args)
{
    try
    {
        int index = Array.IndexOf(args, "--directory") + 1;
        return args[index];
    }
    catch (IndexOutOfRangeException e)
    {
        Console.WriteLine("The parameter for \"--directory\" was not set properly.");
        return args[0];
    }
}

async Task HandleConnection(Socket socket)
{
    var requestBytes = new byte[10*1024];
    int byteReceived = await socket.ReceiveAsync(requestBytes);

    var request = new HttpRequest(requestBytes);
    HttpResponse response;

    if (request.Path.StartsWith("/echo/"))
    {
        Match match = Regex.Match(request.Path, @"(?<=/echo/)[^\s/]+");

        response = new HttpResponse("HTTP/1.1", 200, "OK")
            .AddHeader("Content-Type", "text/plain")
            .AddHeader("Content-Length", $"{match.Value.Length}");
        response.Body = $"{match.Value}";
    }
    else if (request.Path.StartsWith("/files/"))
    {
        Match match = Regex.Match(request.Path, @"(?<=/files/)[^\s/]+");

        string fileName = match.Value;

        try
        {
            await using var stream = new FileStream($"{filesRoot}/{fileName}", FileMode.Open);

            var memory = new Memory<byte>();
            int readAsync = await stream.ReadAsync(memory);

            response = new HttpResponse("HTTP/1.1", 200, "OK")
                .AddHeader("Content-Type", "application/octet-stream")
                .AddHeader("Content-Length", $"{readAsync}")
                .SetBody(memory.ToString());

        }
        catch (FileNotFoundException e)
        {
            response = new HttpResponse("HTTP/1.1", 404, "Not Found");
        }
    }
    else if (request.Path == "/user-agent")
    {
        string userAgent = request.Headers["User-Agent"];
        
        response = new HttpResponse("HTTP/1.1", 200, "OK")
            .AddHeader("Content-Type", "text/plain")
            .AddHeader("Content-Length", userAgent.Length.ToString())
            .SetBody(userAgent);
    }
    else if (request.Path == "/")
    {
        response = new HttpResponse("HTTP/1.1", 200, "OK");
    }
    else
    {
        response = new HttpResponse("HTTP/1.1", 404, "Not Found");
    }

    await socket.SendAsync(response.ToBytes());
}