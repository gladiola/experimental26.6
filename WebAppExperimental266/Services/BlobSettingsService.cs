using WebAppExperimental266.Interfaces.Main_Objects;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Service for managing Blob Storage settings
    /// </summary>
    public class BlobSettingsService : BlobSettings, IBlobSettingsService
    {
        /// <summary>
        /// Make a connection string to blob storage available to other classes.
        /// </summary>
        /// <param name="bs">BlobSettings object that holds a connection string.</param>
        public BlobSettingsService(BlobSettings bs)
        {
            BlobConnectionString = bs.BlobConnectionString;
            MaxAttachments = bs.MaxAttachments;
        }
    }
}
