# Project Setup and Configuration

## About this Project

This project is a sample Blazor Chat Application demonstrating the use of Blazor WebAssembly and ASP.NET Core Web API. The application includes features such as real-time messaging, user authentication, and a responsive UI.

## Project Dependencies

### Required

- .NET
- Visual Studio
- SQL Server Instance
- CosmoDB

### Recommended

- Docker Desktop
- Visual Studio Code
- SQL Server Management Studio

## Getting Started

### Setting up the project

1. Clone the repository to your local machine.
2. Locate the file docker_setup.ps1 in the root of the project and run this in PowerShell which will setup the Docker Containers and Dependencies.

### Using SQL Server Instance

1. To get started with this project, you will need to clone the repository and then open the solution in Visual Studio.
2. Once the solution is open, you will want to build the solution. This will download all of the NuGet packages that are required or the project.
3. Open the Database.Example project and build the project then publish using the "StartupExample.publish.xml" profile.
4. There should be no need to update the secrets.json file with the connection string as it should be configured in the appsettings.json file for SQL Server Instance.

### Using Docker Containers

We are using Docker Services in this project as a "Developer Toolbox" to provide a cafeteria style approach to meeting development dependency needs primarily for local development.

This project includes example `docker-compose*.yml` files in the `containers/` directory. These are provided primarily as reference implementations to help teams get started with local development and integration scenarios. The included containers cover common services, but your team should review and reduce the number of containers to match your specific requirements and workflow. Customizing the setup will help streamline development and resource usage.

Devcontainer support is also available, enabling you to use Visual Studio Code's remote container features for a consistent development environment. This makes onboarding easier and ensures dependencies are managed in a reproducible way. See the devcontainer configuration for details on how to launch the project in a containerized VS Code environment.

