using HarmonyLib;
using RimWorld;
using Verse;

namespace RuneRim
{
    // Патч для вероятностного дропа с Runethorn
    [HarmonyPatch(typeof(Plant), "YieldNow")]
    public static class Plant_YieldNow_Patch
    {
        public static void Postfix(Plant __instance, ref int __result)
        {
            if (__instance.def.defName != "Plant_Runethorn") return;
            if (__instance.Growth < 1f) return;

            IntVec3 position = __instance.Position;
            Map map = __instance.Map;

            // 10% шанс - RAW RUNETHORN (1-3 шт) - бутоны для крафта рун
            if (Rand.Chance(0.10f))
            {
                int budCount = Rand.RangeInclusive(1, 3);
                ThingDef budDef = DefDatabase<ThingDef>.GetNamedSilentFail("RuneRim_RawRunethorn");
                
                if (budDef != null)
                {
                    Thing buds = ThingMaker.MakeThing(budDef);
                    buds.stackCount = budCount;
                    GenPlace.TryPlaceThing(buds, position, map, ThingPlaceMode.Near);

                    Messages.Message(
                        $"Harvested {budCount}x runethorn bud{(budCount > 1 ? "s" : "")}!",
                        new TargetInfo(position, map),
                        MessageTypeDefOf.PositiveEvent,
                        false
                    );
                }
            }
            else
            {
                // 90% шанс - VEIL POWDER (1-5 шт) - побочка
                int powderCount = Rand.RangeInclusive(1, 5);
                ThingDef powderDef = DefDatabase<ThingDef>.GetNamedSilentFail("RuneRim_VeilPowder");
                
                if (powderDef != null)
                {
                    Thing powder = ThingMaker.MakeThing(powderDef);
                    powder.stackCount = powderCount;
                    GenPlace.TryPlaceThing(powder, position, map, ThingPlaceMode.Near);
                }
            }

            // Обнуляем стандартный дроп
            __result = 0;
        }
    }
}
