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
            var harmony = new Harmony("yourname.runerim");
            harmony.PatchAll();
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
