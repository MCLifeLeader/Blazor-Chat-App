# Copilot Instructions

These instructions define how GitHub Copilot should assist with this project. The goal is to ensure consistent, high-quality code generation aligned with our conventions, stack, and best practices.

## 🧠 Context

- **Project Type**: Web API / Blazor Web Assembly Application
- **Purpose**: This project is a web application that provides a RESTful API and a Blazor Web Assembly front-end application.
- **Language**: C#, JavaScript, Blazor
- **Data Persistence**: SQL Server / Cosmos DB
- **Development Environment**: Visual Studio / VS Code
- **Project Management Tool**: Azure DevOps
- **Testing Framework**: nUnit, NSubstitute
- **Version Control**: GitHub
- **Framework / Libraries**: .NET 8+ / ASP.NET Core / nUnit / NSubstitute
- **Architecture**: Clean Architecture / MVC / Onion, Services, Factory patterns, Repository patterns


## 🔑 Core Architectural Rules

**Data flow must always follow this path:**

```
SQL or Cosmos DB
    ↕ (Repository Layer)
Business Logic / Service Layer
    ↕ (API Controller Layer)
Blazor Front-End
```

- **No direct repository calls** from controllers or Blazor components.
- **All data access** must be through the Business / Service layer.
- **DTO (Data Transfer Objects)** must be used to encapsulate data sent between the client and server.
- Use extension method mappings for DTOs bidirectionally.
- The Service layer may act as a thin pass-through to repositories if no additional logic is needed — but the layer must still exist.
- All **external API calls** must be encapsulated in repository classes to ensure separation of concerns and testability.
- Controllers should **never** bypass the Service layer, even for read-only queries.
- This pattern must be followed for **all** CRUD, query, and integration operations.

## 🔧 General Guidelines

- Make only high confidence suggestions when reviewing code changes.
- Format using `dotnet format` or IDE auto-formatting tools.
- Prioritize readability, testability, and SOLID principles.
- Use `CONTRIBUTING.md` for contribution guidelines.
- C# class properties should be PascalCase. The JSON serializer will convert them to camelCase for the API, and the Blazor app will use camelCase as it receives the data from the API.
- Use requirements file found `docs/requirements.csv` for project context and requirements.
- Use the `docs/*` directory for project documentation.


## 📁 Folder Structure

Use this structure as a guide when creating or updating files:

```
/
  docs/
  containers/
  devops/
  src/
    Ai.Coach/
      Ai.Coach.App/
        Ai.Coach.App.ApiService/
        Ai.Coach.App.ApiService.Tests/
        Ai.Coach.App.AppHost/
        Ai.Coach.App.Home/
        Ai.Coach.App.Home.Tests/
        Ai.Coach.App.ServiceDefaults/
        Ai.Coach.App.Web/
        Ai.Coach.App.Web.Client/
        Ai.Coach.App.Web.Tests/
      Ai.Coach.Business/
        DependencyInjection/
        Services/
      Ai.Coach.Business.Tests/
      Ai.Coach.Common/
        Connection/
        Constants/
        Helpers/
          Data/
          Exceptions/
          Extensions/
          Filter/
        Models/
        Repositories/
      Ai.Coach.Common.Tests/
      Ai.Coach.Data/
        Helpers/
        Models/
        Repositories/
      Ai.Coach.Data.Tests/
      Ai.Coach.Database/
      Ai.Coach.Locale/
```

## HTML, CSS, and JavaScript Guidelines

### Front-End Guidelines (Blazor)

Blazor development in this project should follow **component-based architecture principles** similar to **React**.
Pages should be composed of smaller, reusable components, and components can nest other components to form complete pages.

**Key Principles:**
- **Strict Separation of Concerns**:
  - The front-end and back-end code must remain independent.
  - No business logic or direct data access should exist in `.razor` or client-side C# files.
  - All server communication must be routed through the API layer.
- **Componentization**:
  - Break down pages into small, reusable components.
  - Pages should primarily act as containers that assemble smaller UI components.
  - Each component should handle a single, well-defined responsibility.
- **Folder Structure**:
  - Components, Pages, and Layouts should be organized under the `Components` folder.
  - Use a `Layout` subfolder for shared layouts and navigation components.
- **API Calls**:
  - All API calls should be made through dedicated service classes — never directly from a component.
  - Use `HttpClient` for API requests, configured in the `RegisterDependentServices.cs` file.
- **Configuration**:
  - Use `IOptions<T>` for all configuration settings.
- **File Separation (Mandatory)**:
  - HTML markup → `.razor` files
  - C# logic → `.razor.cs` files
  - CSS styling → `.razor.css` files
  - JavaScript interop → `.js` files
- **Testing & Debugging**:
  - Whenever possible, add `data-testid` attributes to HTML elements to facilitate automated testing and debugging.
- **Localization**:
  - Use `.resx` resource files in the `Ai.Coach.Locale` project for all translatable UI strings.

Following these guidelines ensures the Blazor application remains modular, testable, and maintainable — enabling easier scaling and consistent development practices.

## 🧶 C# .NET Patterns

