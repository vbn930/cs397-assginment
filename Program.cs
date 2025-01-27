using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Microsoft.Extensions.Primitives;

using System.Text.Json;

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

        TelemetrySpan currentSpan = _tracer.StartSpan("HelloWorldDelegate");
        currentSpan.SetAttribute("http.method", context.Request.Method);
        currentSpan.SetAttribute("http.url", context.Request.Path);

        context.Response.Headers.Append("x-trace-id", currentSpan.Context.TraceId.ToString());

        try{

            await context.Response.WriteAsync("Hello, World!");

        }catch(Exception e){
            currentSpan.SetAttribute("error", true);
            currentSpan.SetAttribute("error.message", e.Message);
            currentSpan.SetAttribute("error.stacktrace", e.StackTrace);

            context.Response.StatusCode = 500;
        }finally{
            currentSpan.End();
        }
    }

    private async Task GoodbyeDelegate(HttpContext context){
        TelemetrySpan currentSpan = _tracer.StartSpan("HelloWorldDelegate");
        currentSpan.SetAttribute("http.method", context.Request.Method);
        currentSpan.SetAttribute("http.url", context.Request.Path);

        context.Response.Headers.Append("x-trace-id", currentSpan.Context.TraceId.ToString());

        try{

            await context.Response.WriteAsync("Goodbye, World!");

        }catch(Exception e){
            currentSpan.SetAttribute("error", true);
            currentSpan.SetAttribute("error.message", e.Message);
            currentSpan.SetAttribute("error.stacktrace", e.StackTrace);

            context.Response.StatusCode = 500;
        }finally{
            currentSpan.End();
        }
    }

    private async Task HelloJsonDelegate(HttpContext context){
        // OpenTelemetry uses concepts of "span" to correlate all messages together
        // from the same operation. We start with a new span, include some attributes,
        // and then wrap the work in a try-catch-finally block to ensure the span is
        // ended even if an exception is thrown, and to include error information in
        // the span.
        TelemetrySpan currentSpan = _tracer.StartSpan("HelloJsonWorldDelegate");
        currentSpan.SetAttribute("http.method", context.Request.Method);
        currentSpan.SetAttribute("http.url", context.Request.Path);

        // Let's add the current TraceId to the response headers
        // Remember, good behavior is to include "x-" in front of custom headers
        context.Response.Headers.Append("x-trace-id", currentSpan.Context.TraceId.ToString());

        try
        {
            HttpRequest request = context.Request;

            string echostring = "Hello, World!";
            if (request.Query.TryGetValue("Message", out StringValues messageValues))
            {
                echostring = messageValues[0];
                currentSpan.SetAttribute("message", echostring);
            }
            else
            {
                currentSpan.SetAttribute("message", "<empty>");
            }
            
            HelloMessage m = new HelloMessage();
            m.Message = echostring ?? "<empty>";
            string jsonString = JsonSerializer.Serialize(m);

            HttpResponse response = context.Response;
            response.ContentLength = jsonString.Length;
            response.ContentType = "application/json";

            await context.Response.WriteAsync(jsonString);
        }
        catch(Exception e)
        {
            // If any error happend via an exception, we can include that information
            // in the log. Including the stack trace can be either useful or very
            // noisy, so you often control that with log levels.
            currentSpan.SetAttribute("error", true);
            currentSpan.SetAttribute("error.message", e.Message);
            currentSpan.SetAttribute("error.stacktrace", e.StackTrace);
            
            // 500 is the typical status code for an internal server error
            // We got an unhandled exception, so we don't know what went wrong
            // Hence we log the information and return a 500 status code
            context.Response.StatusCode = 500;
        }
        finally
        {
            // Ending the span will automatically include the time the entire operation took
            currentSpan.End();
        }
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
