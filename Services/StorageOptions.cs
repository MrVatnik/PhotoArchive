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

        /// <summary>Folder where generated thumbnails are cached.</summary>
        public string ThumbnailsRoot { get; set; } = "App_Data/thumbnails";

        /// <summary>Longest side of a generated thumbnail, in pixels.</summary>
        public int ThumbnailSize { get; set; } = 175;

        /// <summary>URL prefix under which originals are served.</summary>
        public const string OriginalsRequestPath = "/media/originals";

        /// <summary>URL prefix under which thumbnails are served.</summary>
        public const string ThumbnailsRequestPath = "/media/thumbs";
    }
}
