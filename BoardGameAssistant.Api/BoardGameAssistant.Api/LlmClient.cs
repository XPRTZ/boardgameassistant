using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace BoardGameAssistant.Api;

public class LlmClient
{
    private readonly ChatHistory _history;
    private readonly Kernel _kernel;
    private readonly KernelFunction _qAndAPrompt;

    public LlmClient(Kernel kernel)
    {
        _history = [];
        _kernel = kernel;
        
        var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("BoardGameAssistant.Api.Prompts.qAndA.prompt.yaml")!;
        
        using StreamReader reader = new(stream);
        
        _qAndAPrompt = _kernel.CreateFunctionFromPromptYaml(
            reader.ReadToEnd(),
            promptTemplateFactory: new HandlebarsPromptTemplateFactory()
        );
    }

    public void AddUserMessage(string message)
    {
        _history.AddUserMessage(message);
    }

    public async Task<string> CreateResponseAsync(DocumentChunk[] documents)
    {
        var userMessage = _history.LastOrDefault(m => m.Role == AuthorRole.User);
        if (userMessage == null || _history.Last() != userMessage)
        {
            throw new ArgumentException("Last message must be a user message");
        }

        var context = CreateStringRepresentation(documents);
        
        var answer = await _qAndAPrompt.InvokeAsync<string>(
            _kernel,
            new KernelArguments { { "question", userMessage }, { "documents", context } }
        ) ?? "The LLM did not understand the question.";

        _history.AddAssistantMessage(answer);
        
        return JsonSerializer.Serialize(new { answer });
    }

    private string CreateStringRepresentation(DocumentChunk[] documents)
    {
        var sb = new StringBuilder();
        foreach (var document in documents)
        {
            var tempResult = $"""
                             # DOCUMENT
                             ## DOCUMENT_TITLE
                             {document.RuleBook}
                             ## DOCUMENT_URL
                             {document.Url}
                             ## DOCUMENT_TEXT
                             {document.Content}
                             ----------------------------------------------------------

                             """;
            sb.Append(tempResult);
        }
        return sb.ToString();
    }
}