using Verse;
using RimWorld;

namespace WintersWrathHolidayCheer
{
    public class CompProperties_AutoManhunter : CompProperties
    {
        public int checkIntervalTicks = 60;

        public CompProperties_AutoManhunter()
        {
            compClass = typeof(Comp_AutoManhunter);
        }
    }

    public class Comp_AutoManhunter : ThingComp
    {
        private CompProperties_AutoManhunter Props => (CompProperties_AutoManhunter)props;

        public override void CompTick()
        {
            base.CompTick();

            if (parent.IsHashIntervalTick(Props.checkIntervalTicks))
            {
                Pawn pawn = parent as Pawn;
                if (pawn != null && pawn.Spawned && !pawn.Dead && pawn.Faction == null)
                {
                    // Делаем снеговика манхантером, если он еще не агрессивен
                    if (pawn.mindState != null && !pawn.InMentalState)
                    {
                        pawn.mindState.mentalStateHandler.TryStartMentalState(
                            MentalStateDefOf.ManhunterPermanent
                        );
                    }
                }
            }
        }
    }
}
