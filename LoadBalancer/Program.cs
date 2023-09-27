using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

var lstServers = new List<ServerInfo>()
{
    new ServerInfo() { Address = "http://localhost:8081/" },
    new ServerInfo() { Address = "http://localhost:8082/" },
    new ServerInfo() { Address = "http://localhost:8083/" }
};
int serverPos = -1;
const int timeout = 5000;
var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:8080/");
httpListener.Start();
Console.WriteLine("Starting Load Balancer ...");
while (true)
{
    await CheckHealthyServers();
    var context = await httpListener.GetContextAsync();
    var request = context.Request;

    int retries = 3;
    for (int i = retries; i >= 1; i--)
    {
        var server = GetNextServer();
        if (server.Item1 == null)
        {
            using HttpListenerResponse resp = context.Response;
            resp.Headers.Set("Content-Type", "text/plain");

            string data = "Internal Server Error";
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            resp.ContentLength64 = buffer.Length;

            using Stream ros = resp.OutputStream;
            ros.Write(buffer, 0, buffer.Length);
            break;
        }

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(server.Item1.Address);
            using HttpListenerResponse resp = context.Response;
            foreach (var header in response.Headers)
            {
                resp.Headers.Set(header.Key, header.Value.FirstOrDefault());
            }

            var content = await response.Content.ReadAsByteArrayAsync();
            using Stream ros = resp.OutputStream;
            ros.Write(content, 0, content.Length);
            break;
        }
    }
}

(ServerInfo, string) GetNextServer()
{
    var inactiveCount = 0;
    serverPos = (serverPos + 1) % lstServers.Count;
    if (!lstServers[serverPos].IsActive)
    {
        //TODO
        return (null, null);
    }
    else
    {
        return (lstServers[serverPos],string.Empty);
    }
}

async Task CheckHealthyServers()
{
    for (int i = 0; i < lstServers.Count; i++)
    {
        if (await IsHealthy(lstServers[i]))
        {
            ActiveServer(lstServers[i]);
        }
        else
        {
            DeactiveServer(lstServers[i]);
        }
    }
}

async Task<bool> IsHealthy(ServerInfo server)
{
    
    using (var httpClient = new HttpClient())
    {
        var response = await httpClient.GetAsync(server.Address);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            return true;
        }

        return false;
    }
}

void ActiveServer(ServerInfo server)
{
    server.IsActive = true;
}

void DeactiveServer(ServerInfo server)
{
    server.IsActive = false;
}
class ServerInfo
{
    public string Address { get; set; }
    public bool IsActive { get; set; }
}
