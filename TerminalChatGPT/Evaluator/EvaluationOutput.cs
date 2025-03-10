using System.Text.Json.Serialization;

namespace TalkToYourData.Evaluator;

public class EvaluationOutput
{
    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public string Context { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("ground_truth")]
    public string GroundTruth { get; set; } = string.Empty;
}
