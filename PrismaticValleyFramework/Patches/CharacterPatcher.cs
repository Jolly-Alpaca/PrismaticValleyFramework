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
    internal class CharacterPatcher
    {
        private static IMonitor Monitor;

        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Character), nameof(StardewValley.Character.draw), new Type[] {typeof(SpriteBatch), typeof(int), typeof(float)}),
                transpiler: new HarmonyMethod(typeof(CharacterPatcher), nameof(CharacterPatcher.draw_Transpiler))
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
                    new CodeInstruction(OpCodes.Ldarg_0)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromCharacter"));
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