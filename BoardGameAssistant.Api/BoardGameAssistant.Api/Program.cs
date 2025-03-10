using System.Reflection;
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
    return new RAGClient(kernel);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/assistant", async (string prompt, RAGClient client) =>
    {
        client.AddUserMessage(prompt);
        
        var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("BoardGameAssistant.Api.TestDocuments.Lisboa_rulebook_final.md")!;
        
        using StreamReader reader = new(stream);
        
        var document = reader.ReadToEnd();
        
        return await client.CreateResponseAsync( [document]);
    })
    .WithName("BoardGame Assistant API")
    .WithOpenApi();

app.Run();