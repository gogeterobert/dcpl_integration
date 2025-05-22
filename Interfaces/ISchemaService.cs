using DCPLInterpreterV2.Infrastructure;
using DCPLInterpreterV2.Models;

namespace DCPLInterpreterV2.Interfaces;

public interface ISchemaService
{
    public void AddAndReplaceSchema(List<PowerFrame> schema);
    List<string> GetHolders();
    List<HolderAction> GetHolderActions();
    List<string> GetActions();
    Event GetActionConsequence(string action);
    List<string> ParseAllEntitiesFromSchema();
    string GenerateFromTemplate(string projectName);
    void CreateNewEntityInGeneratedSolution(string entityName, string projectName);
    void CreateGenericControllersAndCommands(List<HolderAction> entities, string projectName);
    void AddEfMigration(string projectName, string migrationName);
    void ApplyEfMigrations(string projectName);
    void RemoveDevelopmentIfElse(string projectName);
}
