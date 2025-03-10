using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Configuration;

const string documentPath = "documents";
const string outputPath = "../../../output";

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var endpoint = new Uri(config["DocumentIntelligence:Endpoint"]!);
var credential = new AzureKeyCredential(config["DocumentIntelligence:Key"]!);

var client = new DocumentIntelligenceClient(endpoint, credential);

Console.WriteLine($"Analyzing documents from {documentPath}");

var directory = new DirectoryInfo(documentPath);
foreach (var file in directory.GetFiles())
{
    Console.WriteLine($"Analyzing file {file.Name}");
    
    var bytes = File.ReadAllBytes(Path.Combine(documentPath, file.Name));

    var options = new AnalyzeDocumentOptions("prebuilt-layout", BinaryData.FromBytes(bytes))
    {
        OutputContentFormat = DocumentContentFormat.Markdown
    };

    var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, options);

    Console.WriteLine($"Analyzed file {file.Name}");
    
    File.WriteAllText(Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file.Name)}.md"), operation.Value.Content);
}