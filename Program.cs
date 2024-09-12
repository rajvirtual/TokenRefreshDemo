namespace TokenRefreshDemo
{
    using Serilog;
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
         .ReadFrom.Configuration(builder.Configuration)
         .WriteTo.Console()
         .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
         .CreateLogger();

            builder.Services.AddLogging(loggingBuilder =>
                        {
                            loggingBuilder.AddSerilog();
                        });

            builder.Services.AddHostedService<TokenRefreshService>();

            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.Run();
        }
    }
}
