using Azure;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using TalkToYourData;
using TalkToYourData.Evaluator;

// Loading configuration
IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// Azure AI Search configuration
Uri searchEndpoint = new Uri(config["AzureAISearch:Endpoint"]!);
AzureKeyCredential searchKey = new AzureKeyCredential(config["AzureAISearch:AzureKeyCredential"]!);
string indexName = "gadgetspherepolicies";

// Blob storage configuration
string blobStorageUrl = config["BlobStorage:BlobContainerEndpoint"]!;

IKernelBuilder builder = Kernel.CreateBuilder();

builder.Services.AddAzureOpenAIChatCompletion(
    endpoint: config["AzureOpenAI:Endpoint"]!,
    apiKey: config["AzureOpenAI:AzureKeyCredential"]!,
    deploymentName: "gpt-4o"
);

Kernel kernel = builder.Build();

// Set up the Azure AI Search client
SearchClient searchClient = new SearchClient(searchEndpoint, indexName, searchKey);

RAGChatClient ragChatClient = new RAGChatClient(searchClient, kernel, blobStorageUrl);

Dictionary<string, IGenAIEvaluatable<RAGChatResponse>> evaluationTargets =
    new() { ["GPT-4o-eval"] = ragChatClient };

string inputDataSet = @"Evaluation\evaluation-data.jsonl";
string outputLocation = "Evaluation";

// Add logging configuration
var serviceProvider = new ServiceCollection()
    .AddLogging(configure => configure.AddConsole())
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILogger<GenAIEvaluator<RAGChatClient, RAGChatResponse>>>();

GenAIEvaluator<RAGChatClient, RAGChatResponse> genAiEvaluator =
    new(inputDataSet, outputLocation, logger);

await genAiEvaluator.BatchEvaluateAsync(
    evaluationTargets: evaluationTargets,
    constructHistory: input =>
    {
        ChatHistory history = [];
        history.AddUserMessage(input.Question);
        return history;
    },
    constructOutput: (RAGChatResponse response, EvaluationInput input) =>
    {
        var evaluationOutput = new EvaluationOutput
        {
            Question = input.Question,
            Answer = response.Answer,
            Context = response.Context,
            GroundTruth = input.GroundTruth
        };
        return evaluationOutput;
    }
);
