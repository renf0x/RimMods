using Verse;

namespace WintersWrathHolidayCheer
{
    public class CompProperties_PreferRangedAttack : CompProperties
    {
        public CompProperties_PreferRangedAttack()
        {
            compClass = typeof(CompPreferRangedAttack);
        }
    }

    public class CompPreferRangedAttack : ThingComp
    {
        public CompProperties_PreferRangedAttack Props => (CompProperties_PreferRangedAttack)props;
        
        // Этот компонент теперь просто маркер, логика в Harmony патче
    }
}
