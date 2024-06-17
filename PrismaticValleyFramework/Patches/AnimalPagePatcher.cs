using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using PrismaticValleyFramework.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace PrismaticValleyFramework.Patches
{
    internal class AnimalPagePatcher
    {
        private static IMonitor Monitor;

        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                // Method name passed as string to access/patch private methods (nameof cannot be used for private methods)
                original: AccessTools.Method(typeof(StardewValley.Menus.AnimalPage), "drawNPCSlot"),
                transpiler: new HarmonyMethod(typeof(AnimalPagePatcher), nameof(AnimalPagePatcher.drawNPCSlot_Transpiler))
            );
        }

        internal static IEnumerable<CodeInstruction> drawNPCSlot_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ClickableTextureComponent), "draw", new Type[] {typeof(SpriteBatch)}))
                ).ThrowIfNotMatch("Could not find proper entry point for drawNPCSlot_Transpiler");
                
                matcher.Advance(-1);
                matcher.RemoveInstructions(2);
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromAnimalEntry"))
                );
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(drawNPCSlot_Transpiler)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }
    }
}