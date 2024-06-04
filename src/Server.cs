using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
var server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Socket socket = server.AcceptSocket(); // wait for client

var buffer = new byte[1024];

await socket.ReceiveAsync(buffer);

string request = Encoding.UTF8.GetString(buffer);

Console.WriteLine(request);

string[] requestLines = request.Split("\n\r");

bool validEndpoint = requestLines[0].Split(' ')[1].Equals("/");

string response = validEndpoint ? "HTTP/1.1 200 OK\r\n\r\n" : "HTTP/1.1 404 NotFound\r\n\r\n";

await socket.SendAsync(Encoding.UTF8.GetBytes(response));
