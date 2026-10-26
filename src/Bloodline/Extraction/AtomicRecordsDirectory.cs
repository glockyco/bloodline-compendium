namespace Bloodline.Extraction;

internal sealed class AtomicRecordsDirectory : IDisposable
{
    private readonly string targetDirectory;
    private readonly string backupDirectory;
    private bool committed;
    private bool disposed;

    public AtomicRecordsDirectory(string targetDirectory)
    {
        this.targetDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetDirectory));
        var parent =
            Path.GetDirectoryName(this.targetDirectory)
            ?? throw new ArgumentException(
                "Records directory must have a parent.",
                nameof(targetDirectory)
            );
        Directory.CreateDirectory(parent);
        var name = Path.GetFileName(this.targetDirectory);
        DirectoryPath = Path.Combine(parent, $".{name}.pending-{Guid.NewGuid():N}");
        backupDirectory = Path.Combine(parent, $".{name}.previous-{Guid.NewGuid():N}");
        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath { get; }

    public void Commit()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (committed)
        {
            throw new InvalidOperationException("Records directory has already been committed.");
        }
        var previousExists = Directory.Exists(targetDirectory);
        if (previousExists)
        {
            Directory.Move(targetDirectory, backupDirectory);
        }
        try
        {
            Directory.Move(DirectoryPath, targetDirectory);
        }
        catch
        {
            if (previousExists)
            {
                Directory.Move(backupDirectory, targetDirectory);
            }
            throw;
        }
        committed = true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
        if (committed && Directory.Exists(backupDirectory))
        {
            Directory.Delete(backupDirectory, recursive: true);
        }
        disposed = true;
    }
}
