using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RuneRim
{
    public class CompProperties_AbilityLightningStrike : CompProperties_AbilityEffect
    {
        public float damage = 20f;
        public float stunChance = 0.05f;
        public int stunDurationTicks = 180;

        public CompProperties_AbilityLightningStrike()
        {
            compClass = typeof(CompAbilityEffect_LightningStrike);
        }
    }

    public class CompAbilityEffect_LightningStrike : CompAbilityEffect
    {
        public new CompProperties_AbilityLightningStrike Props => (CompProperties_AbilityLightningStrike)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Thing targetThing = target.Thing;

            if (targetThing == null || !targetThing.Spawned) return;

            // Визуальный эффект молнии
            Map map = targetThing.Map;
            IntVec3 position = targetThing.Position;

            // Молния сверху вниз
            FleckMaker.Static(position, map, FleckDefOf.LightningGlow, 3f);
            FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), map, 2f);
            
            // Звук грома
            SoundDefOf.Thunder_OnMap.PlayOneShot(new TargetInfo(position, map));

            // Наносим урон
            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Burn, // Электрический урон (можно заменить на кастомный DamageDef)
                Props.damage,
                0f,
                -1f,
                caster,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown
            );

            targetThing.TakeDamage(damageInfo);

            // Шанс оглушения
            if (targetThing is Pawn targetPawn && !targetPawn.Dead)
            {
                if (Rand.Chance(Props.stunChance))
                {
                    // Накладываем stun через hediff
                    HediffDef stunHediff = HediffDefOf.PsychicShock; // Используем ванильный stun
                    Hediff hediff = targetPawn.health.hediffSet.GetFirstHediffOfDef(stunHediff);
                    
                    if (hediff == null)
                    {
                        hediff = HediffMaker.MakeHediff(stunHediff, targetPawn);
                        targetPawn.health.AddHediff(hediff);
                    }

                    // Устанавливаем длительность
                    HediffComp_Disappears comp = hediff.TryGetComp<HediffComp_Disappears>();
                    if (comp != null)
                    {
                        comp.ticksToDisappear = Props.stunDurationTicks;
                    }

                    Messages.Message(
                        $"{targetPawn.LabelShort} stunned by lightning!",
                        targetPawn,
                        MessageTypeDefOf.NegativeEvent
                    );

                    Log.Message($"RuneRim: {targetPawn.LabelShort} stunned for {Props.stunDurationTicks} ticks");
                }
            }

            // Дополнительные визуальные эффекты вокруг цели
            for (int i = 0; i < 4; i++)
            {
                IntVec3 sparkPos = position + GenRadial.RadialPattern[Rand.Range(1, 8)];
                if (sparkPos.InBounds(map))
                {
                    FleckMaker.ThrowMicroSparks(sparkPos.ToVector3Shifted(), map);
                }
            }

            Log.Message($"RuneRim: Lightning strike dealt {Props.damage} damage to {targetThing.LabelShort}");
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Thing == null)
            {
                if (throwMessages)
                {
                    Messages.Message("Must target a valid pawn or object.", MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return base.Valid(target, throwMessages);
        }
    }
}
