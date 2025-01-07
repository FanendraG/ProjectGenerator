# ASP.NET Core API Project Generator

A powerful tool to dynamically generate fully functional ASP.NET Core API projects from YAML templates. Define entities, relationships, and configurations in YAML, and the generator will create the solution, projects, and files for you.

## Features
- Dynamic creation of:
  - Models
  - DTOs
  - Repositories
  - Controllers
  - DbContext with seed data
- Integration with:
  - Swagger for API documentation
  - AutoMapper for object mapping
  - EntityFramework Core for database interactions
- Customizable output paths for project creation
- Support for navigation properties and relationships

## Project Structure
The `ProjectGenerator` is organized as follows:

```bash
NZWalks/ 
├── ProjectGenerator/ 
 
│   ├── Program.cs 
│   ├── Models/ 
│   │ └── ProjectConfiguration.cs 
│   ├── Generators/ 
│   │ ├── ControllerGenerator.cs 
│   │ ├── DtoGenerator.cs 
│   │ ├── ModelGenerator.cs 
│   │ ├── RepositoryGenerator.cs 
│   │ └── ProjectStructureGenerator.cs 
│   └── config/ 
│        ├── nzwalks.yaml 
│        ├── library.yaml 
│        └── parking.yaml
```

## Usage Instructions

# Add required NuGet packages
```bash
dotnet add package YamlDotNet --version 13.7.1
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package AutoMapper --version 12.0.1
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
```

### Step 1: Install EF Core Tools
Ensure EF Core tools are installed globally:
```bash
dotnet tool install --global dotnet-ef
```

### Step 2: Run the Generator
Navigate to the ProjectGenerator directory and execute the generator with a YAML configuration file:
```bash
	cd ProjectGenerator
	dotnet run -- config/nzwalks.yaml
```

### Step 3: Open the Generated Solution
The solution will be created at the path specified by the outputPath property in the YAML file.
Example: For outputPath: "C:/Projects/NZWalks", the solution folder will be created at C:/Projects/NZWalks.

### Step 4: Run the Solution
Open the generated solution in your IDE (e.g., Visual Studio).
Build and run the solution to start the API.

### Available YAML Configurations
This project comes with three sample YAML configurations:

nzwalks.yaml: A walking trails API.
library.yaml: A library management API.
parking.yaml: A parking management API.

### Creating a New YAML Template
You can create a new YAML file to define a custom ASP.NET Core API project. Here’s what you need to know:

### Key Properties in YAML
outputPath
Specifies the directory where the generated solution will be created.
Update this property to your desired path:

    outputPath: "C:/Projects/MyNewAPI"

### createDto and updateDto
Ensure every DTO object has createDto and updateDto defined in the YAML.
Missing these properties will cause the project to fail to build.

### Navigational Properties
If a model has navigation properties, update the corresponding DTOs to include them.
Example:
```bash
yaml
- name: "ParkingSlotDto"
  model: "ParkingSlot"
  properties:
    - name: "Id"
      type: "Guid"
    - name: "SlotNumber"
      type: "int"
    - name: "IsOccupied"
      type: "bool"
    - name: "ParkingLotId"
      type: "Guid"
    - name: "ParkingLot"
      type: "ParkingLotDto" # Navigational Property
```
### Important Notes
 # outputPath Property:

Always set the outputPath to a valid directory path.
This is where the solution folder will be created.
# DTO Validations:

Missing createDto and updateDto properties will result in build errors.

# Navigational Properties in DTOs:

Ensure DTOs reflect the relationships in the models (e.g., parent-child relationships, collections).


# Contributing
Feel free to fork this repository and add enhancements or fixes. Submit a pull request with detailed descriptions of your changes.

# License
This project is licensed under the MIT License. See LICENSE for more details.