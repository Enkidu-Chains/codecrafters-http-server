namespace codecrafters_http_server;

public delegate byte[] EndpointAction(byte[] request);

public class Endpoint
{
    private string _path;

    private HttpMethod _method;

    private EndpointAction _endpointAction;

    public Endpoint(string path, HttpMethod method, EndpointAction endpointAction)
    {
        Path = path;
        Method = method;
        EndpointAction = endpointAction;
    }
    
    public Endpoint(string path, string method, EndpointAction endpointAction)
    {
        Path = path;
        Method = HttpMethod.Parse(method);
        EndpointAction = endpointAction;
    } 

    public string Path
    {
        get => _path;
        private set
        {
            if (value is null)
            {
                throw new InvalidOperationException();
            }

            bool isValid = Uri.TryCreate(value, UriKind.Relative, out Uri? _);

            if (!isValid)
            {
                throw new InvalidOperationException();
            }

            _path = value;
        }
    }

    public HttpMethod Method
    {
        get => _method;
        private set => _method = value ?? throw new InvalidOperationException();
    }

    public EndpointAction EndpointAction
    {
        get => _endpointAction;
        set => _endpointAction = value ?? throw new InvalidOperationException();
    }
}