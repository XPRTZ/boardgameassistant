using System.Reflection;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using OpenAI.Chat;

namespace BoardGameAssistant.Api;

public class RAGClient
{
    private readonly ChatHistory _history;
    private readonly Kernel _kernel;
    private readonly KernelFunction _qAndAPrompt;

    public RAGClient(Kernel kernel)
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

    public async Task<string> CreateResponseAsync(string[] documents)
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
        
        return answer;
    }

    private string GetDocument()
    {
        var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("BoardGameAssistant.Api.TestDocuments.Lisboa_rulebook_final.md")!;
        
        using StreamReader reader = new(stream);
        
        return reader.ReadToEnd();
    }

    private string CreateStringRepresentation(string[] documents)
    {
        var sb = new StringBuilder();
        foreach (var document in documents)
        {
            var tempResult = $"""
                             # DOCUMENT
                             {document}
                             ----------------------------------------------------------

                             """;
            sb.Append(tempResult);
        }
        return sb.ToString();
    }
}