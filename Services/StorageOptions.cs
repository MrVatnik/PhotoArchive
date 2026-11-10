namespace PhotoArchive.Services
{
    /// <summary>
    /// Where photo files live on disk. Relative paths are resolved against the content root.
    /// Bound from the "Storage" configuration section.
    /// </summary>
    public class StorageOptions
    {
        public const string SectionName = "Storage";

        /// <summary>Folder that contains one sub-folder per film (Film.FolderName) with the original scans.</summary>
        public string OriginalsRoot { get; set; } = "App_Data/films";

        /// <summary>Folder where generated thumbnails and previews are cached.</summary>
        public string ThumbnailsRoot { get; set; } = "App_Data/thumbnails";

        /// <summary>Longest side of a gallery thumbnail, in pixels.</summary>
        public int ThumbnailSize { get; set; } = 175;

        /// <summary>Longest side of the web preview shown on the photo page, in pixels.</summary>
        public int PreviewSize { get; set; } = 2048;

        /// <summary>Generate missing thumbnails in the background when the app starts.</summary>
        public bool WarmUpThumbnailsOnStartup { get; set; } = true;

        /// <summary>URL prefix under which originals are served as static files.</summary>
        public const string OriginalsRequestPath = "/media/originals";
    }
}
