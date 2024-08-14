using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using StardewValley.GameData.BigCraftables;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using PrismaticValleyFramework.Models;
using StardewModdingAPI;

namespace PrismaticValleyFramework.Framework
{
    static class ParseCustomFields
    {
        /// <summary>
        /// Applies a custom color to the draw of a ClickableTextureComponent representing a FarmAnimal
        /// </summary>
        /// <param name="sprite">The ClickableTextureComponent representing the FarmAnimal to be drawn</param>
        /// <param name="animal">The AnimalEntry for the FarmAnimal to be drawn</param>
        /// <param name="b">The current SpriteBatch</param>
        public static void DrawCustomColorForAnimalEntry(ClickableTextureComponent sprite, AnimalPage.AnimalEntry animal, SpriteBatch b)
        {
            if (animal.Animal is FarmAnimal farmAnimal){
                sprite.draw(b, getCustomColorFromFarmAnimalData(farmAnimal.GetAnimalData()), 0.86f + (float)sprite.bounds.Y / 20000f);
            }
            else sprite.draw(b);
        }

        /// <summary>
        /// Applies a custom color to the draw of a ClickableTextureComponent representing the output of a CraftingRecipe
        /// </summary>
        /// <param name="__instance">The CraftingPage instance calling this method</param>
        /// <param name="key">The ClickableTextureComponent representing the output of a CraftingRecipe to be drawn</param>
        /// <param name="b">The current SpriteBatch</param>
        public static void DrawCustomColorForCraftingPage(CraftingPage __instance, ClickableTextureComponent key, SpriteBatch b)
        {
            DrawCustomColorForCraftingPage(__instance, key, b, Color.White, 1f, 0.86f + (float)key.bounds.Y / 20000f);
        }

        /// <summary>
        /// Applies a custom color to the draw of a ClickableTextureComponent representing the output of a CraftingRecipe
        /// </summary>
        /// <param name="__instance">The CraftingPage instance calling this method</param>
        /// <param name="key">The ClickableTextureComponent representing the output of a CraftingRecipe to be drawn</param>
        /// <param name="b">The current SpriteBatch</param>
        /// <param name="color">The additional tint color to apply</param>
        /// <param name="colorModifier">The multiplier to apply to each color component of the tint</param>
        /// <param name="layerDepth">The depth of the layer to draw the ClickableTextureComponent</param>
        public static void DrawCustomColorForCraftingPage(CraftingPage __instance, ClickableTextureComponent key, SpriteBatch b, Color color, float colorModifier, float layerDepth)
        {
            // Pull the CraftingRecipe from the crafting recipes dictionary using the ClickableTextureComponent as the key
            CraftingRecipe recipe = __instance.pagesOfCraftingRecipes[__instance.currentCraftingPage][key];
            // Pull the itemId from the CraftingRecipe to get the item data. Needed to access the custom fields for the custom color override.
            string itemId = recipe.getIndexOfMenuView();
            ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(recipe.bigCraftable ? ("(BC)" + itemId) : itemId);
            // Draw the ClickableTextureComponent using the custom color
            key.draw(b, getCustomColorFromParsedItemDataWithColor(dataOrErrorItem, color, true) * colorModifier, layerDepth);
        }

        /// <summary>
        /// Applies a custom color to the draw of a ClickableTextureComponent representing the output of an Object
        /// </summary>
        /// <param name="item">The ClickableTextureComponent representing the Object to be drawn</param>
        /// <param name="b">The current SpriteBatch</param>
        /// <param name="color">The additional tint color to apply</param>
        /// <param name="colorModifier">The multiplier to apply to each color component of the tint</param>
        /// <param name="layerDepth">The depth of the layer to draw the ClickableTextureComponent</param>
        public static void DrawCustomColorForCollectionsPage(ClickableTextureComponent item, SpriteBatch b, Color color, float colorModifier, float layerDepth)
        {
            // Pull the itemID from the first part of the ClickableTextureComponent's name to get the item data
            string[] nameParts = ArgUtility.SplitBySpace(item.name);
            ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(nameParts[0]);
            // Draw the ClickableTextureComponent using the custom color
            item.draw(b, getCustomColorFromParsedItemDataWithColor(dataOrErrorItem, color, true) * colorModifier, layerDepth);
        }

