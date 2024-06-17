using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using StardewValley.GameData.BigCraftables;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewModdingAPI;

namespace PrismaticValleyFramework.Framework
{
    static class ParseCustomFields
    {
        public static void getCustomColorFromAnimalEntry(ClickableTextureComponent sprite, AnimalPage.AnimalEntry animal, SpriteBatch b)
        {
            if (animal.Animal is FarmAnimal farmAnimal){
                sprite.draw(b, getCustomColorFromFarmAnimalData(farmAnimal.GetAnimalData()), 0.86f + (float)sprite.bounds.Y / 20000f);
            }
            else sprite.draw(b);
        }

        public static void getCustomColorFromCraftingPage(CraftingPage __instance, ClickableTextureComponent key, SpriteBatch b)
        {
            getCustomColorFromCraftingPage(__instance, key, b, Color.White, 1f, 0.86f + (float)key.bounds.Y / 20000f);
        }
        public static void getCustomColorFromCraftingPage(CraftingPage __instance, ClickableTextureComponent key, SpriteBatch b, Color color, float colorModifier, float layerDepth)
        {
            CraftingRecipe recipe = __instance.pagesOfCraftingRecipes[__instance.currentCraftingPage][key];
            string itemId = recipe.getIndexOfMenuView();
            ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(recipe.bigCraftable ? ("(BC)" + itemId) : itemId);
            key.draw(b, getCustomColorFromParsedItemDataWithColor(dataOrErrorItem, color, true) * colorModifier, layerDepth);
        }
        public static void getCustomColorFromCollectionsPage(ClickableTextureComponent item, SpriteBatch b, Color color, float colorModifier, float layerDepth)
        {
            string[] nameParts = ArgUtility.SplitBySpace(item.name);
            ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(nameParts[0]);
            item.draw(b, getCustomColorFromParsedItemDataWithColor(dataOrErrorItem, color, true) * colorModifier, layerDepth);
        }

        public static Color getCustomColorFromCharacter(Character __instance)
        {
            if (__instance is FarmAnimal FarmAnimalInstance){
                return getCustomColorFromFarmAnimalData(FarmAnimalInstance.GetAnimalData());
            }
            return Color.White;
        }

        public static Color getCustomColorFromFarmAnimalData(FarmAnimalData data)
        {
            if (data != null && data.CustomFields != null && data.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Color", out string? colorString))
                return ColorUtilities.getColorFromString(colorString);
            return Color.White;
        }

        public static Color getCustomColorFromParsedItemData(ParsedItemData data)
        {
            //ModEntry.ModMonitor.Log($"Object Type: {data.ItemType}, {data.ObjectType}.", LogLevel.Debug);
            // Get the color from the custom fields
            if (data != null && data.RawData != null){
                if (data.RawData is ObjectData objectData)
                    return getCustomColorFromObjectData(objectData);
                else if (data.RawData is BigCraftableData bigCraftableData)
                    return getCustomColorFromBigCraftableData(bigCraftableData);
            }
            return Color.White;
        }

        public static Color getCustomColorFromParsedItemDataWithColor(ParsedItemData data, Color color)
        {
            return getCustomColorFromParsedItemDataWithColor(data, color, false);
        }

        public static Color getCustomColorFromParsedItemDataWithColor(ParsedItemData data, Color color, bool overrideColor)
        {
            // Don't overwrite the base color if it is not white
            if (!overrideColor && color != Color.White){
                return color;
            }
            Color customColor = getCustomColorFromParsedItemData(data);
            if (customColor == Color.White) return color;
            if (color == Color.White) return customColor;
            return ColorUtilities.getTintedColor(customColor, color); 
        }

        public static Color getCustomColorFromObjectData(ObjectData data)
        {
            if (data.CustomFields != null && data.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Color", out string? colorString))
                return ColorUtilities.getColorFromString(colorString);
            return Color.White;
        }

        public static Color getCustomColorFromBigCraftableData(BigCraftableData data)
        {
            if (data.CustomFields != null && data.CustomFields.TryGetValue("JollyLlama.PrismaticValleyFramework/Color", out string? colorString))
                return ColorUtilities.getColorFromString(colorString);
            return Color.White;
        }

        private static bool validateInputColor(Color color)
        {
            if (color.A == 255 && color != Color.White) return false;
            return true;
        }
    }
}