using System.Text.Json.Serialization;

namespace TalkToYourData.Evaluator;

public class EvaluationInput
{
    [JsonPropertyName("ground_truth")]
    public string GroundTruth { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;
}
