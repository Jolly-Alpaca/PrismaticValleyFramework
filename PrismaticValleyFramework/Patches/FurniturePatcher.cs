using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;
using StardewModdingAPI;
using PrismaticValleyFramework.Framework;

namespace PrismaticValleyFramework.Patches
{
    internal class FurniturePatcher
    {
        private static IMonitor Monitor;

        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(StardewValley.Objects.Furniture), nameof(StardewValley.Objects.Furniture.draw), new Type[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)}),
                transpiler: new HarmonyMethod(typeof(FurniturePatcher), nameof(FurniturePatcher.draw_Transpiler))
            );
        }

        internal static IEnumerable<CodeInstruction> draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                // Only modifying the draw for held object
                // Find the start of the draw method for held object
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldloc_S), // 12
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ParsedItemData), "GetTexture"))
                ).ThrowIfNotMatch("Could not find proper entry point for draw_Transpiler in Furniture");
                // Advance to the call to Color.White in the draw method for held object
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for Color.White for big craftables in draw_Transpiler in Object");
            
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, 12)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemData"));
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