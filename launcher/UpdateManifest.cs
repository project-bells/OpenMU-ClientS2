namespace Origins.Launcher;

internal sealed class UpdateManifest
{
    public string BaseUrl { get; set; } = string.Empty;
    public List<UpdateFile> Files { get; set; } = [];
}

internal sealed class UpdateFile
{
    public string Path { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
}
