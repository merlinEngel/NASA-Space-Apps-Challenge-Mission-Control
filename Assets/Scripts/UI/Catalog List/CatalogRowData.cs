namespace MissionGame
{
    /// <summary>
    /// Everything one catalog row needs to draw itself.
    /// The list does not know cameras, batteries or rockets; the tab that owns
    /// the list converts its catalog entries into this struct.
    /// </summary>
    public struct CatalogRowData
    {
        /// <summary>Catalog id, e.g. "cubesat_cam_multispectral". Sent back on click.</summary>
        public string Id;

        /// <summary>Text key for the name, e.g. "part.cubesat_cam_multispectral.name".</summary>
        public string NameKey;

        /// <summary>Text key for the one-line description. Empty or null hides the line.</summary>
        public string DescriptionKey;

        /// <summary>Already formatted cell values, e.g. "1.2 kg", "9 W". At most as many as the row has value texts.</summary>
        public string[] Values;

        /// <summary>Row is part of the current design.</summary>
        public bool Selected;

        /// <summary>Row cannot be chosen right now (too heavy, wrong band, over budget).</summary>
        public bool Disabled;
    }
}