### ✅ Patterns to Follow
- Use `.editorconfig` for consistent code style. (If present, see root directory.)
- Always use the latest version C# and .NET.
- Use Clean Architecture with layered separation.
- Use Dependency Injection for services and repositories.
- Map DTOs to domain models using manual mapping extension classes and methods.
- Use C#-idiomatic patterns and follow .NET coding conventions.
- Use named methods instead of anonymous lambdas in business logic.
- Use constructor injection for dependencies.
- Register dependencies in the respective dependency injection file for Services, Repositories, and Factories.
- Avoid magic strings. Use constants as can be found in the `Constants` folder.
- A file can only contain one class declaration.
- Use `IOptions<T>` for configuration settings.
- Prefer using Task async/await whenever possible.
- Use `ILogger<T>` for structured logging.
  - Add logging in the constructor of classes and use it to log important events.

### Back-end Guidelines
- Use ASP.NET Core for the API (see `Ai.Coach.App.ApiService/`).
- Use [ApiController], ActionResult<T>, and ProducesResponseType.
- Handle errors using middleware and Problem Details.
- Controllers should use attributes for parameter binding and validation to indicate from route, from body, from query, required, positiveInt, etc.
- Controllers should inherit from BaseApiController, which provides common functionality for all controllers.
- Inject the controller dependency bundle into the constructor to then pass to the base constructor.
- Controllers should be kept simple, typically calling a single method on the service layer and returning the result.

### Services
- Services should be focused on a single conceptual need and should not be too large.
- The general pattern for a loading service call is to load data via one or more repositories, then use a factory to convert the results into a model to return.
- The general pattern for a saving service call is to use the factory to convert the submitted model into a repository model, then call the repository to save the data.
- Services can contain business logic, though the logic is usually determining what repository to call and what factory to use.

### Data and Repositories
- Always use the repository pattern to abstract data access for all data sources, including:
  - HttpClient REST calls: Encapsulate all external API interactions within repository classes to ensure separation of concerns and testability.
  - SQL Data: Use repositories to manage all database queries and commands, keeping data access logic isolated from business logic.
  - CosmosDb data calls: Implement repository classes to handle all interactions with CosmosDb, maintaining a consistent abstraction layer for data access.
- Repositories should center around outputs that deal with a single entity type.
- Repositories are very simple. For REST, they make an HTTP Client request using the http client wrapper on the expected client name and return the result. For SQL or CosmosDb, they encapsulate the query and data mapping logic.
- Repositories to load data that rarely change (countries, permissions, languages etc.) should have a companion cache that implements IAbstractedCache and is registered in the DI container.

### Factories
- Methods to convert a data model to a UI model used for rendering should be called ToUi or something similar.
- Methods that convert a UI model to a data model should be called ToSubmit or something similar.
- Factories often use business logic in assembling a model based on data passed in, so they can be larger than a repository or service.
- Factories can load data sparingly. It is appropriate to convert an id to an object (e.g., countryId to a country object). Factory load calls should favor repositories that use caching whenever possible.

### 🚫 Patterns to Avoid
- Don’t use static state or service locators.
- Avoid logic in controllers—delegate to services/handlers.
- Don’t hardcode config—use appsettings.json and IOptions.
- Don’t expose entities directly in API responses.
- Avoid fat controllers and God classes.

### Nullable Reference Types

- Declare variables non-nullable, and check for `null` at entry points.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### 🧪 .NET Testing Guidelines
- Use the AAA pattern (Arrange, Act, Assert).
  - When creating tests add the single comment for each section:
    ```csharp
    // Arrange
    // Act
    // Assert
    ```
- Use nUnit for unit testing.
- Use NSubstitute for mocking.
- Categorize tests with the TestFixture attribute on the test class definition.
- Avoid infrastructure dependencies.
- Name tests clearly.
- Write the simplest test that fully validates the intended behavior, avoiding unnecessary assertions or setup.
- Avoid logic in tests.
- Prefer helper methods for setup and teardown.
- Avoid multiple acts in a single test.
- When a class has public virtual methods, use the partial mock pattern to test the class. This allows you to test the class without needing to mock the entire class.
- Make sure to declare DoNotCallBase when testing methods that use the virtual methods, and mock a return value for the virtual method.
- Verify that the virtual method was called with the correct parameters just like an injected dependency.
- Prefer TDD for critical business logic and application services.

## 🧩 Example Prompts
- `Copilot, generate an ASP.NET Core controller with CRUD endpoints for Product.`
- `Copilot, create an Entity Framework Core DbContext for a blog application.`
- `Copilot, write an nUnit test for the CalculateInvoiceTotal method.`
- `Copilot, generate a Blazor component for coach profile display.`

## Running tests

(1) Build from the root with `dotnet build Ai.Coach.sln`.
(2) If that produces errors, fix those errors and build again. Repeat until the build is successful.
(3) Run tests with `dotnet test **/*.tests.csproj`.
(4) Tests are run with `dotnet test **/*tests.csproj`.
(5) If any tests fail, fix those tests and re-run the tests until they all pass.

## 🔁 Iteration & Review
- Copilot output should be reviewed and modified before committing.
- If code isn’t following these instructions, regenerate with more context or split the task.
- Use /// XML documentation comments to clarify intent for Copilot and future devs.
- Use Rider or Visual Studio code inspections to catch violations early.

## 📚 References
- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-8.0)
- [Entity Framework Core Docs](https://learn.microsoft.com/en-us/ef/core/)
- [nUnit Documentation](https://nunit.org/)
- [Clean Architecture in .NET (by Jason Taylor)](https://github.com/jasontaylordev/CleanArchitecture)