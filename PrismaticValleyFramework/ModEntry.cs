using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using PrismaticValleyFramework.Patches;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PrismaticValleyFramework
{
    internal sealed class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; private set; } = null!;

        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static Harmony Harmony { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            ModHelper = helper;
            
            // Apply Harmony patches
            var harmony = new Harmony(this.ModManifest.UniqueID);
            //Harmony.DEBUG = true;
            FarmAnimalPatcher.Apply(ModMonitor, harmony);
            AnimalPagePatcher.Apply(ModMonitor, harmony);
            //CharacterPatcher.Apply(ModMonitor, harmony);
            ObjectPatcher.Apply(ModMonitor, harmony);
            FurniturePatcher.Apply(ModMonitor, harmony);
            CraftingPagePatcher.Apply(ModMonitor, harmony);
            CollectionsPagePatcher.Apply(ModMonitor, harmony);
        }
    }
}
