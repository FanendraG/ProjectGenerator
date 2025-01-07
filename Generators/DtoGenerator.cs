using System.Text;
using System.Threading.Tasks;
using ProjectGenerator.Models;

namespace ProjectGenerator.Generators
{
    public class DtoGenerator
    {
        public async Task GenerateDtosAsync(string projectPath, Project project)
        {
            var dtoPath = Path.Combine(projectPath, "Models", "DTOs");
            Directory.CreateDirectory(dtoPath);

            foreach (var dto in project.Dtos)
            {
                await GenerateDtoFileAsync(dtoPath, dto, project.Name);
            }

            await GenerateAutoMapperProfileAsync(projectPath, project);
        }

        private async Task GenerateDtoFileAsync(string path, Dto dto, string projectName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine();
            sb.AppendLine($"namespace {projectName}.Models.DTOs");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {dto.Name}");
            sb.AppendLine("    {");

            foreach (var prop in dto.Properties)
            {
                if (prop.Required)
                    sb.AppendLine("        [Required]");
                if (prop.MaxLength.HasValue)
                    sb.AppendLine($"        [MaxLength({prop.MaxLength})]");
                if (prop.Nullable)
                    sb.AppendLine($"        public {prop.Type}? {prop.Name} {{ get; set; }}");
                else
                    sb.AppendLine($"        public {prop.Type} {prop.Name} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            await File.WriteAllTextAsync(Path.Combine(path, $"{dto.Name}.cs"), sb.ToString());
        }

        private async Task GenerateAutoMapperProfileAsync(string projectPath, Project project)
        {
            var mappingPath = Path.Combine(projectPath, "Mappings");
            Directory.CreateDirectory(mappingPath);
            var sb = new StringBuilder();

            sb.AppendLine("using AutoMapper;");
            sb.AppendLine($"using {project.Name}.Models;");
            sb.AppendLine($"using {project.Name}.Models.DTOs;");
            sb.AppendLine();
            sb.AppendLine($"namespace {project.Name}.Mappings");
            sb.AppendLine("{");
            sb.AppendLine("    public class AutoMapperProfile : Profile");
            sb.AppendLine("    {");
            sb.AppendLine("        public AutoMapperProfile()");
            sb.AppendLine("        {");

            foreach (var dto in project.Dtos)
            {
                sb.AppendLine($"            CreateMap<{dto.Model}, {dto.Name}>();");
                sb.AppendLine($"            CreateMap<{dto.Name}, {dto.Model}>();");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            await File.WriteAllTextAsync(Path.Combine(mappingPath, "AutoMapperProfile.cs"), sb.ToString());
        }
    }
}
