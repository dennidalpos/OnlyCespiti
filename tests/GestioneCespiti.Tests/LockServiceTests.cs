using System.IO;
using GestioneCespiti.Services;
using Xunit;

namespace GestioneCespiti.Tests;

public class LockServiceTests
{
    [Fact]
    public void TryAcquireLock_ReleaseLock_RemovesCurrentLockFile()
    {
        using var workspace = new TestWorkspace();
        var service = new LockService();

        var acquired = service.TryAcquireLock();
        var currentLock = service.GetCurrentLock();

        Assert.True(acquired);
        Assert.NotNull(currentLock);
        Assert.True(service.IsOwnLock());

        service.ReleaseLock();

        Assert.Null(service.GetCurrentLock());
        Assert.False(File.Exists(Path.Combine(workspace.DataDirectory, "config", "lock.json")));
    }
}
