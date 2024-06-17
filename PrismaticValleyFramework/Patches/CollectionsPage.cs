using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using PrismaticValleyFramework.Framework;
using Microsoft.Xna.Framework;
using StardewValley.Menus;

namespace PrismaticValleyFramework.Patches
{
    internal class CollectionsPagePatcher
    {
        private static IMonitor Monitor;

        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                // Method name passed as string to access/patch private methods (nameof cannot be used for private methods)
                original: AccessTools.DeclaredMethod(typeof(StardewValley.Menus.CollectionsPage), nameof(StardewValley.Menus.CollectionsPage.draw)),
                transpiler: new HarmonyMethod(typeof(CollectionsPagePatcher), nameof(CollectionsPagePatcher.draw_Transpiler))
            );
        }

        internal static IEnumerable<CodeInstruction> draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                // Find the first call to Color multiply
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "op_Multiply", new Type[] {typeof(Color), typeof(float)}))
                ).ThrowIfNotMatch("Could not find first proper entry point for draw_Transpiler in CollectionsPage");
                
                matcher.RemoveInstruction(); // Delete first call to Color multiply to pass color and colorModifier in as separate variables to override function
                
                matcher.MatchStartForward(
                    // Find where Color.White is loaded
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "White"))
                ).ThrowIfNotMatch("Could not find second proper entry point for draw_Transpiler in CollectionsPage");
                
                matcher.Advance(1);
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldc_R4, 1.0f) // Load float of 1 as the colorModifier when color is Color.White
                );
                
                // Find the second call to Color multiply
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "op_Multiply", new Type[] {typeof(Color), typeof(float)}))
                ).ThrowIfNotMatch("Could not find third proper entry point for draw_Transpiler in CollectionsPage");

                matcher.RemoveInstruction(); // Delete second call to Color multiply
                matcher.Advance(1);
                matcher.RemoveInstructions(4); // Delete the call to ClickableTextureComponent.draw and the default loads for its 3 optional parameters
                matcher.InsertAndAdvance( // Add the override method to handle the new draw
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromCollectionsPage"))
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