using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace WintersWrathHolidayCheer.Harmony
{
    [HarmonyPatch(typeof(Pawn), "TryGetAttackVerb")]
    public static class Pawn_TryGetAttackVerb_Patch
    {
        static bool Prefix(Pawn __instance, Thing target, ref Verb __result)
        {
            // Проверяем наличие нашего компонента
            var comp = __instance.TryGetComp<CompPreferRangedAttack>();
            if (comp == null)
                return true; // Пропускаем, если компонента нет

            // Проверяем, должны ли мы предпочесть дальнюю атаку
            if (!comp.ShouldPreferRanged())
                return true; // Используем стандартную логику

            // Ищем дальнобойный verb
            Verb rangedVerb = null;
            if (__instance.equipment?.PrimaryEq?.PrimaryVerb != null && __instance.equipment.PrimaryEq.PrimaryVerb.verbProps.range > 1.5f)
            {
                rangedVerb = __instance.equipment.PrimaryEq.PrimaryVerb;
            }
            else
            {
                // Ищем среди verbs существа
                var verbs = __instance.verbTracker?.AllVerbs;
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
            }

            // Если нашли дальнобойный verb и цель в радиусе действия
            if (rangedVerb != null && target != null)
            {
                float distance = (__instance.Position - target.Position).LengthHorizontal;
                if (distance >= rangedVerb.verbProps.minRange && distance <= rangedVerb.verbProps.range)
                {
                    __result = rangedVerb;
                    return false; // Блокируем оригинальный метод
                }
            }

            return true; // Используем стандартную логику, если дальняя атака недоступна
        }
    }
}
