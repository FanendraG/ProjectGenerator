using System;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ProjectGenerator.Models;
using System.Text;
using System.Text.Json;

namespace ProjectGenerator.Generators
{
    public class ProjectStructureGenerator
    {
        private readonly ControllerGenerator _controllerGenerator;
        private readonly ModelGenerator _modelGenerator = new ModelGenerator();
        private readonly DtoGenerator _dtoGenerator;
        private readonly RepositoryGenerator _repositoryGenerator;
        private string _basePath;

        public ProjectStructureGenerator()
        {
            _controllerGenerator = new ControllerGenerator();
            _dtoGenerator = new DtoGenerator();
            _repositoryGenerator = new RepositoryGenerator();
        }

        public async Task GenerateAsync(string yamlPath)
        {
            var yamlContent = await File.ReadAllTextAsync(yamlPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<ProjectConfiguration>(yamlContent);

            // Use outputPath from the YAML if provided, otherwise use the current directory
            _basePath = string.IsNullOrWhiteSpace(config.Solution.OutputPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), config.Solution.Name)
                : Path.Combine(config.Solution.OutputPath, config.Solution.Name);

            await GenerateProjectStructureAsync(config);
        }

        private async Task GenerateProjectStructureAsync(ProjectConfiguration config)
        {
            Directory.CreateDirectory(_basePath);

            foreach (var project in config.Solution.Projects)
            {
                var projectPath = Path.Combine(_basePath, project.Name);
                Directory.CreateDirectory(projectPath);

                // Create project folders
                CreateProjectFolders(projectPath);

                // Generate components
                await _modelGenerator.GenerateModelsAsync(projectPath, project);
                await _dtoGenerator.GenerateDtosAsync(projectPath, project);
                await _repositoryGenerator.GenerateRepositoriesAsync(projectPath, project);
                await _controllerGenerator.GenerateControllersAsync(projectPath, project);

                // Generate project file
                await GenerateProjectFileAsync(projectPath, project);

                //Generate EF DB Context and create Seed values
                await GenerateDbContextAsync(projectPath, project);

                // Generate Program.cs and other configuration files
                await GenerateConfigurationFilesAsync(projectPath, project);

                
            }

            // Generate solution file
            await GenerateSolutionFileAsync(config);            
        }

        private void CreateProjectFolders(string projectPath)
        {
            var folders = new[]
            {
                "Controllers",
                "Models",
                "Models/DTOs",
                "Data",
                "Repositories",
                "Mappings",
                "Migrations"
            };

            foreach (var folder in folders)
            {
                Directory.CreateDirectory(Path.Combine(projectPath, folder));
            }
        }

        private async Task GenerateProjectFileAsync(string projectPath, Project project)
        {
            var content = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
                          <PropertyGroup>
                            <TargetFramework>{project.TargetFramework}</TargetFramework>
                            <Nullable>enable</Nullable>
                            <ImplicitUsings>enable</ImplicitUsings>
                          </PropertyGroup>

                          <ItemGroup>
                            <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""8.0.0"" />
                            <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""8.0.0"" />
                            <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""8.0.0"" />
                            <PackageReference Include=""AutoMapper.Extensions.Microsoft.DependencyInjection"" Version=""12.0.1"" />
                            <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
                          </ItemGroup>
                        </Project>";

            await File.WriteAllTextAsync(
                Path.Combine(projectPath, $"{project.Name}.csproj"),
                content);
        }

        private async Task GenerateConfigurationFilesAsync(string projectPath, Project project)
        {
            await GenerateProgramFileAsync(projectPath, project);
            await GenerateAppsettingsAsync(projectPath, project);
            await GenerateLaunchSettingsAsync(projectPath, project);

            // Add EF Migration Commands
            var migrationCommand = $"dotnet ef migrations add InitialCreate --project {projectPath} --startup-project {projectPath}";
            var updateCommand = $"dotnet ef database update --project {projectPath} --startup-project {projectPath}";

            await ExecuteCommandAsync(migrationCommand, _basePath);
            await ExecuteCommandAsync(updateCommand, _basePath);
        }

