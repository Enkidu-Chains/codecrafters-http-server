using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_http_server;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
var server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Socket socket = server.AcceptSocket(); // wait for client

var buffer= new byte[1024];
await socket.ReceiveAsync(buffer);
string request = Encoding.UTF8.GetString(buffer);

Console.WriteLine(request);

string[] startLine = request.Split("\r\n")[0].Split(' ');

List<Endpoint> endpoints =
[
    new Endpoint("/echo/", HttpMethod.Get, requestBytes =>
    {
        string requestString = Encoding.UTF8.GetString(requestBytes);
        Match match = Regex.Match(requestString, @"(?<=/echo/)[^\s]+");

        return Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\n" +
                                      $"Content-Type: text/plain\r\n" +
                                      $"Content-Length: {match.Value.Length}\r\n" +
                                      $"\r\n" +
                                      $"{match.Value}");
    })
];

Endpoint? endpoint = endpoints
    .FirstOrDefault(
        e => startLine[1].StartsWith(e.Path) && e.Method == HttpMethod.Parse(startLine[0]));

if (endpoint is null)
{
    await socket.SendAsync("HTTP/1.1 404 Not Found\r\n\r\n"u8.ToArray());
}
else
{
    await socket.SendAsync(endpoint.EndpointAction.Invoke(buffer));
}
    
