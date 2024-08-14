using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using PrismaticValleyFramework.Patches;
using PrismaticValleyFramework.Models;
using PrismaticValleyFramework.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.Buffs;

namespace PrismaticValleyFramework
{
    internal sealed class ModEntry : Mod
    {
        public static ModEntry Instance = null!;
        internal static IMonitor ModMonitor { get; private set; } = null!;

        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static Harmony Harmony { get; private set; } = null!;
        public static Dictionary<string, ModColorData> ModCustomColorData = new ();
        internal static CustomSleevesCache SleevesTextureCache { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            ModMonitor = Monitor;
            ModHelper = helper;
            SleevesTextureCache = new CustomSleevesCache();
            
            // Apply Harmony patches
            var harmony = new Harmony(this.ModManifest.UniqueID);
            //Harmony.DEBUG = true;

            // Apply FarmAnimal patches
            FarmAnimalPatcher.Apply(ModMonitor, harmony);
            AnimalPagePatcher.Apply(ModMonitor, harmony);
            //CharacterPatcher.Apply(ModMonitor, harmony);

            // Apply object, big craftable, and boots patches
            ObjectPatcher.Apply(ModMonitor, harmony);
            FurniturePatcher.Apply(ModMonitor, harmony);
            CraftingPagePatcher.Apply(ModMonitor, harmony);
            CollectionsPagePatcher.Apply(ModMonitor, harmony);
            LibraryMuseumPatcher.Apply(ModMonitor, harmony);
            BootsPatcher.Apply(ModMonitor, harmony);
            FarmerRendererPatcher.Apply(ModMonitor, harmony);

            // Apply Farmer patches
            FarmerPatcher.Apply(ModMonitor, harmony);
            BuffManagerPatcher.Apply(ModMonitor, harmony);

            // Handle assets
            ModHelper.Events.Content.AssetRequested += OnAssetRequested;
            ModHelper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
            ModHelper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // Load custom boot data (equivalent to CustomFields field for data structures that don't have a CustomFields field)
            if (e.NameWithoutLocale.IsEquivalentTo("JollyLlama.PrismaticValleyFramework"))
            {
                e.LoadFrom(() => new Dictionary<string, ModColorData>(), AssetLoadPriority.Exclusive);
            }

            // Load custom Buff data
            if (e.NameWithoutLocale.IsEquivalentTo("Data\\Buffs"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, BuffData>().Data;

                    data[$"{ModManifest.UniqueID}.ColorBuff"] = new()
                    {
                        DisplayName = Helper.Translation.Get("Buff.DefaultName"),
                        Description = Helper.Translation.Get("Buff.DefaultDescription"),
                        Duration = -2,
                        IconTexture = $"{ModManifest.UniqueID}\\BuffIcon",
                        IconSpriteIndex = 0,
                    };
                });
            }

            // Load male shoe texture
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}\\farmer_shoes"))
            {
                e.LoadFromModFile<Texture2D>("Assets/farmer_shoes.png", AssetLoadPriority.Medium);
            }
            // Load female shoe texture
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}\\farmer_girl_shoes"))
            {
                e.LoadFromModFile<Texture2D>("Assets/farmer_girl_shoes.png", AssetLoadPriority.Medium);
            }

            // Load male sleeve texture
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}\\farmer_sleeves"))
            {
                e.LoadFromModFile<Texture2D>("Assets/farmer_sleeves.png", AssetLoadPriority.Medium);
            }
            // Load female sleeve texture
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}\\farmer_girl_sleeves"))
            {
                e.LoadFromModFile<Texture2D>("Assets/farmer_girl_sleeves.png", AssetLoadPriority.Medium);
            }

            // Load male eye texture
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}\\farmer_eyes"))
            {
                e.LoadFromModFile<Texture2D>("Assets/farmer_eyes.png", AssetLoadPriority.Medium);
            }
            // Load female eye texture
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}\\farmer_girl_eyes"))
            {
                e.LoadFromModFile<Texture2D>("Assets/farmer_girl_eyes.png", AssetLoadPriority.Medium);
            }

            // Load custom skin color texture
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}\\customSkinColor"))
            {
                e.LoadFromModFile<Texture2D>("Assets/baseSkinColor.png", AssetLoadPriority.Medium);
            }

            // Load custom Buff icon
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}\\BuffIcon"))
            {
                e.LoadFromModFile<Texture2D>("Assets/BuffIcon.png", AssetLoadPriority.Medium);
            }
        }

        private void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
        {
            // Reload the custom asset if the asset is marked as invalidated
            if (e.NamesWithoutLocale.Any(an => an.IsEquivalentTo("JollyLlama.PrismaticValleyFramework")))
            {
                ModCustomColorData = Game1.content.Load<Dictionary<string, ModColorData>>("JollyLlama.PrismaticValleyFramework");
            }
        }

        private static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Load the custom asset on game launch
            ModCustomColorData = Game1.content.Load<Dictionary<string, ModColorData>>("JollyLlama.PrismaticValleyFramework");
        }
    }
}
