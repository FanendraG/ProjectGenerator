using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using System;

var generator = new ProjectGenerator.Generators.ProjectStructureGenerator();

// Check if arguments are provided
string yamlFilePath;
#if DEBUG
// Provide the path to the YAML file in debug mode
yamlFilePath = @"C:\GitHubProjects\NZWalks\ProjectGenerator\config\parking.yaml";
Console.WriteLine("Running in debug mode. Using hardcoded YAML path: " + yamlFilePath);
#else
// Use the first command-line argument in release mode
if (args.Length == 0)
{
    Console.WriteLine("Please provide the path to the YAML file as a command-line argument.");
    return;
}
yamlFilePath = args[0];
#endif

await generator.GenerateAsync(yamlFilePath);