        /// <summary>
        /// Get the custom color from the custom fields of a Character instance
        /// </summary>
        /// <param name="__instance">The Character instance to pull the custom fields from</param>
        /// <returns>The custom color. Default: Color.White</returns>
        public static Color getCustomColorFromCharacter(Character __instance)
        {
            if (__instance is FarmAnimal FarmAnimalInstance){
                return getCustomColorFromFarmAnimalData(FarmAnimalInstance.GetAnimalData());
            }
            return Color.White;
        }

        /// <summary>
        /// Get the custom color from the custom fields of a FarmAnimal instance
        /// </summary>
        /// <remarks>Target custom field: JollyLlama.PrismaticValleyFramework/Color</remarks>
        /// <param name="data">The metadata for the FarmAnimal to pull the custom fields from</param>
        /// <returns>The custom color. Default: Color.White</returns>
        public static Color getCustomColorFromFarmAnimalData(FarmAnimalData data)
        {
            if (data != null && data.CustomFields != null && data.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Color", out string? colorString))
            {
                if (data.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Palette", out string? paletteString))
                    return ColorUtilities.getColorFromString(colorString, paletteString);
                return ColorUtilities.getColorFromString(colorString);
            }
            return Color.White;
        }

        /// <summary>
        /// Get the custom color from the custom fields of an Item instance
        /// </summary>
        /// <remarks>Supported Item types: Object, BigCraftable</remarks>
        /// <param name="data">The base parsed metadata for an Item to pull the custom fields from</param>
        /// <returns>The custom color. Default: Color.White</returns>
        public static Color getCustomColorFromParsedItemData(ParsedItemData data)
        {
            // Get the color from the custom fields of the respective Item type
            if (data != null && data.RawData != null){
                if (data.RawData is ObjectData objectData)
                    return getCustomColorFromObjectData(objectData);
                else if (data.RawData is BigCraftableData bigCraftableData)
                    return getCustomColorFromBigCraftableData(bigCraftableData);
            }
            return Color.White;
        }

        /// <summary>
        /// Get the custom color from the custom fields of an Item instance if <paramref name="color"/> is Color.White.
        /// </summary>
        /// <remarks>Intended to preserve non-white tints passed to the original draw function</remarks>
        /// <param name="data">The base parsed metadata for an Item to pull the custom fields from</param>
        /// <param name="color">The additional tint color to apply</param>
        /// <returns>The custom color. Default: <paramref name="color"/></returns>
        public static Color getCustomColorFromParsedItemDataWithColor(ParsedItemData data, Color color)
        {
            return getCustomColorFromParsedItemDataWithColor(data, color, false);
        }

        /// <summary>
        /// Get the custom color from the custom fields of an Item instance with additional tint applied
        /// </summary>
        /// <param name="data">The base parsed metadata for an Item to pull the custom fields from</param>
        /// <param name="color">The additional tint color to apply</param>
        /// <param name="overrideColor">If <paramref name="color"/> is not Color.White, flags whether to return <paramref name="color"/> unchanged or blended with the custom color</param>
        /// <returns>The custom color. Default: <paramref name="color"/></returns>
        public static Color getCustomColorFromParsedItemDataWithColor(ParsedItemData data, Color color, bool overrideColor)
        {
            // Don't overwrite the base color if it is not white
            if (!overrideColor && color != Color.White){
                return color;
            }
            // Get the custom color from the item metadata
            Color customColor = getCustomColorFromParsedItemData(data);
            // No need to blend the colors if either is Color.White
            if (customColor == Color.White) return color;
            if (color == Color.White) return customColor;
            // Return the blended color
            return ColorUtilities.getTintedColor(customColor, color); 
        }

        /// <summary>
        /// Get the custom color from the custom fields of an Object instance
        /// </summary>
        /// <remarks>Target custom field: JollyLlama.PrismaticValleyFramework/Color</remarks>
        /// <param name="data">The metadata for an Object type item to pull the custom fields from</param>
        /// <returns>The custom color. Default: Color.White</returns>
        public static Color getCustomColorFromObjectData(ObjectData data)
        {
            if (data.CustomFields != null && data.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Color", out string? colorString))
            {
                if (data.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Palette", out string? paletteString))
                    return ColorUtilities.getColorFromString(colorString, paletteString);
                return ColorUtilities.getColorFromString(colorString);
            }
            return Color.White;
        }

        /// <summary>
        /// Get the custom color from the custom fields of a BigCraftable instance
        /// </summary>
        /// <remarks>Target custom field: JollyLlama.PrismaticValleyFramework/Color</remarks>
        /// <param name="data">The metadata for a BigCraftable type item to pull the custom fields from</param>
        /// <returns>The custom color. Default: Color.White></returns>
        public static Color getCustomColorFromBigCraftableData(BigCraftableData data)
        {
            if (data.CustomFields != null && data.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Color", out string? colorString))
            {
                if (data.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Palette", out string? paletteString))
                    return ColorUtilities.getColorFromString(colorString, paletteString);
                return ColorUtilities.getColorFromString(colorString);
            }
            return Color.White;
        }
        
        /// <summary>
        /// Get the custom color from the ModColorData of a string dictionary instance (e.g. Boots) with additional tint applied
        /// </summary>
        /// <param name="itemId">The qualified or unqualified item ID</param>
        /// <param name="color">The additional tint color to apply</param>
        /// <returns>The custom color. Default: <paramref name="color"/></returns>
        public static Color getCustomColorFromStringDictItemWithColor(string itemId, Color color)
        {
            // Get the custom color from the ModColorData
            Color customColor = getCustomColorFromStringDictItem(itemId);
            // No need to blend the colors if either is Color.White
            if (customColor == Color.White) return color;
            if (color == Color.White) return customColor;
            // Return the blended color
            return ColorUtilities.getTintedColor(customColor, color); 
        }

        /// <summary>
        /// Get the custom color from the ModColorData of a string dictionary instance (e.g. Boots)
        /// </summary>
        /// <remarks>Target ModColorData for instance</remarks>
        /// <param name="itemId">The unqualified item ID</param>
        /// <returns>The custom color. Default: Color.White</returns>
        public static Color getCustomColorFromStringDictItem(string itemId)
        {
            // Pull the ModColorData for the item from the dictionary if it exists
            if (ModEntry.ModCustomColorData.TryGetValue(itemId, out  ModColorData? ItemColorData))
            {
                if (ItemColorData.Palette != null)
                    return ColorUtilities.getColorFromString(ItemColorData.Color, ItemColorData.Palette);
                return ColorUtilities.getColorFromString(ItemColorData.Color);
            }
            return Color.White;
        }

        /// <summary>
        /// Get the target string for the custom texture from the ModColorData of a string dictionary instance (e.g. Boots)
        /// </summary>
        /// <param name="itemId">The unqualified item ID</param>
        /// <returns>The target string for the custom texture. Default: null</returns>
        public static string? getCustomTextureTargetFromStringDictItem(string itemId)
        {
            // Pull the ModColorData for the item from the dictionary if it exists
            if (ModEntry.ModCustomColorData.TryGetValue(itemId, out  ModColorData? ItemColorData))
            {
                if (ItemColorData.TextureTarget != null) return ItemColorData.TextureTarget;
            }
            return null;
        }

        /// <summary>
        /// Determine if the item has custom color data
        /// </summary>
        /// <param name="itemId">The unqualified item ID</param>
        /// <returns>True if the item has custom color data. Default false</returns>
        public static bool HasCustomColorData(string itemId)
        {
            // Check if there is custom color data for the given item
            if (ModEntry.ModCustomColorData.ContainsKey(itemId)) return true;
            return false;
        }

        /// <summary>
        /// Overwrite the path to load the skinColors texture and its index (which) after they are set if custom override color is currently active
        /// </summary>
        /// <param name="skinColorsPath">The default path to load the texture from</param>
        /// <param name="__instance">The calling FarmerRender instance</param>
        /// <param name="which">The index in the texture to use</param>
        /// <returns>The path to load the texture from</returns>
        public static string getCustomColorsTextureFromConfig(string skinColorsPath, FarmerRenderer __instance, ref int which)
        {
            // Get the Farmer associated with the FarmerRenderer instance
            // Check if the farmer currently has the Custom Color Buff
            if (Game1.getAllFarmers().FirstOrDefault(x => x.FarmerRenderer == __instance) is { } farmer && farmer.hasBuff($"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff"))
            {
                // Load the custom texture (white) for optimal color saturation 
                which = 0;
                skinColorsPath = $"{ModEntry.Instance.ModManifest.UniqueID}\\customSkinColor";
            }
            return skinColorsPath;
        }

        /// <summary>
        /// Overwrite the path to load the skinColors texture from if custom override color is currently active
        /// </summary>
        /// <param name="skinColorsPath">The default path to load the texture from</param>
        /// <param name="__instance">The calling FarmerRender instance</param>
        /// <returns>The path to load the texture from</returns>
        public static string getCustomColorsTexturePathFromConfig(string skinColorsPath, FarmerRenderer __instance)
        {
            // Get the Farmer associated with the FarmerRenderer instance
            // Check if the farmer currently has the Custom Color Buff
            if (Game1.getAllFarmers().FirstOrDefault(x => x.FarmerRenderer == __instance) is { } farmer && farmer.hasBuff($"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff"))
            {
                // Load the custom texture (white) for optimal color saturation 
                skinColorsPath = $"{ModEntry.Instance.ModManifest.UniqueID}\\customSkinColor";
            }
            return skinColorsPath;
        }

        /// <summary>
        /// Overwrite the skinColors texture index after it is set if custom override color is currently active
        /// </summary>
        /// <param name="__instance">The calling FarmerRender instance</param>
        /// <param name="index">The index in the texture to use</param>
        /// <returns>The path to load the texture from</returns>
        public static void getCustomColorsIndexFromConfig(FarmerRenderer __instance, ref int index)
        {
            // Get the Farmer associated with the FarmerRenderer instance
            // Check if the farmer currently has the Custom Color Buff
            if (Game1.getAllFarmers().FirstOrDefault(x => x.FarmerRenderer == __instance) is { } farmer && farmer.hasBuff($"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff"))
            {
                index = 0;
            }
        }

        public static Color getCustomColorFromFarmerModData(Farmer who, Color color)
        {
            string colorBuffName = $"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff";
            // Return default color if farmer does not have custom color buff
            if (!who.hasBuff(colorBuffName)) return color;
            
            // Pull the farmer's color buff data from ModCustomColorData dictionary
            Color customColor = getCustomColorFromStringDictItem(string.Concat(colorBuffName, who.UniqueMultiplayerID));
            // No need to blend the colors if either is Color.White
            if (customColor == Color.White) return color;
            if (color == Color.White) return customColor;
            // Return the blended color
            return ColorUtilities.getTintedColor(customColor, color); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Adding method to this class in case add support for custom sleeve colors (in which parsing custom fields would be required)</remarks>
        /// <param name="__instance"></param>
        /// <param name="who"></param>
        public static void ApplySleeveColorToCustomSleeves(FarmerRenderer __instance, Farmer who)
        {
            // Return if farmer does not have custom color buff
            if (!who.hasBuff($"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff")) return;
            
            string texture_name = $"{ModEntry.Instance.ModManifest.UniqueID}\\{(who.IsMale ? "farmer_sleeves" : "farmer_girl_sleeves")}";
            // Add the custom texture's pixel indices to FarmerRenderer.recolorOffsets if they have not been added
            if (!FarmerRenderer.recolorOffsets.ContainsKey(texture_name))
                AddTextureToRecolorOffsets(__instance, texture_name);

            // Apply sleeve color update to custom texture in the cache
            ModEntry.SleevesTextureCache.UpdateFarmerSleevesTexture(who, texture_name);  
        }

        /// <summary>
        /// Add the custom texture to FarmerRenderer instance's recolorOffsets dictionary
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="texture_name"></param>
        internal static void AddTextureToRecolorOffsets(FarmerRenderer __instance, string texture_name)
        {
            // Add the entry to FarmerRenderer.recolorOffsets to prevent the code from crashing when trying to access the dictionary entry
            FarmerRenderer.recolorOffsets[texture_name] = new Dictionary<int, List<int>>();
            // Get the FarmerRenderer's local content manager
            if (ModEntry.Instance.Helper.Reflection.GetField<LocalizedContentManager>(__instance, "farmerTextureManager").GetValue() is LocalizedContentManager farmerTextureManager)
            {
                // Load the custom texture data
                Texture2D source_texture = farmerTextureManager.Load<Texture2D>(texture_name);
                Color[] source_pixel_data = new Color[source_texture.Width * source_texture.Height];
                source_texture.GetData(source_pixel_data);

                // Generate the sleeve pixel indices for the custom texture
                // Use reflection to access the private method FarmerRenderer._GeneratePixelIndices; This adds the data to recolorOffsets
                if (ModEntry.Instance.Helper.Reflection.GetMethod(__instance, "_GeneratePixelIndices") is IReflectedMethod GeneratePixelIndices)
                {
                    GeneratePixelIndices.Invoke(256, texture_name, source_pixel_data);
                    GeneratePixelIndices.Invoke(257, texture_name, source_pixel_data);
                    GeneratePixelIndices.Invoke(258, texture_name, source_pixel_data);
                }
            }
        }
    }
}