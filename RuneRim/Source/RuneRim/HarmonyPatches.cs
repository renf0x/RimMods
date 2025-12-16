using HarmonyLib;
using RimWorld;
using Verse;
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

                // Проверяем, имеют ли оба предмета оба атакующих слота
                bool AHasBothSlots = A.apparel?.layers != null && 
                    A.apparel.layers.Contains(slot1Def) && 
                    A.apparel.layers.Contains(slot2Def);

                bool BHasBothSlots = B.apparel?.layers != null && 
                    B.apparel.layers.Contains(slot1Def) && 
                    B.apparel.layers.Contains(slot2Def);

                // Если оба предмета — атакующие руны с двумя слотами, разрешаем носить вместе
                if (AHasBothSlots && BHasBothSlots)
                {
                    __result = true;
                    Log.Message("RuneRim: Allowing two offensive runes to be worn together");
                }
            }
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

            // Проверяем, это атакующая руна с двумя слотами?
            var layers = newApparel.def.apparel.layers;
            var slot1Def = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("RuneSlot_Offensive1");
            var slot2Def = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("RuneSlot_Offensive2");

            if (slot1Def == null || slot2Def == null) return;

            if (layers.Contains(slot1Def) && layers.Contains(slot2Def))
            {
                // Определяем, какой слот свободен
                bool slot1Occupied = __instance.WornApparel.Any(a => 
                    a.def.apparel.layers.Contains(slot1Def) && a != newApparel);
                
                bool slot2Occupied = __instance.WornApparel.Any(a => 
                    a.def.apparel.layers.Contains(slot2Def) && a != newApparel);

                // ВАЖНО: Сохраняем оригинальные layers перед изменением
                if (compRune.originalLayers == null || compRune.originalLayers.Count == 0)
                {
                    compRune.originalLayers = new List<ApparelLayerDef>(layers);
                }

                // Определяем, какой слот назначить
                List<ApparelLayerDef> tempLayers = null;
                
                if (!slot1Occupied)
                {
                    tempLayers = new List<ApparelLayerDef> { slot1Def };
                    Log.Message($"RuneRim: Assigning {newApparel.Label} to Offensive Slot 1");
                }
                else if (!slot2Occupied)
                {
                    tempLayers = new List<ApparelLayerDef> { slot2Def };
                    Log.Message($"RuneRim: Assigning {newApparel.Label} to Offensive Slot 2");
                }
                else
                {
                    // Оба слота заняты
                    Log.Warning($"RuneRim: Both offensive slots occupied, cannot wear {newApparel.Label}");
                    return;
                }

                // Создаём НОВЫЙ список для layers (не перезаписываем оригинальный Def!)
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
                Log.Message($"RuneRim: Restored original layers for {ap.Label}");
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
                    Log.Message($"RuneRim: Added ability {compRune.Props.abilityDef.defName} to {__instance.pawn.Name}");
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
                        Log.Message($"RuneRim: Removed ability {compRune.Props.abilityDef.defName} from {__instance.pawn.Name}");
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

            Log.Message("RuneRim: Successfully patched Ability.Activate");
            return method;
        }

        public static void Postfix(Ability __instance, LocalTargetInfo target)
        {
            if (__instance?.pawn == null)
            {
                Log.Warning("RuneRim: Ability or pawn is null!");
                return;
            }

            Pawn caster = __instance.pawn;

            // СПЕЦИАЛЬНАЯ ЛОГИКА ДЛЯ FIRE SHIELD - Toggle механика
            if (__instance.def.defName == "RuneRim_FireShield")
            {
                HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed("RuneRim_FireShieldHediff", false);
                
                if (hediffDef != null)
                {
                    // Проверяем, активен ли уже щит
                    Hediff existingShield = caster.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                    
                    if (existingShield != null)
                    {
                        // Щит активен - ОТКЛЮЧАЕМ
                        caster.health.RemoveHediff(existingShield);
                        
                        Messages.Message(
                            $"{caster.Name.ToStringShort} deactivated fire shield.",
                            caster,
                            MessageTypeDefOf.NeutralEvent
                        );
                        
                        // Визуальный эффект отключения
                        if (caster.Spawned && caster.Map != null)
                        {
                            FleckMaker.ThrowSmoke(caster.Position.ToVector3Shifted(), caster.Map, 1f);
                        }
                        
                        Log.Message($"RuneRim: Fire Shield deactivated for {caster.Name.ToStringShort}");
                    }
                    else
                    {
                        // Щит не активен - ВКЛЮЧАЕМ
                        Hediff hediff = HediffMaker.MakeHediff(hediffDef, caster);
                        caster.health.AddHediff(hediff);
                        
                        Messages.Message(
                            $"{caster.Name.ToStringShort} activated fire shield!",
                            caster,
                            MessageTypeDefOf.PositiveEvent
                        );
                        
                        // Визуальный эффект активации (3 огненные вспышки)
                        if (caster.Spawned && caster.Map != null)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                FleckMaker.ThrowFireGlow(caster.Position.ToVector3Shifted(), caster.Map, 1.5f);
                            }
                        }
                        
                        Log.Message($"RuneRim: Fire Shield activated for {caster.Name.ToStringShort}");
                    }
                }
                else
                {
                    Log.Error("RuneRim: HediffDef 'RuneRim_FireShieldHediff' NOT FOUND!");
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
                        Log.Message($"RuneRim: Consumed rune charge for {__instance.def.defName}");
                        break; // Потребляем только из одной руны
                    }
                }
            }
        }
    }
}
