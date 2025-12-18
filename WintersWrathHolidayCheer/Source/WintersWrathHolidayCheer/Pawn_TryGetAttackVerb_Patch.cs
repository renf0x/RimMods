using HarmonyLib;
using Verse;
using RimWorld;

namespace WintersWrathHolidayCheer.HarmonyPatches
{
    // Патч 1: Заставляем манхантеров предпочитать дальнюю атаку
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), "TryGetMeleeVerb")]
    public static class Pawn_MeleeVerbs_TryGetMeleeVerb_Patch
    {
        static bool Prefix(Pawn_MeleeVerbs __instance, Thing target, ref Verb __result)
        {
            Pawn pawn = __instance.Pawn;
            
            // Проверяем наличие нашего компонента
            var comp = pawn?.TryGetComp<CompPreferRangedAttack>();
            if (comp == null || target == null)
                return true;

            // Ищем дальнобойный verb
            Verb rangedVerb = null;
            var verbs = pawn.verbTracker?.AllVerbs;
            if (verbs != null)
            {
                foreach (var verb in verbs)
                {
                    if (verb.verbProps.range > 1.5f && verb.Available())
                    {
                        rangedVerb = verb;
                        break;
                    }
                }
            }

            // Если есть дальняя атака и цель в радиусе
            if (rangedVerb != null)
            {
                float distance = (pawn.Position - target.Position).LengthHorizontal;
                
                if (distance >= rangedVerb.verbProps.minRange && distance <= rangedVerb.verbProps.range)
                {
                    // Возвращаем null для melee, чтобы AI использовал ranged
                    __result = null;
                    return false;
                }
            }

            return true;
        }
    }

    // Патч 2: Переопределяем TryGetAttackVerb для приоритета дальней атаки
    [HarmonyPatch(typeof(Pawn), "TryGetAttackVerb")]
    public static class Pawn_TryGetAttackVerb_Patch
    {
        static void Postfix(Pawn __instance, Thing target, bool allowManualCastWeapons, ref Verb __result)
        {
            // Если уже выбран verb, проверяем нужно ли переключиться на дальний
            var comp = __instance?.TryGetComp<CompPreferRangedAttack>();
            if (comp == null || target == null)
                return;

            // Если выбрана ближняя атака, но доступна дальняя - переключаемся
            if (__result != null && __result.verbProps.range <= 1.5f)
            {
                var verbs = __instance.verbTracker?.AllVerbs;
                if (verbs != null)
                {
                    foreach (var verb in verbs)
                    {
                        if (verb.verbProps.range > 1.5f && verb.Available())
                        {
                            float distance = (__instance.Position - target.Position).LengthHorizontal;
                            
                            if (distance >= verb.verbProps.minRange && distance <= verb.verbProps.range)
                            {
                                __result = verb;
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
