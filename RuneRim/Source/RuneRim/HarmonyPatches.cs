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

    // ОБЪЕДИНЕННЫЙ ПАТЧ С ЛОГАМИ: потребление заряда + применение Fire Shield
    [HarmonyPatch(typeof(Ability))]
    public static class Ability_Activate_Patch
    {
        // Указываем конкретную сигнатуру метода
        static MethodBase TargetMethod()
        {
            var method = typeof(Ability).GetMethod("Activate", 
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }, 
                null);
            
            Log.Message($"RuneRim: TargetMethod Ability.Activate found: {method != null}");
            return method;
        }

        public static void Postfix(Ability __instance, LocalTargetInfo target)
        {
            Log.Message($"RuneRim: Ability_Activate_Patch called for ability: {__instance?.def?.defName ?? "NULL"}");
            
            if (__instance.pawn == null)
            {
                Log.Warning("RuneRim: Ability pawn is null!");
                return;
            }

            Log.Message($"RuneRim: Ability activated by {__instance.pawn.Name.ToStringShort}");

            // 1. СПЕЦИАЛЬНАЯ ЛОГИКА ДЛЯ FIRE SHIELD - применяем Hediff на себя
            if (__instance.def.defName == "RuneRim_FireShield")
            {
                Log.Message("RuneRim: Fire Shield detected! Applying hediff...");
                
                Pawn caster = __instance.pawn;
                HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed("RuneRim_FireShieldHediff", false);
                
                if (hediffDef != null)
                {
                    Log.Message("RuneRim: HediffDef found, creating and adding hediff...");
                    Hediff hediff = HediffMaker.MakeHediff(hediffDef, caster);
                    caster.health.AddHediff(hediff);
                    
                    Log.Message($"RuneRim: Fire Shield hediff successfully added to {caster.Name.ToStringShort}!");
                    
                    Messages.Message(
                        $"{caster.Name.ToStringShort} activated fire shield!",
                        caster,
                        MessageTypeDefOf.PositiveEvent
                    );
                    
                    // Визуальный эффект
                    if (caster.Spawned && caster.Map != null)
                    {
                        FleckMaker.ThrowFireGlow(caster.Position.ToVector3Shifted(), caster.Map, 1.5f);
                    }
                }
                else
                {
                    Log.Error("RuneRim: HediffDef 'RuneRim_FireShieldHediff' NOT FOUND!");
                }
            }

            // 2. ОБЩАЯ ЛОГИКА - потребление заряда руны для ВСЕХ способностей
            if (__instance.pawn.apparel != null)
            {
                foreach (var apparel in __instance.pawn.apparel.WornApparel)
                {
                    var compRune = apparel.TryGetComp<CompRune>();
                    if (compRune != null && compRune.Props.abilityDef == __instance.def)
                    {
                        Log.Message($"RuneRim: Consuming rune use for {__instance.def.defName}");
                        compRune.ConsumeUse();
                        break; // Потребляем только из одной руны
                    }
                }
            }
        }
    }
}
