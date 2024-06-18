using Microsoft.Xna.Framework;
using StardewValley;

namespace PrismaticValleyFramework.Framework
{
    static class ColorUtilities
    {
        /// <summary>
        /// Get a MonoGame color from a string representation
        /// </summary>
        /// <param name="colorString">The raw color value to parse. This can be Prismatic, 
        /// a Microsoft.Xna.Framework.Color property name (like SkyBlue), RGB or RGBA hex code 
        /// (like #AABBCC or #AABBCCDD), or 8-bit RGB or RGBA code (like 34 139 34 or 34 139 34 255).</param>
        /// <returns>The matching color. Default: Color.White</returns>
        public static Color getColorFromString(string colorString)
        {
            switch (colorString) {
                case "Prismatic": return Utility.GetPrismaticColor();
                // Call the Stardew Valley's existing StringToColor method to handle other input
                default: return Utility.StringToColor(colorString) ?? Color.White;
            }
        } 
        
        /// <summary>
        /// Applies a tint to a color by multiplying the tint color on the base color.
        /// </summary>
        /// <param name="baseColor">The base color to be tinted</param>
        /// <param name="tintColor">The tint color to apply to the base color</param>
        /// <returns>The tinted base color</returns>
        public static Color getTintedColor(Color baseColor, Color tintColor)
        {
            Color tintedColor = default(Color);
            // Equivalent to color.R/255 to get the float value, multiply the two floats together, then multiple by 255 to convert back to byte
            tintedColor.R = (byte)(baseColor.R * tintColor.R / 255f);
            tintedColor.G = (byte)(baseColor.G * tintColor.G / 255f);
            tintedColor.B = (byte)(baseColor.B * tintColor.B / 255f);
            tintedColor.A = (byte)(baseColor.A * tintColor.A / 255f);
            return tintedColor;
        }
    }
}