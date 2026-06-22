namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Holds the ArcGIS map URLs and display labels for each audience tier.
    /// Configure these values in the ArcGisSettings section of appsettings.json.
    /// </summary>
    public class ArcGisSettings
    {
        /// <summary>URL shown to unauthenticated visitors.</summary>
        public string PublicMapUrl { get; set; } = "https://harvard-cga.maps.arcgis.com/apps/mapviewer/index.html";

        /// <summary>Display label used when rendering the public map.</summary>
        public string PublicMapLabel { get; set; } = "Public View";

        /// <summary>URL shown to authenticated (non-admin) users.</summary>
        public string UserMapUrl { get; set; } = "https://harvard-cga.maps.arcgis.com/apps/mapviewer/index.html";

        /// <summary>Display label used when rendering the user map.</summary>
        public string UserMapLabel { get; set; } = "User View";

        /// <summary>URL shown to authenticated administrators.</summary>
        public string AdminMapUrl { get; set; } = "https://harvard-cga.maps.arcgis.com/apps/mapviewer/index.html";

        /// <summary>Display label used when rendering the admin map.</summary>
        public string AdminMapLabel { get; set; } = "Admin View";
    }
}
