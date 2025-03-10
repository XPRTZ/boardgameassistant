using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;

namespace TalkToYourData.Evaluator;

public class GenAIEvaluator<TEvaluationTarget, TGenAIResponse>(
    string inputDataSetLocation,
    string outputDirectory,
    ILogger<GenAIEvaluator<TEvaluationTarget, TGenAIResponse>>? logger = null
)
    where TEvaluationTarget : IGenAIEvaluatable<TGenAIResponse>
{
    public async Task BatchEvaluateAsync<TEvalIn, TEvalOut>(
        Dictionary<string, IGenAIEvaluatable<TGenAIResponse>> evaluationTargets,
        Func<TEvalIn, ChatHistory> constructHistory,
        Func<TGenAIResponse, TEvalIn, TEvalOut> constructOutput
    )
    {
        foreach (var evaluationTarget in evaluationTargets)
        {
            logger?.LogInformation(
                "Starting evaluation for target: {EvaluationTarget}",
                evaluationTarget.Key
            );
            await EvaluateAsync(
                evaluationTarget.Key,
                evaluationTarget.Value,
                constructHistory,
                constructOutput
            );
            logger?.LogInformation(
                "Completed evaluation for target: {EvaluationTarget}",
                evaluationTarget.Key
            );
        }
    }

    public async Task EvaluateAsync<TEvalIn, TEvalOut>(
        string evaluationOutputName,
        IGenAIEvaluatable<TGenAIResponse> evaluationTarget,
        Func<TEvalIn, ChatHistory> constructHistory,
        Func<TGenAIResponse, TEvalIn, TEvalOut> constructOutput
    )
    {
        var outputFilePath = Path.Combine(outputDirectory, $"{evaluationOutputName}.jsonl");
        logger?.LogInformation(
            "Evaluation output will be written to: {OutputFilePath}",
            outputFilePath
        );

        using var inputStream = new StreamReader(inputDataSetLocation);
        using var outputStream = new StreamWriter(outputFilePath);

        string? line;
        int index = 0;
        while ((line = await inputStream.ReadLineAsync()) != null)
        {
            logger?.LogInformation($"Evaluating entry {index}");
            TEvalIn? inputData = JsonSerializer.Deserialize<TEvalIn>(line);
            if (inputData == null)
            {
                logger?.LogWarning("Skipping null input data line.");
                continue;
            }

            // Create chat history with the user's question
            ChatHistory? chatHistory = constructHistory(inputData);

            // Generate response using RAGChatClient
            TGenAIResponse? response = await evaluationTarget.CreateResponseAsync(chatHistory);

            // Prepare output data
            TEvalOut evaluationOutput = constructOutput(response, inputData);

            // Write the result to the output JSONL file
            var outputLine = JsonSerializer.Serialize(
                evaluationOutput,
                new JsonSerializerOptions() { WriteIndented = false, }
            );
            await outputStream.WriteLineAsync(outputLine);
            index++;
        }
    }
}
