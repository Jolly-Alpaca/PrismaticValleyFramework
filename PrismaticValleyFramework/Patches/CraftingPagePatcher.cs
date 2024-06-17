using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using PrismaticValleyFramework.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace PrismaticValleyFramework.Patches
{
    internal class CraftingPagePatcher
    {
        private static IMonitor Monitor;

        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(StardewValley.Menus.CraftingPage), nameof(StardewValley.Menus.CraftingPage.draw)),
                transpiler: new HarmonyMethod(typeof(CraftingPagePatcher), nameof(CraftingPagePatcher.draw_Transpiler))
            );
        }

        internal static IEnumerable<CodeInstruction> draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                // Patch the draw function for grayed out recipes
                matcher.MatchStartForward(
                    // Find where Color.DimGray is loaded
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_DimGray"))
                ).ThrowIfNotMatch("Could not find proper entry point for inactive recipes for draw_Transpiler in CraftingPage");
                
                matcher.Advance(-2); // Move to start of variable loading for call to ClickableTextureComponent.draw
                matcher.InsertAndAdvance( 
                    new CodeInstruction(OpCodes.Ldarg_0) // Load the CraftingPage __instance
                );
                matcher.Advance(4); // Pass over the existing loads for ClickableTextureComponent, Spritebatch, Color.DimGray, and color multiplier
                matcher.RemoveInstruction(); // Remove call to Color.multiply
                matcher.Advance(1); // Pass over the existing load for layer depth
                matcher.RemoveInstructions(4); // Remove the call to ClickableTextureComponent.draw and the 3 prior stack pushes for the optional arguments of ClickableTextureComponent.draw (I only see 2 optional arguments???)
                matcher.InsertAndAdvance( // Add the override method to handle the new draw
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromCraftingPage", new Type[] {typeof(CraftingPage), typeof(ClickableTextureComponent), typeof(SpriteBatch), typeof(Color), typeof(float), typeof(float)}))
                );

                // Patch the draw function for available recipes
                matcher.MatchStartForward(
                    // Find the next call to ClickableTextureComponent.draw(SpriteBatch b)
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ClickableTextureComponent), "draw", new Type[] {typeof(SpriteBatch)}))
                ).ThrowIfNotMatch("Could not find proper entry point for active recipes for draw_Transpiler in CraftingPage");

                matcher.Advance(-2); // Move to start of variable loading for call to ClickableTextureComponent.draw
                matcher.SetAndAdvance(OpCodes.Ldarg_0, null); // Replace the original ldloc.2 with the CraftingPage __instance while maintaining the label
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_2) // Readd ldloc.2
                );
                matcher.Advance(1); // Return to call to ClickableTextureComponent.draw(SpriteBatch b)
                matcher.RemoveInstruction(); // Remove the call to ClickableTextureComponent.draw
                matcher.InsertAndAdvance( // Add the override method to handle the new draw
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromCraftingPage", new Type[] {typeof(CraftingPage), typeof(ClickableTextureComponent), typeof(SpriteBatch)}))
                );
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(draw_Transpiler)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }
    }
}