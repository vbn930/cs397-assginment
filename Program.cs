class Program
{
    private static async Task HelloWorldDelegate(HttpContext context){
        await context.Response.WriteAsync("Hello World!");
    }

    private static async Task GoodbyeDelegate(HttpContext context){
        await context.Response.WriteAsync("Goodbye World!");
    }

    static void Main(String[] args){
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/", HelloWorldDelegate);
        app.MapGet("/hello", HelloWorldDelegate);
        app.MapGet("/goodbye", GoodbyeDelegate);

        app.Run();
    }
}
