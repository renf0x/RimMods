using Verse;
using RimWorld;

namespace WintersWrathHolidayCheer
{
    public class CompProperties_PreferRangedAttack : CompProperties
    {
        public float rangedPreferenceChance = 0.9f; // 90% шанс использовать дальнюю атаку
        
        public CompProperties_PreferRangedAttack()
        {
            compClass = typeof(CompPreferRangedAttack);
        }
    }

    public class CompPreferRangedAttack : ThingComp
    {
        public CompProperties_PreferRangedAttack Props => (CompProperties_PreferRangedAttack)props;

        public bool ShouldPreferRanged()
        {
            return Rand.Value < Props.rangedPreferenceChance;
        }
    }
}
