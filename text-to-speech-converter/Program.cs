using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// importing API from secrets
builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

// checks if there is a PORT variable, if not, assigns to 8080
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// configuring server's URL
builder.WebHost.UseUrls($"http://0.0.0.0{port}");

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/tts/convert",
            Period = "1m",
            Limit = 2  // 2 requests per minute per IP
        },
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1h",
            Limit = 10  // 10 total requests per hour per IP
        }
    };
});


builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// https redirection when in development
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable Rate Limiting (must be before controllers)
app.UseIpRateLimiting();

// Enable static files (HTML, CSS, JS)
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable controllers
app.MapControllers();

app.Run();
