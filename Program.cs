using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;
using CS397.Trace;

public class HelloMessage
{
    public string Message { get; set; } = string.Empty;
}

class Program
{
    const string serviceName = "monitored-docker-web-service";
    const string serviceVersion = "1.0.0";

    private readonly Tracer _tracer;

    public Program(){
        _tracer = TracerProvider.Default.GetTracer(serviceName);
    }
    private async Task HelloWorldDelegate(HttpContext context){
        Console.WriteLine("Hello Called");
        await context.Response.WriteAsync("Hello World!");
    }

    private async Task GoodbyeDelegate(HttpContext context){
        Console.WriteLine("Goodbye Called");
        await context.Response.WriteAsync("Goodbye World!");
    }

    private async Task HelloJsonDelegate(HttpContext context){
        HttpRequest request = context.Request;

        string echostring = "Hello, World!";

        if (request.Query.TryGetValue("Message", out StringValues messageValues))
        {
            echostring = messageValues[0];
        }

        Console.WriteLine(echostring);

        await context.Response.WriteAsync(echostring);
    }

    static void Main(String[] args){
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenTelemetry().WithTracing(tcb =>{
            tcb
            .AddSource(serviceName)
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .AddAspNetCoreInstrumentation()
            .AddJsonConsoleExporter();
        });

        Program instance = new Program();
        WebApplication app = builder.Build();

        app.MapGet("/", instance.HelloWorldDelegate);
        app.MapGet("/hello", instance.HelloWorldDelegate);
        app.MapGet("/goodbye", instance.GoodbyeDelegate);
        app.MapGet("/hellojson", instance.HelloJsonDelegate);

        app.Run();
    }
}
