// namespace codecrafters_http_server;
//
// public delegate byte[] EndpointAction(byte[] request);
//
// // TODO: smart endpoint path (or endpoint regex)
// public class Endpoint
// {
//
//     private HttpMethod _method;
//
//     private EndpointAction _endpointAction;
//
//     public Endpoint(string path, HttpMethod method, EndpointAction endpointAction)
//     {
//         Path = path;
//         Method = method;
//         EndpointAction = endpointAction;
//     }
//     
//     public Endpoint(string path, string method, EndpointAction endpointAction)
//     {
//         Path = path;
//         Method = HttpMethod.Parse(method);
//         EndpointAction = endpointAction;
//     }
//
//     public HttpMethod Method
//     {
//         get => _method;
//         private set => _method = value ?? throw new InvalidOperationException();
//     }
//
//     public EndpointAction EndpointAction
//     {
//         get => _endpointAction;
//         set => _endpointAction = value ?? throw new InvalidOperationException();
//     }
// }