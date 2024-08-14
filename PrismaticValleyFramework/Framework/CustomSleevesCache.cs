using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using PrismaticValleyFramework.Models;

namespace PrismaticValleyFramework.Framework
{
    public class CustomSleevesCache
    {
        private Dictionary<long, CustomSleevesTextures> _cache;

        public CustomSleevesCache()
        {
            _cache = new Dictionary<long, CustomSleevesTextures>();
        }

        public Texture2D? GetFarmerSleevesTexture(long UniqueMultiplayerID)
        {
            // Get the CustomSleevesTexture from the cache dictionary if it exists
            if (_cache.TryGetValue(UniqueMultiplayerID, out CustomSleevesTextures? customSleevesTexture))
            {
                // Return the SleevesTexture if the base texture is still valid
                if (customSleevesTexture.BaseSleevesTexture.TryGetTarget(out _))
                {
                    return customSleevesTexture.SleevesTexture;
                }
                // Else refresh the SleevesTexture
                else
                {
                    // Dispose of the old SleevesTexture before overwriting the entry in the cache dictionary 
                    customSleevesTexture.SleevesTexture.Dispose();
                    return CreateNewSleevesTexture(UniqueMultiplayerID);
                }
            }
            // Else add a new entry to the dictionary
            else {
                return CreateNewSleevesTexture(UniqueMultiplayerID);
            }
        }

        public void UpdateFarmerSleevesTexture(Farmer farmer, string texture_name)
        {
            // Get the CustomSleevesTexture from the cache dictionary if it exists
            if (_cache.TryGetValue(farmer.UniqueMultiplayerID, out CustomSleevesTextures? customSleevesTexture))
            {
                // Update the SleevesTexture if the base texture is still valid
                if (customSleevesTexture.BaseSleevesTexture.TryGetTarget(out _))
                {
                    // Get the FarmerRenderer instance for the farmer
                    FarmerRenderer farmerRenderer = farmer.FarmerRenderer;

                    // Get the color data from the sleeves texture
                    Color[] pixel_data = new Color[customSleevesTexture.SleevesTexture.Width * customSleevesTexture.SleevesTexture.Height];
                    customSleevesTexture.SleevesTexture.GetData(pixel_data);
                    
                    // Apply changes to the color data for the custom texture
                    farmerRenderer.ApplySleeveColor(texture_name, pixel_data, farmer);
                    _cache[farmer.UniqueMultiplayerID].SleevesTexture.SetData(pixel_data);
                }
                // Else refresh the SleevesTexture
                else
                {
                    // Dispose of the old SleevesTexture before overwriting the entry in the cache dictionary 
                    customSleevesTexture.SleevesTexture.Dispose();
                    CreateNewSleevesTexture(farmer.UniqueMultiplayerID);
                }
            }
            // Else add a new entry to the dictionary
            else {
                CreateNewSleevesTexture(farmer.UniqueMultiplayerID);
            }
        }

        public void ValidateBaseTexture(long UniqueMultiplayerID)
        {
            if (_cache.TryGetValue(UniqueMultiplayerID, out CustomSleevesTextures? customSleevesTexture))
            {
                // Dispose the texture and remove the dictionary entry if the base texture is no longer valid
                if (!customSleevesTexture.BaseSleevesTexture.TryGetTarget(out _))
                {
                    customSleevesTexture.SleevesTexture.Dispose();
                    _cache.Remove(UniqueMultiplayerID);
                }
            }
        }

        private Texture2D? CreateNewSleevesTexture(long UniqueMultiplayerID)
        {
            // Get the Farmer instance from the UniqueMultiplayerId
            if (!(Game1.getAllFarmers().FirstOrDefault(x => x.UniqueMultiplayerID == UniqueMultiplayerID) is { } farmer))
                return null; // Invalid multiplayer ID
            FarmerRenderer farmerRenderer = farmer.FarmerRenderer;

            // Load the base texture data
            string texture_name = $"{ModEntry.Instance.ModManifest.UniqueID}\\{(farmer.IsMale ? "farmer_sleeves" : "farmer_girl_sleeves")}";
            Texture2D baseSleevesTexture = Game1.content.Load<Texture2D>(texture_name);
            // Copy the base texture data into a new texture
            Texture2D sleevesTexture = new(Game1.graphics.GraphicsDevice, baseSleevesTexture.Width, baseSleevesTexture.Height);
            Color[] pixel_data = new Color[baseSleevesTexture.Width * baseSleevesTexture.Height];
            baseSleevesTexture.GetData(pixel_data, 0, pixel_data.Length);
            
            // Modify the data with the farmer's currently equipped shirt
            // Add the custom texture's pixel indices to FarmerRenderer.recolorOffsets if they have not been added
            if (!FarmerRenderer.recolorOffsets.ContainsKey(texture_name))
                ParseCustomFields.AddTextureToRecolorOffsets(farmerRenderer, texture_name);
            // Set the color data for the custom texture
            farmerRenderer.ApplySleeveColor(texture_name, pixel_data, farmer);
            
            // Set the modified data to the custom sleeves texture
            sleevesTexture.SetData(pixel_data);
            
            // Add to cache
            _cache[UniqueMultiplayerID] = new CustomSleevesTextures
                {
                    SleevesTexture = sleevesTexture,
                    BaseSleevesTexture = new WeakReference<Texture2D>(baseSleevesTexture)
                };
            return sleevesTexture;
        }
    }
}