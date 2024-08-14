using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using PrismaticValleyFramework.Framework;
using StardewValley;

namespace PrismaticValleyFramework.Patches
{
    internal class FarmerPatcher
    {
        private static IMonitor Monitor;

        /// <summary>
        /// Apply the harmony patches to Farmer.cs
        /// </summary>
        /// <param name="monitor">The Monitor instance for the PrismaticValleyFramework module</param>
        /// <param name="harmony">The Harmony instance for the PrismaticvalleyFramework module</param>
        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.doneEating)),
                postfix: new(typeof(FarmerPatcher), nameof(Farmer_DoneEating_Postfix))
            );
        }

        
        private static void Farmer_DoneEating_Postfix(Farmer __instance)
        {  
            if (__instance.itemToEat is null || !DataLoader.Objects(Game1.content).TryGetValue(__instance.itemToEat.ItemId, out var objectData))
                return;

            string colorBuffName = $"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff";
            if (objectData.Buffs?.FirstOrDefault(x => x.BuffId == colorBuffName) is { } buff && buff.CustomFields != null)
            {
                if (buff.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Color", out string? colorString))
                {
                    buff.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Palette", out string? paletteString);
                    ModEntry.ModCustomColorData[string.Concat(colorBuffName, __instance.UniqueMultiplayerID)] =
                        new Models.ModColorData
                        {
                            Color = colorString,
                            Palette = paletteString
                        };
                    // Flag the FarmerRenderer to redraw the farmer sprite to load the custom skinColor texture
                    // Setting same variables as skin field change delegate
                    // The method that recolors the base farmer sprite is called every tick in the draw method so does not need to be called here
                    BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    typeof(FarmerRenderer).GetField("_spriteDirty", flags)!.SetValue(__instance.FarmerRenderer, true);
                    typeof(FarmerRenderer).GetField("_skinDirty", flags)!.SetValue(__instance.FarmerRenderer, true);
                    typeof(FarmerRenderer).GetField("_shirtDirty", flags)!.SetValue(__instance.FarmerRenderer, true);
                }
            }
        }
    }
}