using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;
using StardewModdingAPI;
using PrismaticValleyFramework.Framework;

namespace PrismaticValleyFramework.Patches
{
    internal class ObjectPatcher
    {
        private static IMonitor Monitor;

        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                // DeclaredMethod to target the overload defined in the class
                original: AccessTools.DeclaredMethod(typeof(StardewValley.Object), nameof(StardewValley.Object.drawWhenHeld)),
                transpiler: new HarmonyMethod(typeof(ObjectPatcher), nameof(ObjectPatcher.drawWhenHeld_Transpiler))
            );
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(StardewValley.Object), nameof(StardewValley.Object.drawInMenu)),
                transpiler: new HarmonyMethod(typeof(ObjectPatcher), nameof(ObjectPatcher.drawInMenu_Transpiler))
            );
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(StardewValley.Object), nameof(StardewValley.Object.drawAsProp)),
                transpiler: new HarmonyMethod(typeof(ObjectPatcher), nameof(ObjectPatcher.drawAsProp_Transpiler))
            );
            harmony.Patch(
                // Include list of parameters to target specific overload - Avoids the ambigous method call error
                original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.draw), new Type[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)}),
                transpiler: new HarmonyMethod(typeof(ObjectPatcher), nameof(ObjectPatcher.draw_Transpiler))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.draw), new Type[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(float)}),
                transpiler: new HarmonyMethod(typeof(ObjectPatcher), nameof(ObjectPatcher.draw_Transpiler2))
            );
        }

        internal static IEnumerable<CodeInstruction> drawWhenHeld_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for drawWhenHeld_Transpiler");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_0)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemData"));
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(drawWhenHeld_Transpiler)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }

        internal static IEnumerable<CodeInstruction> drawInMenu_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // Need to handle if alpha multiplier has been applied to the color variable?
            var code = instructions.ToList();
            try
            {
                // Find color * transparency passed as the color argument in the draw methond in drawInMenu
                var matcher = new CodeMatcher(code, il);
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_S, null, "color"),
                    new CodeMatch(OpCodes.Ldarg_S, null, "transparency")
                ).ThrowIfNotMatch("Could not find proper entry point for drawInMenu_Transpiler");
                
                // Replace the color variable (as applicable) before it is multiplied by transparency
                matcher.Insert(
                    new CodeInstruction(OpCodes.Ldloc_0)
                );
                matcher.Advance(2);
                matcher.Insert(
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemDataWithColor", new Type[] {typeof(ParsedItemData), typeof(Color)}))
                );
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(drawInMenu_Transpiler)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }

        /*
          The first call to Color.White is in the the draw for big craftables.
            Modified: Check for an override color in custom fields.
          The second call only applies for vanilla looms and is inside the draw for the wheel part with thread from the object spritesheet.
            Unmodified: No current use case
          The third call is in the draw for shadows of objects (not big craftables) that have one.
            Unmodified.
          The fourth and final call is in the draw for objects (not big craftables).
            Modified: Check for an override color in custom fields.

          Untested: Do not have a test case as this is only called from event.cs
        */
        internal static IEnumerable<CodeInstruction> drawAsProp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                // Find the first call to Color.White
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for drawAsProp_Transpiler");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_3)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemData"));
                // Find the final call to Color.White
                // Begin searching backwards from the end of the instructions
                matcher.End();
                matcher.MatchStartBackwards(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for drawAsProp_Transpiler backwards");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, 9)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemData"));
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(drawAsProp_Transpiler)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }

        /*
          (BC)272: Auto Petter
          The first draw methods are called for the vanilla Auto Petter object. (Unmodified)
          The third draw method is for all big craftables. (Modified)
          The fourth draw method is for the vanilla loom to draw the wheel (see above). (Unmodified)
          The fifth draw method is for BC marked as lamps and does not use the BC texture. (Unmodified)
          The sixth draw method draws hats (presumably scarecrows). This may need to be modified to support static color overrides, but does natively support prismatic. (Unmodified)
          The next three draw methods do not draw the object texture (Unmodified)
          The tenth draw method is for all objects (not big craftables). (Modified)
          The eleventh draw method is for sprinkler attachments. (Modified)
          The thirteenth draw method does not draw an object texture (Unmodified)
          The fourteenth draw method is for held objects within the object (Modified)
        */
        internal static IEnumerable<CodeInstruction> draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                // Find the start of the draw method for big craftables
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldloc_S), // 7
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ParsedItemData), "GetTexture"))
                ).ThrowIfNotMatch("Could not find proper entry point for big craftables in draw_Transpiler in Object");
                // Advance to the call to Color.White in the draw method for big craftables
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for Color.White for big craftables in draw_Transpiler in Object");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, 7)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemData"));
                
                // Find the start of the draw method for non big craftable objects
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldloc_S), // 17
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ParsedItemData), "GetTexture"))
                ).ThrowIfNotMatch("Could not find proper entry point for objects in draw_Transpiler in Object");
                // Advance to the call to Color.White in the draw method for not big craftable objects
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for Color.White for objects in draw_Transpiler in Object");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, 17)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemData"));

                // Find the start of the draw method for sprinkler attachments
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldloc_S), // 21
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ParsedItemData), "GetTexture"))
                ).ThrowIfNotMatch("Could not find proper entry point for sprinkler attachments in draw_Transpiler in Object");
                // Advance to the call to Color.White in the draw method for sprinkler attachments
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for Color.White for sprinkler attachments in draw_Transpiler in Object");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, 21)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemData"));

                // Find the start of the draw method for held objects within the object
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldloc_S) // 25
                ).ThrowIfNotMatch("Could not find proper entry point for held objects within the object in draw_Transpiler in Object");
                // Advance to the call to Color.White in the draw method for held objects within the object
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for Color.White for held objects within the object in draw_Transpiler in Object");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, 24)
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

        /*
          The first draw method is for all big craftables. (Modified)
          The second draw method is for the vanilla loom to draw the wheel (see above). (Unmodified)
          The third draw method is for BC marked as lamps and does not use the BC texture. (Unmodified)
          The fourth draw method is for a shadow. (Unmodified)
          The fifth draw method is for all non big craftable objects. (Modified)
        */
        internal static IEnumerable<CodeInstruction> draw_Transpiler2(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                // Find the first call to Color.White
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for big craftables in draw_Transpiler2 in Object");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, 4)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemData"));

                // Find the final call to Color.White
                // Begin searching backwards from the end of the instructions
                matcher.End();
                matcher.MatchStartBackwards(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Color), "get_White"))
                ).ThrowIfNotMatch("Could not find proper entry point for objects in draw_Transpiler2");
                
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, 5)
                );
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), "getCustomColorFromParsedItemData"));
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(draw_Transpiler2)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }
    }
}