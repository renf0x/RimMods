using HarmonyLib;
using RimWorld;
using Verse;

namespace RuneRim.Patches
{
    // Дроп осколков рун при добыче камня
    [HarmonyPatch(typeof(Mineable), "Destroy")]
    public static class Mineable_Destroy_Patch
    {
        public static void Prefix(Mineable __instance, DestroyMode mode)
        {
            // Проверяем, что блок уничтожается добычей
            if (mode != DestroyMode.KillFinalize) return;
            
            // Проверяем, что блок был на карте
            if (!__instance.Spawned || __instance.Map == null) return;
            
            // 10% шанс выпадения осколков рун
            if (Rand.Chance(0.1f))
            {
                ThingDef fragmentDef = DefDatabase<ThingDef>.GetNamedSilentFail("RuneRim_RuneFragment");
                
                if (fragmentDef != null)
                {
                    // 1-2 осколка
                    int fragmentCount = Rand.RangeInclusive(1, 2);
                    
                    Thing fragments = ThingMaker.MakeThing(fragmentDef);
                    fragments.stackCount = fragmentCount;
                    
                    IntVec3 position = __instance.Position;
                    Map map = __instance.Map;
                    
                    if (GenPlace.TryPlaceThing(fragments, position, map, ThingPlaceMode.Near))
                    {
                        Messages.Message(
                            $"Found {fragmentCount}x rune fragment{(fragmentCount > 1 ? "s" : "")} while mining {__instance.def.label}!",
                            new TargetInfo(position, map),
                            MessageTypeDefOf.PositiveEvent
                        );
                        
                        Log.Message($"RuneRim: Mined {fragmentCount} rune fragment(s) from {__instance.def.label}");
                    }
                }
                else
                {
                    Log.Error("RuneRim: RuneRim_RuneFragment ThingDef not found!");
                }
            }
        }
    }
}
