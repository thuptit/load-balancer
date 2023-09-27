using System.Net;
using System.Text;

const int timeout = 5000;
var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:8083/");
Console.WriteLine("Start Server 3 ...");
httpListener.Start();
while (true)
{
    var context = await httpListener.GetContextAsync();
    var request = context.Request;
    Console.WriteLine("Received request");
    
    using HttpListenerResponse resp = context.Response;
    resp.Headers.Set("Content-Type", "text/plain");

    string data = "Response from server 3";
    byte[] buffer = Encoding.UTF8.GetBytes(data);
    resp.ContentLength64 = buffer.Length;

    using Stream ros = resp.OutputStream;
    ros.Write(buffer, 0, buffer.Length);
}