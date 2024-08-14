using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using PrismaticValleyFramework.Framework;
using StardewValley;

namespace PrismaticValleyFramework.Patches
{
    internal class FarmerRendererPatcher
    {
        private static IMonitor Monitor;

        /// <summary>
        /// Apply the harmony patches to FarmerRenderer.cs
        /// </summary>
        /// <param name="monitor">The Monitor instance for the PrismaticValleyFramework module</param>
        /// <param name="harmony">The Harmony instance for the PrismaticvalleyFramework module</param>
        internal static void Apply(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
            harmony.Patch(
                // Use Method with overload parameters as FarmerRenderer.cs has multiple implementations of the draw method
                original: AccessTools.Method(typeof(FarmerRenderer), "draw", new Type[] {typeof(SpriteBatch), typeof(FarmerSprite.AnimationFrame), typeof(int), typeof(Rectangle), typeof(Vector2), typeof(Vector2), typeof(float), typeof(int), typeof(Color), typeof(float), typeof(float), typeof(Farmer)}),
                transpiler: new HarmonyMethod(typeof(FarmerRendererPatcher), nameof(draw_Transpiler))
            );
            harmony.Patch(
                // executeRecolorActions is a private method so cannot be referenced using nameof
                original: AccessTools.DeclaredMethod(typeof(FarmerRenderer), "executeRecolorActions"),
                transpiler: new HarmonyMethod(typeof(FarmerRendererPatcher), nameof(executeRecolorActions_Transpiler))
            );
            harmony.Patch(
                // ApplySkinColor is a private method so cannot be referenced using nameof
                original: AccessTools.DeclaredMethod(typeof(FarmerRenderer), "ApplySkinColor"),
                transpiler: new HarmonyMethod(typeof(FarmerRendererPatcher), nameof(ApplySkinColor_Transpiler))
            );
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(FarmerRenderer), nameof(FarmerRenderer.ApplySleeveColor)),
                transpiler: new HarmonyMethod(typeof(FarmerRendererPatcher), nameof(ApplySleeveColor_Transpiler))
            );
        }

        /// <summary>
        /// Transpiler instructions to patch the draw method in FarmerRenderer.cs. 
        /// Overwrites the color parameter passed to SpriteBatch.draw drawing the base farmer sprite texture to call a custom method instead.
        /// Calls a custom method to draw custom boot sprites if the currently equiped boots have a custom color override.
        /// </summary>
        /// <param name="instructions">The IL instructions</param>
        /// <param name="il">The IL generator</param>
        /// <returns>The patched IL instructions</returns>
        internal static IEnumerable<CodeInstruction> draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);
                // Find where overrideColor is loaded for the call to b.draw
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_S, null, "overrideColor"), // Load overrideColor arg
                    new CodeMatch(OpCodes.Ldarg_S, null, "rotation"), // Load rotation arg
                    new CodeMatch(OpCodes.Ldarg_S, null, "origin") // Load origin arg
                ).ThrowIfNotMatch("Could not find proper entry point for b.draw in draw_Transpiler in FarmerRenderer");

                // Overwrite the load for overrideColor arg
                PatchInstructionsForFarmerCustomColorMethod(ref matcher);

                // Call a custom method to draw overrides to the base farmer sprite
                // Find where b.draw is called for the base farmer sprite
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt) // Call b.draw for base farmer sprite
                ).ThrowIfNotMatch("Could not find proper entry point for b.draw for base farmer in draw_Transpiler in FarmerRenderer");

                // Skip over the draw for base farmer sprite
                matcher.Advance(1);
                // Load the parameters for the custom draw method
                PatchInstructionsForCustomDrawMethod(ref matcher);
                matcher.InsertAndAdvance(
                    // Load additional parameters for the custom draw method
                    new CodeInstruction(OpCodes.Ldc_I4_2), // Loads 2 onto the stack as the parameter FarmerSpriteLayers layer arg of GetLayerDepth. FarmerSpriteLayers in an enum, where Base is at index 2.
                    new CodeInstruction(OpCodes.Ldc_I4_0), // Loads 0 onto the stack to pass false to the optional bool arg of GetLayerDepth
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FarmerRenderer), nameof(FarmerRenderer.GetLayerDepth))), // Call FarmerRenderer.GetLayerDepth
                    // Insert a call to the custom draw features method
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomDrawUtilities), nameof(CustomDrawUtilities.DrawCustomFeatures)))
                );

                // Overwrite the color parameter passed to SpriteBatch.draw drawing the face and eyes (farmer swimming)
                PatchOverrideColorForFaceAndEyes(ref matcher, "face 1");
                PatchOverrideColorForFaceAndEyes(ref matcher, "eyes 1");
                // Draw the whites of the eyes separately to not apply prismatic effect (farmer swimming)
                AddDrawForEyes(ref matcher, true);

                // Draw the boots just before the pants are drawn as this is after the swimming check. No need to draw shoes if the farmer is swimming.
                // Find the call to Farmer.GetDisplayPants 
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_S, null, "who"), // Load Farmer arg who
                    new CodeMatch(OpCodes.Ldloca_S), // Load the address for local variable 1 to store texture from GetDisplayPants
                    new CodeMatch(OpCodes.Ldloca_S)  // Load the address for local variable 2 to store pantsIndex from GetDisplayPants
                ).ThrowIfNotMatch("Could not find proper entry point for Farmer.GetDisplayPants in draw_Transpiler in FarmerRenderer");

                // Call a custom method to draw the custom boots
                // Replace the load Farmer who arg for GetDisplayPants to steal the labels so the custom draw method isn't added to an unreachable code block
                matcher.SetAndAdvance(OpCodes.Ldarg_0, null); // Loads the FarmerRenderer instance
                // Load the parameters for the custom draw method
                PatchInstructionsForCustomDrawMethod(ref matcher);
                matcher.InsertAndAdvance(
                    // Load additional parameters for the custom draw method
                    new CodeInstruction(OpCodes.Ldc_I4_2), // Loads 2 onto the stack as the parameter FarmerSpriteLayers layer arg of GetLayerDepth. FarmerSpriteLayers in an enum, where Base is at index 2.
                    new CodeInstruction(OpCodes.Ldc_I4_0), // Loads 0 onto the stack to pass false to the optional bool arg of GetLayerDepth
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FarmerRenderer), nameof(FarmerRenderer.GetLayerDepth))), // Call FarmerRenderer.GetLayerDepth
                    // Insert a call to the custom draw boots method
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomDrawUtilities), nameof(CustomDrawUtilities.DrawCustomBoots))),
                    // Readd the load Farmer who arg for GetDisplayPants
                    new CodeInstruction(OpCodes.Ldarg_S, 12)
                );

                // Overwrite the color parameter passed to SpriteBatch.draw drawing the face and eyes (general)
                PatchOverrideColorForFaceAndEyes(ref matcher, "face 2");
                PatchOverrideColorForFaceAndEyes(ref matcher, "eyes 2");
                // Draw the whites of the eyes separately to not apply prismatic effect (general)
                AddDrawForEyes(ref matcher, false);

                // Overwrite the color parameter passed to SpriteBatch.draw drawing the farmer's arms
                // Find where overrideColor is loaded for the first call to b.draw to draw the arms
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_S, null, "overrideColor"), // Load overrideColor arg
                    new CodeMatch(OpCodes.Ldarg_S, null, "rotation"), // Load rotation arg
                    new CodeMatch(OpCodes.Ldarg_S, null, "origin"), // Load origin arg
                    new CodeMatch(OpCodes.Ldc_R4) // Load a float to the stack
                ).ThrowIfNotMatch("Could not find proper entry point for overrideColor for b.draw for arms in draw_Transpiler in FarmerRenderer");

                // Overwrite the load for overrideColor arg
                PatchInstructionsForFarmerCustomColorMethod(ref matcher);

                // Call a custom method to draw the custom sleeves after the arms to draw them above the arm
                // Find where b.draw is called for the arms
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt) // Call b.draw for arms
                ).ThrowIfNotMatch("Could not find proper entry point for b.draw for arms in draw_Transpiler in FarmerRenderer");

                // Skip over the draw for arms to add the draw for sleeves after
                matcher.Advance(1);
                // Load the parameters for the custom draw method
                PatchInstructionsForCustomDrawMethod(ref matcher);
                matcher.Insert(
                    // Load additional parameters for the custom draw method
                    new CodeInstruction(OpCodes.Ldloc_S, 4), // Loads the armLayer local variable onto the stack as the second arg of GetLayerDepth
                    new CodeInstruction(OpCodes.Ldc_I4_0), // Loads 0 onto the stack to pass false to the optional bool arg of GetLayerDepth
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FarmerRenderer), nameof(FarmerRenderer.GetLayerDepth))), // Call FarmerRenderer.GetLayerDepth
                    // Insert a call to the custom draw sleeves method
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomDrawUtilities), nameof(CustomDrawUtilities.DrawCustomSleeves)))
                );

                // Overwrite the calls to Color.White for drawing the arms with slingshot
                // Find the call to Color.White; Targetting only the calls b.draw, not Utility.drawLineWithScreenCoordinates
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(Rectangle?), new Type[] {typeof(int), typeof(int), typeof(int), typeof(int)})),
                    new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Color), nameof(Color.White)))
                ).ThrowIfNotMatch("Could not find proper entry point for b.draw for arms (slingshot) in draw_Transpiler in FarmerRenderer");

                matcher.Repeat( matcher =>
                    {
                        // Advance over the rectangle constructor
                        matcher.Advance(1);
                        // Overwrite the call to Color.White
                        PatchInstructionsForFarmerCustomColorMethod(ref matcher);
                    }
                );
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(draw_Transpiler)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }

        /// <summary>
        /// Transpiler instructions to patch the executeRecolorActions method in FarmerRenderer.cs.
        /// Calls ApplySleeveColor to custom sleeve texture if a custom override color is currently active
        /// </summary>
        /// <param name="instructions">The IL instructions</param>
        /// <param name="il">The IL generator</param>
        /// <returns>The patched IL instructions</returns>
        internal static IEnumerable<CodeInstruction> executeRecolorActions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                // Call a custom method to call ApplySleeveColor to a custom sleeve texture if a custom override color is active 
                // Find the call to ApplySleeveColor for the base farmer texture
                var matcher = new CodeMatcher(code, il);
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(FarmerRenderer), nameof(FarmerRenderer.ApplySleeveColor))) // Load the skinColors texture string
                ).ThrowIfNotMatch("Could not find proper entry point for executeRecolorActions_Transpiler in FarmerRenderer");
                // Advance to call the custom method after ApplySleeveColor is called for the base farmer texture
                matcher.Advance(1);

                // Call a method to call ApplySleeveColor to a custom texture
                matcher.Insert(
                    // Load the additional parameters for the custom method
                    new CodeInstruction(OpCodes.Ldarg_0), // Loads the FarmerRenderer instance
                    new CodeInstruction(OpCodes.Ldarg_1), // Loads the Farmer farmer arg
                    // Insert a call to the custom method
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), nameof(ParseCustomFields.ApplySleeveColorToCustomSleeves)))
                );
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(executeRecolorActions_Transpiler)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }

        /// <summary>
        /// Transpiler instructions to patch the ApplySkinColor method in FarmerRenderer.cs.
        /// Calls a custom method to overwrite the skinColors texture path string and its index 
        /// if a custom override color is currently active
        /// </summary>
        /// <remarks>
        /// Implementing the texture change here to preserve the original values in the Farmer's skin variables
        /// to revert back to when the buff ends
        /// </remarks>
        /// <param name="instructions">The IL instructions</param>
        /// <param name="il">The IL generator</param>
        /// <returns>The patched IL instructions</returns> 
        internal static IEnumerable<CodeInstruction> ApplySkinColor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                // Call a custom method to overwrite the skinColors texture string and index (which) 
                // if custom override color is currently active
                // Find the first instance where a string (ldstr) is loaded to the stack 
                var matcher = new CodeMatcher(code, il);
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldstr) // Load the skinColors texture string
                ).ThrowIfNotMatch("Could not find proper entry point for ApplySkinColor_Transpiler in FarmerRenderer");
                // Advance to pass the string as a parameter
                matcher.Advance(1);

                // Call a method to overwrite the index (which) and skinColors texture string
                matcher.Insert(
                    // Load the additional parameters for the custom skin color method
                    new CodeInstruction(OpCodes.Ldarg_0), // Loads the FarmerRenderer instance
                    new CodeInstruction(OpCodes.Ldloca_S, 0), // Loads the address of the which local variable (the index of the colors in the colors texture to use)
                    // Insert a call to the custom skin color method
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), nameof(ParseCustomFields.getCustomColorsTextureFromConfig)))
                );
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(ApplySkinColor_Transpiler)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }

        /// <summary>
        /// Suppress the call to ApplySleeveColor if the texture is the custom sleeve texture and the currently equipped shirt
        /// has no sleeves
        /// </summary>
        /// <param name="texture_name">The texture to be modified by ApplySleeveColor</param>
        /// <param name="who">The Farmer of the calling FarmerRenderer</param>
        /// <returns>False if ApplySleeveColor should not be executed. Default: True</returns>
        internal static bool ApplySleeveColor_Prefix(string texture_name, Farmer who)
        {
            try
            {
                // Only suppress the call to ApplySleeveColor if the texture being updated is the custom sleeve texture
                if (!texture_name.StartsWith($"{ModEntry.Instance.ModManifest.UniqueID}")) return true;
                
                // Get the variables required to duplicate the no sleeves check in ApplySleeveColor
                // Not including (this.skin.Value == -12345 && who.shirtItem.Value == null) case from the original as this.skin.Value is a private field
                    // This case only applies to mannequins. Including this case will likely only be necessary if adding some wearable that somehow applies the prismatic skin buff to a manniquin
                who.GetDisplayShirt(out var shirtTexture, out var shirtIndex);
                Color[] shirtData = new Color[shirtTexture.Bounds.Width * shirtTexture.Bounds.Height];
                shirtTexture.GetData(shirtData);
                int index = shirtIndex * 8 / 128 * 32 * shirtTexture.Bounds.Width + shirtIndex * 8 % 128 + shirtTexture.Width * 4;
                // If no sleeves should be drawn, no need to update the custom sleeve texture
                if (!who.ShirtHasSleeves() || index >= shirtData.Length)
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(ApplySleeveColor_Prefix)}:\n{ex}", LogLevel.Error);
                return true;
            }
        }

        /// <summary>
        /// Transpiler instructions to patch the ApplySleeveColor method in FarmerRenderer.cs.
        /// Calls a custom method to overwrite the skinColors texture path string and its index 
        /// if a custom override color is currently active
        /// </summary>
        /// <remarks>
        /// Implementing the texture change here to preserve the original values in the Farmer's skin variables
        /// to revert back to when the buff ends
        /// </remarks>
        /// <param name="instructions">The IL instructions</param>
        /// <param name="il">The IL generator</param>
        /// <returns>The patched IL instructions</returns> 
        internal static IEnumerable<CodeInstruction> ApplySleeveColor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            try
            {
                // Call a custom method to overwrite the skinColors texture string and index (skin_index) 
                // if custom override color is currently active
                // Find the first instance where a string (ldstr) is loaded to the stack 
                var matcher = new CodeMatcher(code, il);
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldstr) // Load the skinColors texture string
                ).ThrowIfNotMatch("Could not find proper entry point for ApplySleeveColor_Transpiler in FarmerRenderer");
                // Advance to pass the string as a parameter
                matcher.Advance(1);

                // Call a method to overwrite the skinColors texture string
                matcher.InsertAndAdvance(
                    // Load the additional parameter for the custom skin color method
                    new CodeInstruction(OpCodes.Ldarg_0), // Loads the FarmerRenderer instance
                    // Insert a call to the custom skin color method
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), nameof(ParseCustomFields.getCustomColorsTexturePathFromConfig)))
                );

                // Find the first instance skin_index is loaded to the stack
                matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_S), // Load skin_index 
                    new CodeMatch(OpCodes.Ldc_I4_0) // Load 0 to the stack
                ).ThrowIfNotMatch("Could not find proper entry point for ApplySleeveColor_Transpiler for skin index in FarmerRenderer");

                // Call a method to overwrite skin_index
                matcher.InsertAndAdvance(
                    // Load the additional parameters for the custom skin index method
                    new CodeInstruction(OpCodes.Ldarg_0), // Loads the FarmerRenderer instance
                    new CodeInstruction(OpCodes.Ldloca_S, 7), // Loads the address of the skin_index local variable (the index of the colors in the colors texture to use)
                    // Insert a call to the custom skin index method
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), nameof(ParseCustomFields.getCustomColorsIndexFromConfig)))
                );
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(ApplySleeveColor_Transpiler)}:\n{ex}", LogLevel.Error);
                return code;
            }
        }

        /// <summary>
        /// Set of matcher instructions to identify the calls to the draw method for face an eyes and replace the load for
        /// the overrideColor arg with a call to a custom get color method
        /// </summary>
        /// <remarks>This same set of instructions is called four times, so refactored for code reusabilty</remarks>
        /// <param name="matcher">CodeMatcher to add instructions to</param>
        /// <param name="errorIdentifier">Identifier to add to the error message if the entry point is not found</param>
        private static void PatchOverrideColorForFaceAndEyes(ref CodeMatcher matcher, string errorIdentifier)
        {
            
            // Find where overrideColor is loaded for the first call to b.draw to draw the face and eyes
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_S, null, "overrideColor"), // Load overrideColor arg
                new CodeMatch(OpCodes.Ldc_R4), // Load a float to the stack
                new CodeMatch(OpCodes.Ldarg_S, null, "origin"), // Load origin arg
                new CodeMatch(OpCodes.Ldc_R4) // Load a float to the stack
            ).ThrowIfNotMatch($"Could not find proper entry point for b.draw for {errorIdentifier} in draw_Transpiler in FarmerRenderer");

            PatchInstructionsForFarmerCustomColorMethod(ref matcher);
        }

        /// <summary>
        /// Insert a set of instructions into the matcher to modify the load for the default color (overrideColor, Color.White, etc.) 
        /// with a call to a custom get color method
        /// </summary>
        /// <remarks>This same set of instructions is called multiple times, so refactored for code reusabilty</remarks>
        /// <param name="matcher">CodeMatcher to add instructions to</param>
        private static void PatchInstructionsForFarmerCustomColorMethod(ref CodeMatcher matcher)
        {
            // Load the Farmer who arg as a parameter for the custom get color method
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_S, 12) 
            );
            // Advance over the load for default color (overrideColor, Color.White, etc.) to use as parameter for the custom get color method
            matcher.Advance(1);
            // Add the call to the custom get color method
            matcher.Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParseCustomFields), nameof(ParseCustomFields.getCustomColorFromFarmerModData)))
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Does not include the initial load of the FarmerRenderer instance as that is/can be used to steal flags</remarks>
        /// <param name="matcher">CodeMatcher to add instructions to</param>
        private static void PatchInstructionsForCustomDrawMethod(ref CodeMatcher matcher)
        {
            // Load the parameters for the custom draw method
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_S, 12), // Loads the Farmer who arg
                new CodeInstruction(OpCodes.Ldarg_1), // Loads the SpriteBatch arg
                new CodeInstruction(OpCodes.Ldarg_S, 5), // Loads the position arg
                new CodeInstruction(OpCodes.Ldarg_S, 6), // Loads the origin arg
                    // Load positionOffset 
                new CodeInstruction(OpCodes.Ldarg_0), // Loads the FarmerRenderer instance
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FarmerRenderer), "positionOffset")), // Loads the positionOffset field for the FarmerRenderer instance
                    // Load positionOffset end
                new CodeInstruction(OpCodes.Ldarg_S, 4), // Loads the sourceRect arg
                new CodeInstruction(OpCodes.Ldarg_S, 9), // Loads the overrideColor arg
                new CodeInstruction(OpCodes.Ldarg_S, 10), // Loads the rotation arg
                new CodeInstruction(OpCodes.Ldarg_S, 11), // Loads the scale arg
                new CodeInstruction(OpCodes.Ldarg_2), // Loads the animationFrame arg
                    // Load layerDepth: FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Base)
                new CodeInstruction(OpCodes.Ldarg_S, 7) // Loads the layerDepth arg as parameter to pass to GetLayerDepth
            );
        }

        /// <summary>
        /// Set of matcher instructions to identify the calls to the draw method for eyes and add a call to
        /// a custom draw method to draw the whites of the eyes w/o the prismatic effect
        /// </summary>
        /// <remarks>This same set of instructions is called twice, so refactored for code reusabilty</remarks>
        /// <param name="matcher">CodeMatcher to add instructions to</param>
        /// <param name="isSwimming">Identifies if the draw for eyes is for the farmer swimming. The additional offset applied to the position differs depending on the draw.</param>
        private static void AddDrawForEyes(ref CodeMatcher matcher, bool isSwimming)
        {
            string errorIdentifier = $"draw eyes {(isSwimming ? 1 : 2)}";
            // Find where overrideColor is loaded for the first call to b.draw to draw the face and eyes
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Callvirt) // Call b.draw for eyes
            ).ThrowIfNotMatch($"Could not find proper entry point for b.draw for {errorIdentifier} in draw_Transpiler in FarmerRenderer");

            // Skip over the draw for eyes to add the draw for whites of eyes after
            matcher.Advance(1);
            // Load the parameters for the custom draw method
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_S, 12), // Loads the Farmer who arg
                new CodeInstruction(OpCodes.Ldarg_1), // Loads the SpriteBatch arg
                new CodeInstruction(OpCodes.Ldarg_S, 5), // Loads the position arg
                new CodeInstruction(OpCodes.Ldarg_S, 6), // Loads the origin arg
                                                         // Load positionOffset 
                new CodeInstruction(OpCodes.Ldarg_0), // Loads the FarmerRenderer instance
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FarmerRenderer), "positionOffset")), // Loads the positionOffset field for the FarmerRenderer instance
                                                                                                                // Load positionOffset end
                new CodeInstruction(OpCodes.Ldarg_3), // Loads the currentFrame arg
                new CodeInstruction(OpCodes.Ldarg_S, 9), // Loads the overrideColor arg
                new CodeInstruction(OpCodes.Ldarg_S, 11), // Loads the scale arg
                                                          // Load layerDepth: FarmerRenderer.GetLayerDepth(layerDepth, FarmerSpriteLayers.Eyes)
                new CodeInstruction(OpCodes.Ldarg_S, 7), // Loads the layerDepth arg as parameter to pass to GetLayerDepth
                new CodeInstruction(OpCodes.Ldc_I4_5), // Loads 5 onto the stack as the parameter FarmerSpriteLayers layer arg of GetLayerDepth. FarmerSpriteLayers is an enum, where Eyes is at index 5.
                new CodeInstruction(OpCodes.Ldc_I4_0), // Loads 0 onto the stack to pass false to the optional bool arg of GetLayerDepth
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FarmerRenderer), nameof(FarmerRenderer.GetLayerDepth))) // Call FarmerRenderer.GetLayerDepth
            );

            if (isSwimming)
            {
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldc_I4_0), // Loads 0 onto the stack for the x_adjustment (calculated in the custom draw method)
                    new CodeInstruction(OpCodes.Ldc_I4_1) // Loads 1 onto the stack to pass true to the bool arg of the custom draw method. This affects the additional offset to draw position.
                );
            }
            else
            {
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, 7), // Loads the local variable x_adjustment
                    new CodeInstruction(OpCodes.Ldc_I4_0) // Loads 0 onto the stack to pass false to the bool arg of the custom draw method. This affects the additional offset to draw position.
                );

            }
            
            matcher.InsertAndAdvance(
                // Insert a call to the custom draw sleeves method
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomDrawUtilities), nameof(CustomDrawUtilities.DrawEyeWhites)))
            );
        }
    }
}