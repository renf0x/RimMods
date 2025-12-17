using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RuneRim
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("r40x.runerim");
            harmony.PatchAll();
        }
    }

    // Патч для переопределения проверки конфликтов между рунами
    [HarmonyPatch(typeof(ApparelUtility), "CanWearTogether")]
    public static class ApparelUtility_CanWearTogether_Patch
    {
        public static void Postfix(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
        {
            // Если уже есть конфликт, проверяем, не являются ли оба предмета атакующими рунами
            if (!__result)
            {
                var slot1Def = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("RuneSlot_Offensive1");
                var slot2Def = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("RuneSlot_Offensive2");

                if (slot1Def == null || slot2Def == null) return;

                // Проверяем, есть ли у предметов CompProperties_Rune (это руны)
                bool AIsOffensiveRune = false;
                bool BIsOffensiveRune = false;

                // Проверяем A
                var ARuneComp = A.comps?.FirstOrDefault(c => c is CompProperties_Rune);
                if (ARuneComp != null)
                {
                    // Это руна - проверяем, является ли она атакующей (имеет хотя бы один из слотов)
                    AIsOffensiveRune = (A.apparel?.layers != null) && 
                        (A.apparel.layers.Contains(slot1Def) || A.apparel.layers.Contains(slot2Def));
                }

                // Проверяем B
                var BRuneComp = B.comps?.FirstOrDefault(c => c is CompProperties_Rune);
                if (BRuneComp != null)
                {
                    BIsOffensiveRune = (B.apparel?.layers != null) && 
                        (B.apparel.layers.Contains(slot1Def) || B.apparel.layers.Contains(slot2Def));
                }

                // Если оба предмета — атакующие руны, разрешаем носить вместе
                if (AIsOffensiveRune && BIsOffensiveRune)
                {
                    __result = true;
                }
            }
        }
    }

    // КРИТИЧЕСКИЙ ПАТЧ: Предотвращаем снятие рун при надевании новой
    [HarmonyPatch(typeof(JobDriver_Wear), "TryUnequipSomething")]
    public static class JobDriver_Wear_TryUnequipSomething_Patch
    {
        public static bool Prefix(JobDriver_Wear __instance)
        {
            Pawn pawn = __instance.pawn;
            Apparel newApparel = (Apparel)__instance.job.targetA.Thing;

            // Проверяем, это руна с двумя слотами?
            var compRune = newApparel.TryGetComp<CompRune>();
            if (compRune == null) return true; // Не руна — стандартная логика

            var slot1Def = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("RuneSlot_Offensive1");
            var slot2Def = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("RuneSlot_Offensive2");

            if (slot1Def == null || slot2Def == null) return true;

            // Используем оригинальные слоты, если они сохранены
            var layersToCheck = compRune.originalLayers != null && compRune.originalLayers.Count > 0 
                ? compRune.originalLayers 
                : newApparel.def.apparel.layers;

            if (layersToCheck.Contains(slot1Def) && layersToCheck.Contains(slot2Def))
            {
                // Это атакующая руна — проверяем, есть ли свободный слот
                bool slot1Occupied = pawn.apparel.WornApparel.Any(a => 
                    a.def.apparel.layers.Contains(slot1Def));
                
                bool slot2Occupied = pawn.apparel.WornApparel.Any(a => 
                    a.def.apparel.layers.Contains(slot2Def));

                // Если хотя бы один слот свободен, НЕ снимаем ничего
                if (!slot1Occupied || !slot2Occupied)
                {
                    return false; // Отменяем оригинальный метод (ничего не снимаем)
                }
            }

            return true; // Стандартная логика для остальных случаев
        }
    }

    // Патч для динамического назначения слота при надевании руны
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Wear")]
    public static class Pawn_ApparelTracker_Wear_Patch
    {
        public static void Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            var compRune = newApparel.TryGetComp<CompRune>();
            if (compRune == null) return;

            var slot1Def = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("RuneSlot_Offensive1");
            var slot2Def = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("RuneSlot_Offensive2");

            if (slot1Def == null || slot2Def == null) return;

            // ВАЖНО: Сохраняем оригинальные layers перед ПЕРВЫМ изменением
            if (compRune.originalLayers == null || compRune.originalLayers.Count == 0)
            {
                compRune.originalLayers = new List<ApparelLayerDef>(newApparel.def.apparel.layers);
            }

            var originalLayers = compRune.originalLayers;

            // Проверяем, это атакующая руна с двумя слотами?
            if (originalLayers.Contains(slot1Def) && originalLayers.Contains(slot2Def))
            {
                // Определяем, какой слот свободен
                bool slot1Occupied = __instance.WornApparel.Any(a => 
                    a.def.apparel.layers.Contains(slot1Def) && a != newApparel);
                
                bool slot2Occupied = __instance.WornApparel.Any(a => 
                    a.def.apparel.layers.Contains(slot2Def) && a != newApparel);

                // Определяем, какой слот назначить
                List<ApparelLayerDef> tempLayers = null;
                
                if (!slot1Occupied)
                {
                    tempLayers = new List<ApparelLayerDef> { slot1Def };
                }
                else if (!slot2Occupied)
                {
                    tempLayers = new List<ApparelLayerDef> { slot2Def };
                }
                else
                {
                    // Оба слота заняты
                    return;
                }

                // Создаём НОВЫЙ список для layers
                if (tempLayers != null)
                {
                    newApparel.def.apparel.layers = tempLayers;
                }
            }
        }
    }

    // Патч для восстановления оригинальных слоёв при снятии руны
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Remove")]
    public static class Pawn_ApparelTracker_Remove_Patch
    {
        public static void Postfix(Apparel ap)
        {
            var compRune = ap.TryGetComp<CompRune>();
            if (compRune != null && compRune.originalLayers != null && compRune.originalLayers.Count > 0)
            {
                // Восстанавливаем оригинальные layers
                ap.def.apparel.layers = new List<ApparelLayerDef>(compRune.originalLayers);
            }
        }
    }

    // Патч для добавления способностей при надевании руны
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelAdded")]
    public static class Pawn_ApparelTracker_Notify_ApparelAdded_Patch
    {
        public static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            if (__instance.pawn == null || __instance.pawn.abilities == null) return;

            var compRune = apparel.TryGetComp<CompRune>();
            if (compRune != null && compRune.Props.abilityDef != null)
            {
                // Проверяем, нет ли уже этой способности
                if (!__instance.pawn.abilities.abilities.Any(a => a.def == compRune.Props.abilityDef))
                {
                    __instance.pawn.abilities.GainAbility(compRune.Props.abilityDef);
                }
            }
        }
    }

    // Патч для удаления способностей при снятии руны
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelRemoved")]
    public static class Pawn_ApparelTracker_Notify_ApparelRemoved_Patch
    {
        public static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            if (__instance.pawn == null || __instance.pawn.abilities == null) return;

            var compRune = apparel.TryGetComp<CompRune>();
            if (compRune != null && compRune.Props.abilityDef != null)
            {
                // Проверяем, есть ли другие руны с такой же способностью
                bool hasOtherRuneWithSameAbility = __instance.WornApparel.Any(a =>
                {
                    var otherComp = a.TryGetComp<CompRune>();
                    return otherComp != null && otherComp.Props.abilityDef == compRune.Props.abilityDef && a != apparel;
                });

                if (!hasOtherRuneWithSameAbility)
                {
                    var ability = __instance.pawn.abilities.GetAbility(compRune.Props.abilityDef);
                    if (ability != null)
                    {
                        __instance.pawn.abilities.RemoveAbility(compRune.Props.abilityDef);
                    }
                }
            }
        }
    }

    // Патч для активации способностей и потребления зарядов
    [HarmonyPatch(typeof(Ability))]
    public static class Ability_Activate_Patch
    {
        static MethodBase TargetMethod()
        {
            var method = typeof(Ability).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "Activate" && m.GetParameters().Length == 2);
    
            if (method == null)
            {
                Log.Error("RuneRim: Failed to patch Ability.Activate! Game version may be incompatible.");
                return null;
            }

            return method;
        }

        public static void Postfix(Ability __instance, LocalTargetInfo target)
        {
            if (__instance?.pawn == null) return;

            Pawn caster = __instance.pawn;

            // СПЕЦИАЛЬНАЯ ЛОГИКА ДЛЯ FIRE SHIELD - активация без toggle
            if (__instance.def.defName == "RuneRim_FireShield")
            {
                HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed("RuneRim_FireShieldHediff", false);
                
                if (hediffDef != null)
                {
                    // Проверяем, активен ли уже щит
                    Hediff existingShield = caster.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                    
                    if (existingShield != null)
                    {
                        // Щит уже активен - НЕ ДЕЛАЕМ НИЧЕГО, просто возвращаемся
                        return; // НЕ ПОТРЕБЛЯЕМ ЗАРЯД - выходим ДО общей логики
                    }
                    else
                    {
                        // Щит не активен - ВКЛЮЧАЕМ
                        Hediff hediff = HediffMaker.MakeHediff(hediffDef, caster);
                        caster.health.AddHediff(hediff);
                        
                        // Визуальный эффект активации (3 огненные вспышки)
                        if (caster.Spawned && caster.Map != null)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                FleckMaker.ThrowFireGlow(caster.Position.ToVector3Shifted(), caster.Map, 1.5f);
                            }                            
                        }
                    }
                }
                else
                {
                    Log.Error("RuneRim: HediffDef 'RuneRim_FireShieldHediff' NOT FOUND!");
                    return; // Не потребляем заряд при ошибке
                }
            }

            // ОБЩАЯ ЛОГИКА - потребление заряда руны для ВСЕХ способностей
            if (caster.apparel != null)
            {
                foreach (var apparel in caster.apparel.WornApparel)
                {
                    var compRune = apparel.TryGetComp<CompRune>();
                    if (compRune != null && compRune.Props.abilityDef == __instance.def)
                    {
                        compRune.ConsumeUse();
                        break; // Потребляем только из одной руны
                    }
                }
            }
        }
    }

    // ИСПРАВЛЕННЫЙ ПАТЧ: Дроп осколков рун при добыче (ИСПОЛЬЗУЕТ PREFIX!)
    [HarmonyPatch(typeof(Mineable), "DestroyMined")]
    public static class Mineable_DestroyMined_Patch
    {
        public static void Prefix(Mineable __instance, Pawn pawn)
        {
            if (__instance == null || !__instance.Spawned) return;
        
            // ВАЖНО: Сохраняем Map и Position ДО уничтожения
            Map map = __instance.Map;
            IntVec3 position = __instance.Position;
        
            // 20% шанс выпадения осколков рун
            if (Rand.Chance(0.2f))
            {
                ThingDef fragmentDef = DefDatabase<ThingDef>.GetNamedSilentFail("RuneRim_RuneFragment");
            
                if (fragmentDef != null)
                {
                    int fragmentCount = Rand.RangeInclusive(1, 2);
                    Thing fragments = ThingMaker.MakeThing(fragmentDef);
                    fragments.stackCount = fragmentCount;
                
                    if (map != null && position.IsValid)
                    {
                        GenPlace.TryPlaceThing(fragments, position, map, ThingPlaceMode.Near);
                    }
                }
            }
        }
    }
}
