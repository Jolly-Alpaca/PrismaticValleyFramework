using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace PrismaticValleyFramework.Framework
{
    static class CustomDrawUtilities
    {
        static readonly float LayerDepthModifier = 1E-06f;

        /// <summary>
        /// Draw boots in a separate draw from the base farmer sprite
        /// </summary>
        /// <param name="__instance">The calling FarmerRenderer instance</param>
        /// <param name="who">The Farmer to draw</param>
        /// <param name="b">The SpriteBatch</param>
        /// <param name="position"></param>
        /// <param name="origin"></param>
        /// <param name="positionOffset"></param>
        /// <param name="sourceRect"></param>
        /// <param name="overrideColor"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        /// <param name="animationFrame"></param>
        /// <param name="layerDepth"></param>        
        public static void DrawCustomBoots(FarmerRenderer __instance, Farmer who, SpriteBatch b, Vector2 position, Vector2 origin, Vector2 positionOffset, Rectangle sourceRect, Color overrideColor, float rotation, float scale, FarmerSprite.AnimationFrame animationFrame, float layerDepth)
        {
            // Return if equipped boots are not custom boots
            if (who.boots.Value is null) return;
            string BootsId = who.boots.Value.ItemId;
            if (!ParseCustomFields.HasCustomColorData(BootsId)) return;
            // Use the custom texture if provided. Otherwise, use the default textures in the Assets folder.
            string BootsTextureTarget = ParseCustomFields.getCustomTextureTargetFromStringDictItem(BootsId) ?? ($"{ModEntry.Instance.ModManifest.UniqueID}\\{(who.IsMale ? "farmer_shoes" : "farmer_girl_shoes")}");
            Texture2D BootsTexture = ModEntry.ModHelper.GameContent.Load<Texture2D>(BootsTextureTarget);
            // Get the custom color and draw the boots
            b.Draw(BootsTexture, position + origin + positionOffset, sourceRect, ParseCustomFields.getCustomColorFromStringDictItemWithColor(BootsId, overrideColor), rotation, origin, 4f * scale, animationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
        }

        public static void DrawCustomFeatures(Farmer who, SpriteBatch b, Vector2 position, Vector2 origin, Vector2 positionOffset, Rectangle sourceRect, Color overrideColor, float rotation, float scale, FarmerSprite.AnimationFrame animationFrame, float layerDepth)
        {
            // Return if farmer does not have custom color buff 
            if (!who.hasBuff($"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff")) return;

            // Get the Farmer's eye whites texture
            string eyesTextureTarget = $"{ModEntry.Instance.ModManifest.UniqueID}\\{(who.IsMale ? "farmer_eyes" : "farmer_girl_eyes")}";
            Texture2D eyesTexture = ModEntry.ModHelper.GameContent.Load<Texture2D>(eyesTextureTarget);

            // Get the custom color and draw the boots
            b.Draw(eyesTexture, position + origin + positionOffset, sourceRect, overrideColor, rotation, origin, 4f * scale, animationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth + LayerDepthModifier);
        }

        public static void DrawCustomSleeves(Farmer who, SpriteBatch b, Vector2 position, Vector2 origin, Vector2 positionOffset, Rectangle sourceRect, Color overrideColor, float rotation, float scale, FarmerSprite.AnimationFrame animationFrame, float layerDepth)
        {
            // Return if farmer does not have custom color buff or the currently equipped shirt doesn't have sleeves
            if (!who.hasBuff($"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff") || !who.ShirtHasSleeves()) return;

            // Get the Farmer's sleeve texture
            if (ModEntry.SleevesTextureCache.GetFarmerSleevesTexture(who.UniqueMultiplayerID) is Texture2D sleevesTexture)
            {
                // Get the custom color and draw the boots
                b.Draw(sleevesTexture, position + origin + positionOffset + who.armOffset, sourceRect, overrideColor, rotation, origin, 4f * scale, animationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth + LayerDepthModifier);
            }
        }

        public static void DrawEyeWhites(Farmer who, SpriteBatch b, Vector2 position, Vector2 origin, Vector2 positionOffset, int currentFrame, Color overrideColor, float scale, float layerDepth, int x_adjustment, bool isSwimming)
        {
            // Return if farmer does not have custom color buff 
            if (!who.hasBuff($"{ModEntry.Instance.ModManifest.UniqueID}.ColorBuff")) return;

            // Get the Farmer's eye whites texture
            string eyesTextureTarget = $"{ModEntry.Instance.ModManifest.UniqueID}\\{(who.IsMale ? "farmer_eyes" : "farmer_girl_eyes")}";
            Texture2D eyesTexture = ModEntry.ModHelper.GameContent.Load<Texture2D>(eyesTextureTarget);

            // Draw the eye whites
            Vector2 eyePositionOffset;
            if (isSwimming) eyePositionOffset = new(FarmerRenderer.featureXOffsetPerFrame[currentFrame] * 4 + 20 + ((who.FacingDirection == 1) ? 12 : ((who.FacingDirection == 3) ? 4 : 0)), FarmerRenderer.featureYOffsetPerFrame[currentFrame] * 4 + 40);
            else eyePositionOffset = new(x_adjustment, FarmerRenderer.featureYOffsetPerFrame[currentFrame] * 4 + ((who.FacingDirection == 1 || who.FacingDirection == 3) ? 40 : 44));
            
            b.Draw(eyesTexture, position + origin + positionOffset + eyePositionOffset, new Rectangle(264 + ((who.FacingDirection == 3) ? 4 : 0), 2 + (who.currentEyes - 1) * 2, (who.FacingDirection == 2) ? 6 : 2, 2), overrideColor, 0f, origin, 4f * scale, SpriteEffects.None, layerDepth + LayerDepthModifier);
        }
    }
}