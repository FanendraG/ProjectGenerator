using System.Collections.Generic;

namespace ProjectGenerator.Models
{
    public class ProjectConfiguration
    {
        public string Version { get; set; }
        public Solution Solution { get; set; }
    }

    public class Solution
    {
        public string Name { get; set; }
        public string OutputPath { get; set; }
        public List<Project> Projects { get; set; }
    }

    public class Project
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string TargetFramework { get; set; }
        public List<string> Features { get; set; }
        public List<Model> Models { get; set; }
        public List<Dto> Dtos { get; set; }
        public DbContext DbContext { get; set; }
        public List<Controller> Controllers { get; set; }
        public List<Repository> Repositories { get; set; }
        public List<SeedData> SeedData { get; set; }
    }

    public class Model
    {
        public string Name { get; set; }
        public List<Property> Properties { get; set; }
    }

    public class Property
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsFK { get; set; }
        public string References { get; set; }
        public int? MaxLength { get; set; }
        public bool Required { get; set; }
        public bool Nullable { get; set; }
    }

    public class Dto
    {
        public string Name { get; set; }
        public string Model { get; set; }
        public List<Property> Properties { get; set; }
    }

    public class Repository
    {
        public string Name { get; set; }
        public string Model { get; set; }
        public List<string> Operations { get; set; }
    }

    public class Controller
    {
        public string Name { get; set; }
        public string Model { get; set; }
        public string Dto { get; set; }
        public string CreateDto { get; set; }
        public string UpdateDto { get; set; }
        public List<string> Operations { get; set; }
    }

    public class DbContext
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
    }

    public class SeedData
    {
        public string Entity { get; set; }  // Name of the entity
        public List<Dictionary<string, object>> Values { get; set; }  // List of key-value pairs for properties
    }
}
