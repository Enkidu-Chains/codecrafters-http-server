using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
var server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Socket socket = server.AcceptSocket(); // wait for client

var buffer= new byte[1024];
await socket.ReceiveAsync(buffer);
string request = Encoding.UTF8.GetString(buffer);
byte[] response;

Console.WriteLine(request);

string[] startLine = request.Split("\r\n")[0].Split(' ');

if (startLine[1].StartsWith("/echo/"))
{
    Match match = Regex.Match(startLine[1], @"(?<=/echo/)[^\s/]+");

    response = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n" +
                                             "Content-Type: text/plain\r\n" +
                                             $"Content-Length: {match.Value.Length}\r\n" +
                                             "\r\n" +
                                             $"{match.Value}");
}
else if (startLine[1] == "/")
{
    response = "HTTP/1.1 200 OK\r\n\r\n"u8.ToArray();
}
else
{
    response = "HTTP/1.1 404 Not Found\r\n\r\n"u8.ToArray();
}

await socket.SendAsync(response);