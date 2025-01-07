using System.Text;
using System.Threading.Tasks;
using ProjectGenerator.Models;

namespace ProjectGenerator.Generators
{
    public class ControllerGenerator
    {
        public async Task GenerateControllersAsync(string projectPath, Project project)
        {
            var controllerPath = Path.Combine(projectPath, "Controllers");
            Directory.CreateDirectory(controllerPath);

            foreach (var controller in project.Controllers)
            {
                await GenerateControllerFileAsync(controllerPath, controller, project);
            }
        }

        private async Task GenerateControllerFileAsync(string path, Controller controller, Project project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine($"using {project.Name}.Repositories;");
            sb.AppendLine($"using {project.Name}.Models;");
            sb.AppendLine($"using {project.Name}.Models.DTOs;");
            sb.AppendLine($"using AutoMapper;");
            sb.AppendLine();
            sb.AppendLine($"namespace {project.Name}.Controllers");
            sb.AppendLine("{");
            sb.AppendLine($"    [ApiController]");
            sb.AppendLine($"    [Route(\"api/[controller]\")]");
            sb.AppendLine($"    public class {controller.Name} : ControllerBase");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly I{controller.Model}Repository _repository;");
            sb.AppendLine("        private readonly IMapper _mapper;");
            sb.AppendLine();
            sb.AppendLine($"        public {controller.Name}(I{controller.Model}Repository repository, IMapper mapper)");
            sb.AppendLine("        {");
            sb.AppendLine("            _repository = repository;");
            sb.AppendLine("            _mapper = mapper;");
            sb.AppendLine("        }");
            sb.AppendLine();

            foreach (var operation in controller.Operations)
            {
                GenerateControllerOperation(sb, operation, controller);
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            await File.WriteAllTextAsync(Path.Combine(path, $"{controller.Name}.cs"), sb.ToString());
        }

        private void GenerateControllerOperation(StringBuilder sb, string operation, Controller controller)
        {
            switch (operation.ToLower())
            {
                case "getall":
                    sb.AppendLine($"        [HttpGet]");
                    sb.AppendLine($"        public async Task<IActionResult> GetAll()");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var entities = await _repository.GetAllAsync();");
                    sb.AppendLine($"            return Ok(_mapper.Map<IEnumerable<{controller.Dto}>>(entities));");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                    break;

                case "getbyid":
                    sb.AppendLine($"        [HttpGet(\"{{id:guid}}\")]");
                    sb.AppendLine($"        public async Task<IActionResult> GetById(Guid id)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var entity = await _repository.GetByIdAsync(id);");
                    sb.AppendLine("            if (entity == null)");
                    sb.AppendLine("                return NotFound();");
                    sb.AppendLine();
                    sb.AppendLine($"            return Ok(_mapper.Map<{controller.Dto}>(entity));");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                    break;

                case "create":
                    sb.AppendLine($"        [HttpPost]");
                    sb.AppendLine($"        public async Task<IActionResult> Create([FromBody] {controller.CreateDto} dto)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            if (!ModelState.IsValid)");
                    sb.AppendLine("                return BadRequest(ModelState);");
                    sb.AppendLine();
                    sb.AppendLine($"            var entity = _mapper.Map<{controller.Model}>(dto);");
                    sb.AppendLine($"            entity = await _repository.CreateAsync(entity);");
                    sb.AppendLine($"            return CreatedAtAction(nameof(GetById), new {{ id = entity.Id }}, _mapper.Map<{controller.Dto}>(entity));");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                    break;

                case "update":
                    sb.AppendLine($"        [HttpPut(\"{{id:guid}}\")]");
                    sb.AppendLine($"        public async Task<IActionResult> Update(Guid id, [FromBody] {controller.UpdateDto} dto)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            if (!ModelState.IsValid)");
                    sb.AppendLine("                return BadRequest(ModelState);");
                    sb.AppendLine();
                    sb.AppendLine($"            var entity = _mapper.Map<{controller.Model}>(dto);");
                    sb.AppendLine($"            entity = await _repository.UpdateAsync(id, entity);");
                    sb.AppendLine("            if (entity == null)");
                    sb.AppendLine("                return NotFound();");
                    sb.AppendLine();
                    sb.AppendLine($"            return Ok(_mapper.Map<{controller.Dto}>(entity));");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                    break;

                case "delete":
                    sb.AppendLine($"        [HttpDelete(\"{{id:guid}}\")]");
                    sb.AppendLine($"        public async Task<IActionResult> Delete(Guid id)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            var success = await _repository.DeleteAsync(id);");
                    sb.AppendLine("            if (!success)");
                    sb.AppendLine("                return NotFound();");
                    sb.AppendLine();
                    sb.AppendLine("            return NoContent();");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                    break;

                default:
                    sb.AppendLine($"        // Operation '{operation}' not supported.");
                    sb.AppendLine();
                    break;
            }
        }
    }
}
