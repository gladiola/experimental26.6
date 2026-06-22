namespace WebAppExperimental266.Interfaces.Main_Objects
{
    public interface IBlobSettingsService
    {
        string? BlobConnectionString { get; set; }
        int MaxAttachments { get; set; }
    }
}
