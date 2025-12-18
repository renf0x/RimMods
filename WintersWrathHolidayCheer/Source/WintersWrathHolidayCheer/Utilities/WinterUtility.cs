using RimWorld;
using Verse;

namespace WintersWrathHolidayCheer.Utilities
{
    public static class WinterUtility
    {
        public static bool IsWinterBiome(BiomeDef biome)
        {
            if (biome == null) return false;
            
            return biome == BiomeDefOf.Tundra || 
                   biome == BiomeDefOf.IceSheet ||
                   biome == BiomeDefOf.SeaIce ||
                   biome.defName.ToLower().Contains("cold") ||
                   biome.defName.ToLower().Contains("ice") ||
                   biome.defName.ToLower().Contains("tundra") ||
                   biome.defName.ToLower().Contains("boreal");
        }

        public static bool IsWinterSeason(Map map)
        {
            if (map == null) return false;
            
            Twelfth twelfth = GenDate.Twelfth(Find.TickManager.TicksAbs, map.Tile);
            
            // Decembary это зимний квадрум (дни 46-60)
            // Он соответствует Twelfth.Tenth, Twelfth.Eleventh, Twelfth.Twelfth
            return twelfth == Twelfth.Tenth ||      // 10-й twelfth (день ~46-50)
                   twelfth == Twelfth.Eleventh ||   // 11-й twelfth (день ~51-55)
                   twelfth == Twelfth.Twelfth;      // 12-й twelfth (день ~56-60)
        }

        public static bool IsColdEnough(Map map)
        {
            if (map == null) return false;
            
            float avgTemp = map.mapTemperature.OutdoorTemp;
            return avgTemp < 0f;
        }

        public static bool CanSpawnWinterContent(Map map)
        {
            return IsWinterBiome(map.Biome) || IsWinterSeason(map) || IsColdEnough(map);
        }
    }
}
