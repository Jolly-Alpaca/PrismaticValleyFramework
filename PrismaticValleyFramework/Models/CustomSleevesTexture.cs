using Microsoft.Xna.Framework.Graphics;

namespace PrismaticValleyFramework.Models {
    /// <summary>
    /// Data structure for custom sleeve textures
    /// </summary>
    public class CustomSleevesTextures
    {
        public Texture2D SleevesTexture; // Texture to apply to Farmer
        public WeakReference<Texture2D> BaseSleevesTexture; // Base sleeve texture SleevesTexture is created from
    }
}