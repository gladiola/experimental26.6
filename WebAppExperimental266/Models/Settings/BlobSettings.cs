namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Class that describes settings used to connect to blob storage.
    /// </summary>
    public class BlobSettings 
    {
        /// <summary>
        /// Be able to pass in the connection string repeatedly to a controller.  
        /// </summary>
        public string? BlobConnectionString { get; set; }

        /// <summary>
        /// Be able to limit the number of items per blob container
        /// </summary>
        public int MaxAttachments { get; set; } = 10;
    }
}
