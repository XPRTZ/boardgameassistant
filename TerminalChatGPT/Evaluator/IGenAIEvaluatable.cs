using Microsoft.SemanticKernel.ChatCompletion;

namespace TalkToYourData.Evaluator;

public interface IGenAIEvaluatable<T>
{
Task<T> CreateResponseAsync(ChatHistory chatHistory);
}