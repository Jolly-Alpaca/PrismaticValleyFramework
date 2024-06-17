using System;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;
using PrismaticValleyFramework.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PrismaticValleyFramework.Patches
{
    internal class FarmAnimalPatcher
    {
        private static IMonitor Monitor;

        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(StardewValley.FarmAnimal), nameof(StardewValley.FarmAnimal.draw)),
                transpiler: new HarmonyMethod(typeof(FarmAnimalPatcher), nameof(FarmAnimalPatcher.draw_Transpiler))
            );
        }

        internal static IEnumerable<CodeInstruction> draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for draw_Transpiler");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_2)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromFarmAnimalData"));
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