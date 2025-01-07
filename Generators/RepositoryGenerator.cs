using System.Text;
using System.Threading.Tasks;
using ProjectGenerator.Models;

namespace ProjectGenerator.Generators
{
    public class RepositoryGenerator
    {
        public async Task GenerateRepositoriesAsync(string projectPath, Project project)
        {
            var repositoriesPath = Path.Combine(projectPath, "Repositories");

            foreach (var repo in project.Repositories)
            {
                await GenerateRepositoryInterfaceAsync(repositoriesPath, repo, project.Name);
                await GenerateRepositoryImplementationAsync(repositoriesPath, repo, project);
            }
        }

        private async Task GenerateRepositoryInterfaceAsync(string path, Repository repo, string projectName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine($"using {projectName}.Models;");
            sb.AppendLine();
            sb.AppendLine($"namespace {projectName}.Repositories");
            sb.AppendLine("{");
            sb.AppendLine($"    public interface {repo.Name}");
            sb.AppendLine("    {");
            sb.AppendLine($"        Task<IEnumerable<{repo.Model}>> GetAllAsync();");
            sb.AppendLine($"        Task<{repo.Model}> GetByIdAsync(Guid id);");
            sb.AppendLine($"        Task<{repo.Model}> CreateAsync({repo.Model} entity);");
            sb.AppendLine($"        Task<{repo.Model}> UpdateAsync(Guid id, {repo.Model} entity);");
            sb.AppendLine("        Task<bool> DeleteAsync(Guid id);");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            await File.WriteAllTextAsync(
                Path.Combine(path, $"{repo.Name}.cs"),
                sb.ToString());
        }

        private async Task GenerateRepositoryImplementationAsync(string path, Repository repo, Project project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine($"using {project.Name}.Models;");
            sb.AppendLine($"using {project.Name}.Data;");
            sb.AppendLine();
            sb.AppendLine($"namespace {project.Name}.Repositories");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {repo.Name.Substring(1)} : {repo.Name}");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly {project.DbContext.Name} _context;"); // Use DbContext name from YAML
            sb.AppendLine();
            sb.AppendLine($"        public {repo.Name.Substring(1)}({project.DbContext.Name} context)"); // Constructor uses DbContext name
            sb.AppendLine("        {");
            sb.AppendLine("            _context = context;");
            sb.AppendLine("        }");
            sb.AppendLine();

            var navigationProperties = project.Models
                .FirstOrDefault(m => m.Name == repo.Model)?
                .Properties
                .Where(p => p.IsFK)
                .Select(p => p.References)
                .ToList();

            // Generate implementation methods
            GenerateRepositoryMethods(sb, repo.Model, navigationProperties);

            sb.AppendLine("    }");
            sb.AppendLine("}");

            await File.WriteAllTextAsync(
                Path.Combine(path, $"{repo.Name.Substring(1)}.cs"),
                sb.ToString());
        }

        private void GenerateRepositoryMethods(StringBuilder sb, string model, IEnumerable<string> navigationProperties)
        {
            // GetAllAsync
            sb.AppendLine($"        public async Task<IEnumerable<{model}>> GetAllAsync()");
            sb.AppendLine("        {");
            if (navigationProperties != null && navigationProperties.Any())
            {
                sb.AppendLine($"            return await _context.{model}s");
                foreach (var navProp in navigationProperties)
                {
                    sb.AppendLine($"                .Include(e => e.{navProp})");
                }
                sb.AppendLine("                .ToListAsync();");
            }
            else
            {
                sb.AppendLine($"            return await _context.{model}s.ToListAsync();");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // GetByIdAsync
            sb.AppendLine($"        public async Task<{model}> GetByIdAsync(Guid id)");
            sb.AppendLine("        {");
            if (navigationProperties != null && navigationProperties.Any())
            {
                sb.AppendLine($"            return await _context.{model}s");
                foreach (var navProp in navigationProperties)
                {
                    sb.AppendLine($"                .Include(e => e.{navProp})");
                }
                sb.AppendLine($"                .FirstOrDefaultAsync(e => e.Id == id);");
            }
            else
            {
                sb.AppendLine($"            return await _context.{model}s.FindAsync(id);");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // CreateAsync
            sb.AppendLine($"        public async Task<{model}> CreateAsync({model} entity)");
            sb.AppendLine("        {");
            sb.AppendLine($"            await _context.{model}s.AddAsync(entity);");
            sb.AppendLine("            await _context.SaveChangesAsync();");
            sb.AppendLine("            return entity;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // UpdateAsync
            sb.AppendLine($"        public async Task<{model}> UpdateAsync(Guid id, {model} entity)");
            sb.AppendLine("        {");
            sb.AppendLine("            var existingEntity = await GetByIdAsync(id);");
            sb.AppendLine("            if (existingEntity == null)");
            sb.AppendLine("                return null;");
            sb.AppendLine();
            sb.AppendLine("            _context.Entry(existingEntity).CurrentValues.SetValues(entity);");
            sb.AppendLine("            await _context.SaveChangesAsync();");
            sb.AppendLine("            return existingEntity;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // DeleteAsync
            sb.AppendLine("        public async Task<bool> DeleteAsync(Guid id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var entity = await GetByIdAsync(id);");
            sb.AppendLine("            if (entity == null)");
            sb.AppendLine("                return false;");
            sb.AppendLine();
            sb.AppendLine($"            _context.{model}s.Remove(entity);");
            sb.AppendLine("            await _context.SaveChangesAsync();");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
        }

    }
}