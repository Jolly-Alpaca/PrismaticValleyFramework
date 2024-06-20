using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using StardewValley.GameData.BigCraftables;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

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
        /// <returnsThe custom color. Default: Color.White></returns>
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
    }
}