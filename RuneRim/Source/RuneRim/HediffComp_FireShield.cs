using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RuneRim
{
    public class HediffComp_FireShield : HediffComp
    {
        private int lastBurnTick = 0;
        private const int BurnCooldownTicks = 60;
        
        public HediffCompProperties_FireShield Props => (HediffCompProperties_FireShield)props;

        // Получаем компонент исчезновения для отображения таймера
        private HediffComp_Disappears disappearsComp;
        
        public override void CompPostMake()
        {
            base.CompPostMake();
            disappearsComp = parent.TryGetComp<HediffComp_Disappears>();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            
            if (Pawn.IsHashIntervalTick(15))
            {
                TryBurnNearbyEnemies();
            }
        }

        private void TryBurnNearbyEnemies()
        {
            if (!Pawn.Spawned || Pawn.Map == null) return;
            if (Find.TickManager.TicksGame - lastBurnTick < BurnCooldownTicks) return;

            var enemies = GenRadial.RadialDistinctThingsAround(Pawn.Position, Pawn.Map, 3f, true)
                .OfType<Pawn>()
                .Where(p => p.Faction != null && p.Faction.HostileTo(Pawn.Faction) && !p.Dead);

            bool anyBurnApplied = false;

            foreach (var enemy in enemies)
            {
                if (Rand.Chance(0.05f))
                {
                    ApplyBurnToEnemy(enemy);
                    anyBurnApplied = true;
                }
            }

            if (anyBurnApplied)
            {
                lastBurnTick = Find.TickManager.TicksGame;
            }
        }

        private void ApplyBurnToEnemy(Pawn enemy)
        {
            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.Flame,
                5f,
                0f,
                -1f,
                Pawn,
                null,
                null
            );
            
            enemy.TakeDamage(dinfo);

            FleckMaker.ThrowFireGlow(enemy.Position.ToVector3Shifted(), enemy.Map, 0.8f);
            SoundDefOf.Pawn_Melee_Punch_HitPawn.PlayOneShot(new TargetInfo(enemy.Position, enemy.Map));
            
            if (PawnUtility.ShouldSendNotificationAbout(enemy))
            {
                Messages.Message(
                    $"{enemy.LabelShort} burned by {Pawn.LabelShort}'s fire shield!",
                    enemy,
                    MessageTypeDefOf.SilentInput
                );
            }
        }

        // ОТОБРАЖЕНИЕ ТАЙМЕРА В UI
        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (disappearsComp != null)
                {
                    int ticksLeft = disappearsComp.ticksToDisappear;
                    if (ticksLeft > 0)
                    {
                        float secondsLeft = ticksLeft / 60f;
                        return $"{secondsLeft:F1}s"; // Формат: "25.3s"
                    }
                }
                return null;
            }
        }

        // ДЕТАЛЬНАЯ ИНФОРМАЦИЯ ПРИ НАВЕДЕНИИ
        public override string CompTipStringExtra
        {
            get
            {
                if (disappearsComp != null)
                {
                    int ticksLeft = disappearsComp.ticksToDisappear;
                    if (ticksLeft > 0)
                    {
                        float secondsLeft = ticksLeft / 60f;
                        return $"Time remaining: {secondsLeft:F1} seconds\nBurn chance: 15%\nRadius: 3 tiles";
                    }
                }
                return "Burn chance: 15%\nRadius: 3 tiles";
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastBurnTick, "lastBurnTick", 0);
        }
    }

    public class HediffCompProperties_FireShield : HediffCompProperties
    {
        public HediffCompProperties_FireShield()
        {
            compClass = typeof(HediffComp_FireShield);
        }
    }
}
