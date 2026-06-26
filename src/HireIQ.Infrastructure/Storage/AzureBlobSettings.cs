namespace HireIQ.Infrastructure.Storage;

public sealed class AzureBlobSettings
{
    public const string SectionName = "AzureBlob";
    public string ConnectionString { get; set; } = string.Empty;
    public string DefaultContainer { get; set; } = "hireiq";
    public bool CreateContainerIfMissing { get; set; } = true;
}
