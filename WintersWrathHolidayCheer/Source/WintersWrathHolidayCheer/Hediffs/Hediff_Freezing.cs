using RimWorld;
using Verse;

namespace WintersWrathHolidayCheer.Hediffs
{
    public class Hediff_Freezing : HediffWithComps
    {
        private int snowballHits = 0;
        private const int HITS_TO_FREEZE = 10;
        private const float FREEZE_DAMAGE = 25f;

        // Публичное свойство для доступа извне
        public int SnowballHits => snowballHits;

        public void AddSnowballHit()
        {
            snowballHits++;

            if (snowballHits >= HITS_TO_FREEZE)
            {
                TriggerFreeze();
            }
            else
            {
                Severity = (float)snowballHits / HITS_TO_FREEZE;
            }
        }

        private void TriggerFreeze()
        {
            Pawn pawn = this.pawn;
            if (pawn == null || pawn.Dead) return;

            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.Frostbite,
                FREEZE_DAMAGE,
                0f,
                -1f,
                null,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown
            );

            pawn.TakeDamage(dinfo);

            Hediff hypothermia = HediffMaker.MakeHediff(HediffDefOf.Hypothermia, pawn);
            hypothermia.Severity = 0.5f;
            pawn.health.AddHediff(hypothermia);

            snowballHits = 0;
            Severity = 0f;

            Messages.Message(
                pawn.LabelShort + " has been frozen solid!",
                pawn,
                MessageTypeDefOf.NegativeEvent
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref snowballHits, "snowballHits", 0);
        }

        public override string LabelInBrackets => $"{snowballHits}/{HITS_TO_FREEZE} hits";
    }
}