1. Start Visual Studio and open the solution.
2. Open the Database.Example project and build the project then publish using the "StartupExample.Docker.publish.xml" profile. Using the default password of "P@ssword123!".
3. MailHog was added as an email trap.
4. Open Telemetry was added to the project to help with debugging and development and can be found [here](http://localhost:4341/) after starting the docker containers.
   1. Watch the following video for why and how its used: [The Logging Everyone Should Be Using in .NET](https://www.youtube.com/watch?v=MHJ0BHfWhRw)

### Running the Applications

1. Once the project has been built and the database has been created, you can run any of the applications.

## CodeSpaces

If you are using CodeSpaces you'll want to update your container git configuration profile. Be sure to update with your appropriate name and email details.

### GitHub Configuration

```
git config --global user.name "Your Name"
git config --global user.email "youremail@yourdomain.com"
```

## Installing Resources

This document outlines the resources generally needed to do development on this project. Some resources are not critical but recommended.

### DotNet SDKs

✅ Required Resource ✅

This includes .NET 8 and .NET 9.

#### .NET 8 SDK

```
winget install Microsoft.DotNet.SDK.8
```

#### .NET 9 SDK

```
winget install Microsoft.DotNet.SDK.9
```

### Windows Terminal Console Support

👍 Recommended 👍

This utility provides windows a Unix like tabbed console window manager.

```
winget install Microsoft.WindowsTerminal
```

### Latest version of PowerShell

✅ Required Resource ✅

DevOps customized commands are written using the latest version of PowerShell.

```
winget install Microsoft.PowerShell
```

### Azure toolkit

👍 Recommended 👍

These tools provide CLI support for interacting with and managing Azure resources from your console. Some of these tools may be required depending on some of the development work required such as FunctionsCoreTools if doing function app development.

#### Azure CLI

```
winget install Microsoft.AzureCLI
winget install Microsoft.Azd
```

#### Azure Functions Core Tools

```
winget install Microsoft.Azure.FunctionsCoreTools
```

### Azure Storage Emulator and Azure Storage Explorer

If you need to work with Azure Blob Storage this Visual GUI tool provides easy access and management of content stored in an Storage resource in Azure.

```
winget install Microsoft.Azure.StorageEmulator
winget install Microsoft.Azure.StorageExplorer
```

### Git Client and Visual Git Clients

✅ Required Resource ✅

This adds the windows GIT client to interact with git based repositories.

```
winget install Git.Git
```

👍 Recommended 👍

These are two recommended GUI wrapper clients for the git Cli. These are not required but maybe helpful in managing repository changes and visualizing history and branching better. Atlassian's tool works very well at providing a history tree view for tracking changes. Both Visual Studio and VS Code have git support making these tools optional, additionally the integration with GitHub Copilot is very helpful when making code commits through Visual Studio or VS Code.

#### Atlassian Source Tree

```
winget install Atlassian.Sourcetree
```

#### GitHub Desktop

```
winget install GitHub.GitHubDesktop
```

### Primarily used for Git Merge Conflicts, compares files and folders

👍 Recommended 👍 <br />
⭐ License Required ⭐

When performing merging of branches, the BeyondCompare tool has been fantastic. This tool also supports comparisons between folders making it easier to perform diffs between two large directory structures. This tool also integrates well with Atlassian Source Tree.

```
winget install ScooterSoftware.BeyondCompare.5
```

### Postman Gui Http Client

👍 Recommended 👍

```
winget install Postman.Postman
```

### Bruno Gui Http Client

👍 Recommended 👍

```
winget install Bruno.Bruno
```

### Local Database Engine, SQL Server Express

👍 Recommended 👍

Having a local instance of SQL Server can be helpful for development purposes. However, if you are using Docker there is a built in container image for SQL Server that can be used instead. This is recommended as it provides a more consistent environment across developers and does not require installation of a local database engine. However, if needed, it can be installed locally.

⚠️ No production data from our servers maybe copied into your local database, Scrubbed data only ⚠️

```
winget install Microsoft.SQLServer.2022.Express
```

### IDE, SQL Server Management Studio and General Text Editor

✅ Required Resource ✅ <br />
⭐ License Required ⭐

Our primary development uses Visual Studio and / or Visual Studio Code
There are three options for Visual Studio, Enterprise, Professional, and Community. The Community edition is free and sufficient for most development needs. The Professional and Enterprise editions require a license. Check with your Engineering Manager to see if you need a license for the Professional or Enterprise editions.

#### Visual Studio Enterprise

```
winget install Microsoft.VisualStudio.2022.Enterprise
```

#### Visual Studio Professional

```
winget install Microsoft.VisualStudio.2022.Professional
```

#### Visual Studio Community

```
winget install Microsoft.VisualStudio.2022.Community
```

#### Visual Studio Code

```
winget install Microsoft.VisualStudioCode
```


👍 Recommended 👍

Having the ability to query the database is required, however, SQL Server Management Studio is recommended as the primary tool for doing so.

#### SQL Server Management Studio

```
winget install Microsoft.SQLServerManagementStudio
```

### Docker / Container Services

👍 Recommended 👍

This resource provides local development tools and resources will speed up developer setup and configuration and provide diagnostic and debugging resources not previously available. The number of options and tools is near limitless. This project uses Docker Desktop to provide a local development environment for .NET and React development. It includes support for DevContainers, RedisCache, OpenTelemetry durability, MailHog mail trap, Azurite, and more.

```
winget install Docker.DockerDesktop
```

### Update Dotnet workloads

👍 Recommended 👍

It is recommended to periodically run the dotnet workload update command, as this will help keep your local development tools and resources up to date. Running Visual Studio updates do not always update all of the dotnet resources.

If you desire experimenting with Aspire resources it is recommended to install the wasm-tools and aspire workloads.
Lastly, the LibraryManager provides resources for using libman.json configuration files to automatically download web resources such as jQuery, Bootstrap, and other libraries for web development.

⚠️ .NET Aspire ⚠️

```
dotnet workload update
dotnet workload install wasm-tools
dotnet workload install aspire
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
```

## Getting the project setup

### Configure unsigned PowerShell scripts to run locally

This will update PowerShell permissions settings to grant unsigned scripts access to run locally.

Run as local machine administrator

```
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser
```

### DevContainers and Local Development with Containers

👍 Recommended 👍

A DevContainer is a development environment defined by a Docker container, which includes all necessary tools, libraries, and dependencies for a project. It ensures a consistent development setup across different machines, simplifies onboarding, and enhances productivity by providing a pre-configured environment that mirrors production settings. DevContainers are commonly used with Visual Studio Code to streamline development workflows and improve collaboration.

[DevContainer Documentation](https://code.visualstudio.com/docs/devcontainers/containers)

To run DevContainers on a Windows machine for .NET and React development, the following dependencies must be met:

- Docker Desktop: Install Docker Desktop to enable containerization.
- Visual Studio Code: Install VS Code as the primary IDE.
- Install the VS Code extension 'ms-vscode-remote.remote-containers'
- .NET SDK: Ensure the .NET SDK is installed for .NET development.
