using InMemoryInlineWebApiBridge;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NServiceBus;

var builder = WebApplication.CreateBuilder(args);

var demoState = new DemoState();
var sharedBroker = new InMemoryBroker();
var sharedStorage = new InMemoryStorage();

builder.Services.AddSingleton(demoState);
builder.Services.AddSingleton(sharedBroker);
builder.Services.AddSingleton(sharedStorage);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddSampleEndpoints(builder.Configuration, sharedBroker, sharedStorage, demoState);
builder.AddSampleBridge(builder.Configuration, sharedBroker, demoState);

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;
        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Title = "Unhandled exception",
                Status = StatusCodes.Status500InternalServerError,
                Detail = exception?.Message
            }
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "InMemoryInlineWebApiBridge v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapControllers();

app.MapGet("/", (IConfiguration configuration) => Results.Ok(new
{
    sample = "InMemory inline Web API + bridge",
    openApi = "/openapi/v1.json",
    swagger = "/swagger",
    endpoints = new[]
    {
        "/api/demo/retries",
        "/api/demo/bubble",
        "/api/demo/bridge",
        "/api/demo/state"
    },
    bridgeAzureEndpoint = "Samples.InMemoryInlineWebApiBridge.AzureReceiver",
    bridgeAzureErrorQueue = "error"
}));

await app.RunAsync();