        private async Task GenerateProgramFileAsync(string projectPath, Project project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("using Microsoft.OpenApi.Models;");
            sb.AppendLine($"using {project.Name}.Data;");
            sb.AppendLine($"using {project.Name}.Repositories;");
            sb.AppendLine($"using {project.Name}.Mappings;");
            sb.AppendLine();
            sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
            sb.AppendLine();
            sb.AppendLine("// Add services to the container");
            sb.AppendLine("builder.Services.AddControllers();");
            sb.AppendLine("builder.Services.AddEndpointsApiExplorer();");

            // Add Swagger
            if (project.Features.Contains("swagger"))
            {
                sb.AppendLine("builder.Services.AddSwaggerGen(c =>");
                sb.AppendLine("{");
                sb.AppendLine($"    c.SwaggerDoc(\"v1\", new OpenApiInfo {{ Title = \"{project.Name}\", Version = \"v1\" }});");
                sb.AppendLine("});");
            }

            // Add DbContext
            if (project.Features.Contains("entityframework"))
            {
                sb.AppendLine($"builder.Services.AddDbContext<{project.DbContext.Name}>(options =>");
                sb.AppendLine("    options.UseSqlServer(builder.Configuration.GetConnectionString(\"DefaultConnection\")));");
            }

            // Add Repository Services
            foreach (var repository in project.Repositories)
            {
                sb.AppendLine($"builder.Services.AddScoped<{repository.Name}, {repository.Name.Substring(1)}>();");
            }

            // Add AutoMapper
            if (project.Features.Contains("automapper"))
            {
                sb.AppendLine($"builder.Services.AddAutoMapper(typeof(AutoMapperProfile));");
            }

            // Add Logging
            if (project.Features.Contains("logging"))
            {
                sb.AppendLine("builder.Services.AddLogging(logging =>");
                sb.AppendLine("{");
                sb.AppendLine("    logging.AddConsole();");
                sb.AppendLine("    logging.AddDebug();");
                sb.AppendLine("});");
            }

            sb.AppendLine();
            sb.AppendLine("var app = builder.Build();");
            sb.AppendLine();
            sb.AppendLine("// Configure the HTTP request pipeline");
            sb.AppendLine("if (app.Environment.IsDevelopment())");
            sb.AppendLine("{");
            if (project.Features.Contains("swagger"))
            {
                sb.AppendLine("    app.UseSwagger();");
                sb.AppendLine("    app.UseSwaggerUI();");
            }
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("app.UseHttpsRedirection();");
            sb.AppendLine("app.UseAuthorization();");
            sb.AppendLine("app.MapControllers();");
            sb.AppendLine();
            sb.AppendLine("app.Run();");

            await File.WriteAllTextAsync(Path.Combine(projectPath, "Program.cs"), sb.ToString());
        }


