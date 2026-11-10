using System.IO;
using GestioneCespiti.Models;
using GestioneCespiti.Services;
using Xunit;

namespace GestioneCespiti.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void MoveSettingsForSheet_RenamesSettingsFileAndPreservesValues()
    {
        using var workspace = new TestWorkspace();
        var service = new SettingsService();
        var settings = new AppSettings();
        settings.TipoAssetOptions.Add("Monitor");

        service.SaveSettingsForSheet(settings, "foglio-a.json");
        service.MoveSettingsForSheet("foglio-a.json", "foglio-b.json");

        var loaded = service.LoadSettingsForSheet("foglio-b.json", new AppSettings());

        Assert.Contains("Monitor", loaded.TipoAssetOptions);
        Assert.False(File.Exists(Path.Combine(workspace.DataDirectory, "config", "sheets", "foglio-a.settings.json")));
        Assert.True(File.Exists(Path.Combine(workspace.DataDirectory, "config", "sheets", "foglio-b.settings.json")));
    }

    [Fact]
    public void DeleteSettingsForSheet_RemovesSettingsAndBackupFiles()
    {
        using var workspace = new TestWorkspace();
        var service = new SettingsService();
        var settings = new AppSettings();

        service.SaveSettingsForSheet(settings, "foglio-c.json");
        service.SaveSettingsForSheet(settings, "foglio-c.json");
        service.DeleteSettingsForSheet("foglio-c.json");

        var settingsPath = Path.Combine(workspace.DataDirectory, "config", "sheets", "foglio-c.settings.json");
        Assert.False(File.Exists(settingsPath));
        Assert.False(File.Exists(settingsPath + ".bak"));
    }
}
