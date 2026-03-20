using System;
using System.IO;

namespace GestioneCespiti.Tests;

internal sealed class TestWorkspace : IDisposable
{
    private readonly string _baseDirectory;

    public TestWorkspace()
    {
        _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        Directory.SetCurrentDirectory(_baseDirectory);
        ResetLocalState();
    }

    public string DataDirectory => Path.Combine(_baseDirectory, "data");

    public void Dispose()
    {
        ResetLocalState();
    }

    private void ResetLocalState()
    {
        if (Directory.Exists(DataDirectory))
        {
            Directory.Delete(DataDirectory, true);
        }
    }
}
