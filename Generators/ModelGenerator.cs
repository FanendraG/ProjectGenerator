using System.Text;
using System.Threading.Tasks;
using ProjectGenerator.Models;

namespace ProjectGenerator.Generators
{
    public class ModelGenerator
    {
        public async Task GenerateModelsAsync(string projectPath, Project project)
        {
            var modelsPath = Path.Combine(projectPath, "Models");
            Directory.CreateDirectory(modelsPath);

            foreach (var model in project.Models)
            {
                await GenerateModelFileAsync(modelsPath, model, project);
            }
        }

        private async Task GenerateModelFileAsync(string path, Model model, Project project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine();
            sb.AppendLine($"namespace {project.Name}.Models");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {model.Name}");
            sb.AppendLine("    {");

            foreach (var prop in model.Properties)
            {
                if (prop.IsPrimaryKey)
                    sb.AppendLine("        [Key]");

                if (prop.IsFK)
                    sb.AppendLine($"        [ForeignKey(\"{prop.References}\")]");

                if (prop.Required)
                    sb.AppendLine("        [Required]");

                if (prop.MaxLength.HasValue)
                    sb.AppendLine($"        [MaxLength({prop.MaxLength})]");

                sb.AppendLine($"        public {prop.Type} {prop.Name} {{ get; set; }}");
                sb.AppendLine();
            }

            // Add navigation properties for foreign keys
            foreach (var prop in model.Properties.Where(p => p.IsFK))
            {
                sb.AppendLine($"        public {prop.References} {prop.References} {{ get; set; }}"); // Navigation property for FK
            }

            // Add collection navigation properties for models referencing this one
            var referencingModels = project.Models
                .Where(m => m.Properties.Any(p => p.IsFK && p.References == model.Name));

            foreach (var referencingModel in referencingModels)
            {
                sb.AppendLine($"        public ICollection<{referencingModel.Name}> {referencingModel.Name}s {{ get; set; }}"); // Collection navigation
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            await File.WriteAllTextAsync(Path.Combine(path, $"{model.Name}.cs"), sb.ToString());
        }
    }
}

