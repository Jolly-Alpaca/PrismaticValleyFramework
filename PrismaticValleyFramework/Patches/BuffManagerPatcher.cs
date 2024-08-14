using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using PrismaticValleyFramework.Framework;
using StardewValley;
using StardewValley.Buffs;

namespace PrismaticValleyFramework.Patches
{
    internal class BuffManagerPatcher
    {
        private static IMonitor Monitor;

        /// <summary>
        /// Apply the harmony patches to Buff.cs
        /// </summary>
        /// <param name="monitor">The Monitor instance for the PrismaticValleyFramework module</param>
        /// <param name="harmony">The Harmony instance for the PrismaticvalleyFramework module</param>
        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                original: AccessTools.Method(typeof(BuffManager), nameof(BuffManager.Remove)),
                postfix: new(typeof(BuffManagerPatcher), nameof(BuffManager_Remove_Postfix))
            );
        }

        /// <summary>
        /// Flag the Farmer's sprite in FarmerRenderer to be refreshed if the custom color buff is removed
        /// from the Farmer to switch back to the Farmer's original skin color
        /// </summary>
        /// <param name="__instance">The BuffManager instance</param>
        /// <param name="id">The id of the buff being removed from the Farmer's BuffManager</param> 
        private static void BuffManager_Remove_Postfix(BuffManager __instance, string id)
        {
            if (id == $"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff")
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                // Get the Farmer from the BuffManager (protected)
                if (typeof(BuffManager).GetField("Player", flags)!.GetValue(__instance) is Farmer Player)
                {
                    // Flag the FarmerRenderer to redraw the farmer sprite to reload the players original skinColor texture
                    // Setting same variables as skin field change delegate
                    // The method that recolors the base farmer sprite is called every tick in the draw method so does not need to be called here
                    typeof(FarmerRenderer).GetField("_spriteDirty", flags)!.SetValue(Player.FarmerRenderer, true);
                    typeof(FarmerRenderer).GetField("_skinDirty", flags)!.SetValue(Player.FarmerRenderer, true);
                    typeof(FarmerRenderer).GetField("_shirtDirty", flags)!.SetValue(Player.FarmerRenderer, true);
                }
            }
        }
    }
}