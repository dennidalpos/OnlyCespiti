using System.Linq;
using GestioneCespiti.Managers;
using GestioneCespiti.Models;
using GestioneCespiti.Services;
using Xunit;

namespace GestioneCespiti.Tests;

public class SearchManagerTests
{
    [Fact]
    public void PerformSearch_IncludesArchivedSheetsWhenRequested()
    {
        using var workspace = new TestWorkspace();
        var persistence = new DataPersistenceService();

        var activeSheet = AssetSheet.CreateNew("Attivi");
        activeSheet.Rows.Add(new Asset());
        activeSheet.Rows[0]["Descrizione"] = "Server locale";
        persistence.SaveSheet(activeSheet);

        var archivedSheet = AssetSheet.CreateNew("Archiviati");
        archivedSheet.Rows.Add(new Asset());
        archivedSheet.Rows[0]["Descrizione"] = "Server storico";
        persistence.SaveSheet(archivedSheet);
        persistence.ArchiveSheet(archivedSheet);

        var manager = new SearchManager(persistence);
        var results = new System.Collections.Generic.List<SearchResult>();
        manager.NavigateRequested += (_, args) => results.Add(args.Result);

        manager.PerformSearch("Server", includeArchived: true, matchCase: false, showUserMessages: false);
        Assert.True(manager.NavigateNext());

        Assert.Equal(2, manager.TotalResults);
        Assert.Contains(results, result => result.Sheet.IsArchived);
        Assert.Contains(results, result => !result.Sheet.IsArchived);
    }
}
