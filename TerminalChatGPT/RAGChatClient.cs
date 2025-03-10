using System.Reflection;
using System.Text;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using TalkToYourData.Evaluator;

namespace TalkToYourData;

public class RAGChatClient : IGenAIEvaluatable<RAGChatResponse>
{
    private readonly SearchClient _searchClient;
    private readonly string _blobStorageUrl;
    private readonly Kernel _kernel;
    private KernelFunction _qAndAPrompt;

    public RAGChatClient(SearchClient searchClient, Kernel kernel, string blobStorageUrl)
    {
        _searchClient = searchClient;
        _kernel = kernel;
        _blobStorageUrl = blobStorageUrl;

        var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("TalkToYourData.Prompts.qAndA.prompt.yaml")!;
        using StreamReader reader = new(stream);

        _qAndAPrompt = _kernel.CreateFunctionFromPromptYaml(
            reader.ReadToEnd(),
            promptTemplateFactory: new HandlebarsPromptTemplateFactory()
        );
    }

    public async Task<RAGChatResponse> CreateResponseAsync(ChatHistory chatHistory)
    {
        ChatMessageContent? userMessage = chatHistory.LastOrDefault(m => m.Role == AuthorRole.User);
        if (userMessage == null || chatHistory.Last() != userMessage)
        {
            throw new ArgumentException("Last message must be a user message");
        }

        string userQuestion = userMessage.Content!;

        // Create the query to send to the search service
        VectorizableTextQuery vectorQuery = new VectorizableTextQuery(userQuestion)
        {
            KNearestNeighborsCount = 5,
            Fields = { "text_vector" },
            Exhaustive = true
        };

        SearchResults<SearchDocument> searchResults =
            await _searchClient.SearchAsync<SearchDocument>(
                new SearchOptions()
                {
                    VectorSearch = new VectorSearchOptions() { Queries = { vectorQuery } }
                }
            );

        // Convert search results to nicely formatted string representation
        string context = CreateStringRepresentation(searchResults);

        // Get the response from the AI
        string? answer = await _qAndAPrompt.InvokeAsync<string>(
            _kernel,
            new() { { "question", userMessage }, { "documents", context } }
        );

        RAGChatResponse ragChatResponse = new RAGChatResponse(
            Guid.NewGuid(),
            userQuestion,
            answer,
            context
        );

        return ragChatResponse;
    }

    private string CreateStringRepresentation(SearchResults<SearchDocument> searchResults)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (SearchResult<SearchDocument> searchResult in searchResults.GetResults())
        {
            SearchDocument document = searchResult.Document;
            int pageNumber =
                Convert.ToInt32(document["chunk_id"]?.ToString().Split('_').Last()) + 1;

            string tempResult = $"""
                # DOCUMENT
                ## DOCUMENT_URL
                {_blobStorageUrl}/{document["title"]}#page={pageNumber}
                ## DOCUMENT_CONTENT
                {document["chunk"]}
                ----------------------------------------------------------
                
                """;
            stringBuilder.Append(tempResult);
        }
        return stringBuilder.ToString();
    }
}