        private async Task GenerateAppsettingsAsync(string projectPath, Project project)
        {
            var appsettings = new
            {
                Logging = new
                {
                    LogLevel = new
                    {
                        Default = "Information",
                        Microsoft = "Warning",
                        MicrosoftHostingLifetime = "Information"
                    }
                },
                AllowedHosts = "*",
                ConnectionStrings = new
                {
                    DefaultConnection = project.DbContext.ConnectionString
                }
            };

            var json = JsonSerializer.Serialize(appsettings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(
                Path.Combine(projectPath, "appsettings.json"),
                json);

            await File.WriteAllTextAsync(
                Path.Combine(projectPath, "appsettings.Development.json"),
                json);
        }

        private async Task GenerateLaunchSettingsAsync(string projectPath, Project project)
        {
            var settingsPath = Path.Combine(projectPath, "Properties");
            Directory.CreateDirectory(settingsPath);

            var launchSettings = new
            {
                profiles = new
                {
                    Development = new
                    {
                        commandName = "Project",
                        dotnetRunMessages = true,
                        launchBrowser = true,
                        applicationUrl = "https://localhost:50081;http://localhost:50082",
                        launchUrl = "swagger",
                        environmentVariables = new
                        {
                            ASPNETCORE_ENVIRONMENT = "Development"
                        }
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(launchSettings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(Path.Combine(settingsPath, "launchSettings.json"), json);
        }
        private async Task GenerateSolutionFileAsync(ProjectConfiguration config)
        {
            var solutionPath = Path.Combine(_basePath, $"{config.Solution.Name}.sln");

            // Use dotnet CLI to create the solution file and add projects to it
            var sb = new StringBuilder();

            sb.AppendLine($"Creating solution: {solutionPath}");
            sb.AppendLine();

            var command = $"dotnet new sln --name {config.Solution.Name}";
            await ExecuteCommandAsync(command, _basePath);

            foreach (var project in config.Solution.Projects)
            {
                var projectPath = Path.Combine(_basePath, project.Name, $"{project.Name}.csproj");
                command = $"dotnet sln add \"{projectPath}\"";
                await ExecuteCommandAsync(command, _basePath);
            }

            sb.AppendLine("Solution file generation completed.");
            Console.WriteLine(sb.ToString());
        }

        private async Task GenerateDbContextAsync(string projectPath, Project project)
        {
            var dbContextPath = Path.Combine(projectPath, "Data");
            Directory.CreateDirectory(dbContextPath);

            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine($"using {project.Name}.Models;");
            sb.AppendLine();
            sb.AppendLine($"namespace {project.Name}.Data");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {project.DbContext.Name} : DbContext");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {project.DbContext.Name}(DbContextOptions options) : base(options) {{ }}");
            sb.AppendLine();

            // Define DbSet properties for each model
            foreach (var model in project.Models)
            {
                sb.AppendLine($"        public DbSet<{model.Name}> {model.Name}s {{ get; set; }}");
            }

            sb.AppendLine();

            // Add OnModelCreating method with seed data
            sb.AppendLine("        protected override void OnModelCreating(ModelBuilder modelBuilder)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnModelCreating(modelBuilder);");

            // Process seed data dynamically based on model definitions
            if (project.SeedData != null && project.Models != null)
            {
                foreach (var seed in project.SeedData)
                {
                    var model = project.Models.FirstOrDefault(m => m.Name == seed.Entity);
                    if (model != null)
                    {
                        sb.AppendLine(GenerateSeedDataForEntity(seed, model));
                    }
                    else
                    {
                        sb.AppendLine($"            // Seed data for entity '{seed.Entity}' not found in models");
                    }
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            await File.WriteAllTextAsync(Path.Combine(dbContextPath, $"{project.DbContext.Name}.cs"), sb.ToString());
        }


        private string GenerateSeedDataForEntity(SeedData seed, Model model)
        {
            var sb = new StringBuilder();

            if (seed.Values != null && seed.Values.Count > 0)
            {
                sb.AppendLine($"            modelBuilder.Entity<{seed.Entity}>().HasData(");
                var seedItems = new List<string>();

                foreach (var value in seed.Values)
                {
                    var propertyAssignments = value
                        .Select(kv =>
                        {
                            var propertyType = model.Properties.FirstOrDefault(p => p.Name == kv.Key)?.Type;
                            return $"{kv.Key} = {FormatValue(kv.Key, kv.Value, propertyType)}";
                        })
                        .ToArray();

                    seedItems.Add($"                new {seed.Entity} {{ {string.Join(", ", propertyAssignments)} }}");
                }

                sb.AppendLine(string.Join(",\n", seedItems));
                sb.AppendLine("            );");
            }

            return sb.ToString();
        }

        private string FormatValue(string key, object value, string propertyType)
        {
            // Handle type conversions based on propertyType
            return propertyType switch
            {
                "Guid" when value is string s => $"Guid.Parse(\"{s}\")",
                "double" when double.TryParse(value.ToString(), out var d) => d.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
                "decimal" when decimal.TryParse(value.ToString(), out var m) => $"{m}m",
                "int" when int.TryParse(value.ToString(), out var i) => i.ToString(),
                "long" when long.TryParse(value.ToString(), out var l) => l.ToString(),
                "DateTime" when DateTime.TryParse(value.ToString(), out var dt) => $"DateTime.Parse(\"{dt:yyyy-MM-ddTHH:mm:ss}\")",
                "string" => $"\"{value}\"",
                _ => value?.ToString() ?? "null"
            };
        }

        private async Task ExecuteCommandAsync(string command, string workingDirectory)
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine(output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine($"Error: {error}");
            }
        }
    }
}
