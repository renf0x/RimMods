using RimWorld;
using Verse;
using WintersWrathHolidayCheer.Hediffs;

namespace WintersWrathHolidayCheer.Projectiles
{
    public class Projectile_Snowball : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing, blockedByShield);

            if (hitThing is Pawn pawn && !blockedByShield)
            {
                ApplySnowballEffect(pawn);
            }
        }

        private void ApplySnowballEffect(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;

            Hediff_Freezing freezingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamed("FreezingEffect")
            ) as Hediff_Freezing;

            if (freezingHediff == null)
            {
                freezingHediff = (Hediff_Freezing)HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamed("FreezingEffect"), pawn
                );
                pawn.health.AddHediff(freezingHediff);
            }

            freezingHediff.AddSnowballHit();
        }
    }
}