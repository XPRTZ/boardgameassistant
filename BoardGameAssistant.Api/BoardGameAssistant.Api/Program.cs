using BoardGameAssistant.Api;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(services =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    
    kernelBuilder.Services.AddAzureOpenAIChatCompletion(
        endpoint: builder.Configuration["AzureOpenAI:Endpoint"]!,
        apiKey: builder.Configuration["AzureOpenAI:Key"]!,
        deploymentName: builder.Configuration["AzureOpenAI:Deployment"]!
    );
    
    return kernelBuilder.Build();
});
builder.Services.AddSingleton(services =>
{
    var kernel = services.GetRequiredService<Kernel>();
    return new LlmClient(kernel);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "MyAllowSpecificOrigins",
        builder =>
        {
            builder.WithOrigins("*", "http://example.com",
                "http://www.contoso.com");
        });
});




var app = builder.Build();

app.UseCors("MyAllowSpecificOrigins");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/assistant/{prompt}", async (string prompt, DocumentChunk[] chunks, LlmClient client) =>
    {
        client.AddUserMessage(prompt);
        
        return await client.CreateResponseAsync(chunks);
    })
    .WithName("BoardGame Assistant API")
    .WithOpenApi();

app.Run();