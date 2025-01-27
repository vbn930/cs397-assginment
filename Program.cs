public class HelloMessage
{
    public string Message { get; set; } = string.Empty;
}

class Program
{
    private static async Task HelloWorldDelegate(HttpContext context){
        Console.WriteLine("Hello Called");
        await context.Response.WriteAsync("Hello World!");
    }

    private static async Task GoodbyeDelegate(HttpContext context){
        Console.WriteLine("Goodbye Called");
        await context.Response.WriteAsync("Goodbye World!");
    }

    private static async Task HelloJsonDelegate(HttpContext context){
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
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/", HelloWorldDelegate);
        app.MapGet("/hello", HelloWorldDelegate);
        app.MapGet("/goodbye", GoodbyeDelegate);
        app.MapGet("/hellojson", HelloJsonDelegate);

        app.Run();
    }
}
