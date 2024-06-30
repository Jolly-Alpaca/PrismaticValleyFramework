namespace PrismaticValleyFramework.Models {
    /// <summary>
    /// Data structure for override color settings for entities that do not have a Custom Fields field (e.g. boots)
    /// </summary>
    public class ModColorData
    {
        public string Color;
        public string? Palette;
        public string? TextureTarget;
    }
}