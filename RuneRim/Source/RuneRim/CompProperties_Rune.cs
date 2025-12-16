using RimWorld;
using Verse;

namespace RuneRim
{
    public class CompProperties_Rune : CompProperties
    {
        public AbilityDef abilityDef;
        public int baseUses = 4; // Базовое количество использований для Normal качества

        public CompProperties_Rune()
        {
            compClass = typeof(CompRune);
        }
    }
}
