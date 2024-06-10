using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using codecrafters_http_server;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
var server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    Socket socket = server.AcceptSocket(); // wait for client
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
    else if (request.Path == "/user-agent")
    {
        string userAgent = request.Headers["User-Agent"];
        
        response = new HttpResponse("HTTP/1.1", 200, "OK")
            .AddHeader("Content-Type", "text/plain")
            .AddHeader("Content-Length", userAgent.Length.ToString());
        response.Body = userAgent;
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