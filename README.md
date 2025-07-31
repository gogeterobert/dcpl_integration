#  DCPLInterpreterV2

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Clean architecture .NET template installed](https://github.com/jasontaylordev/CleanArchitecture?tab=readme-ov-file#getting-started)

## Getting Started

1. **Clone the repository:**

    ```sh
    git clone https://github.com/gogeterobert/dcpl_integration
    cd DCPLInterpreterV2
    ```

2. **Open the project in Visual Studio Code:**

    ```sh
    code .
    ```

3. **Restore dependencies:**

    ```sh
    dotnet restore
    ```

4. **Build the solution:**

    ```sh
    dotnet build
    ```

5. **Run the application with the `http` profile:**

    ```sh
    dotnet run --launch-profile http
    ```

6. **Open the browser:**

    Open your browser and navigate to `http://localhost:5182/swagger` to access the Swagger UI.


## Uploading and using a schema

The application supports power frames that can be validated using the [DCPL json schema](https://github.com/gsileno/DCPLschema).

In order to generate code based on a schema for the computational operationalization:

1. **Paste the example in the /Schema/CreateAndReplace:**

The schema is stored locally in a SQLite database. 

2. **Generate by hitting /Schema/generate:**

This will create a new controller that will be visible after restarting the application. The controller will then show up in swagger containing actions for instantiating new entities and making actions listed in the schema.
