using RimWorld;
using Verse;
using System.Collections.Generic;

namespace RuneRim
{
    public class CompRune : ThingComp
    {
        private int remainingUses = -1;
        private Pawn lastWearer;

        public CompProperties_Rune Props => (CompProperties_Rune)props;

        public int RemainingUses
        {
            get
            {
                if (remainingUses < 0)
                {
                    remainingUses = CalculateMaxUses();
                }
                return remainingUses;
            }
            set => remainingUses = value;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remainingUses, "remainingUses", -1);
            Scribe_References.Look(ref lastWearer, "lastWearer");
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            remainingUses = CalculateMaxUses();
        }

        private int CalculateMaxUses()
        {
            if (parent.TryGetQuality(out QualityCategory quality))
            {
                switch (quality)
                {
                    case QualityCategory.Awful:
                        return 1;
                    case QualityCategory.Poor:
                        return 2;
                    case QualityCategory.Normal:
                        return Props.baseUses;
                    case QualityCategory.Good:
                        return Props.baseUses + 2;
                    case QualityCategory.Excellent:
                        return Props.baseUses + 4;
                    case QualityCategory.Masterwork:
                        return Props.baseUses + 6;
                    case QualityCategory.Legendary:
                        return Props.baseUses + 8;
                    default:
                        return Props.baseUses;
                }
            }
            return Props.baseUses;
        }

        public void ConsumeUse()
        {
            remainingUses--;
            if (remainingUses <= 0)
            {
                Messages.Message($"{parent.Label} has crumbled to dust after exhausting its power.", 
                    MessageTypeDefOf.NeutralEvent);
                parent.Destroy(DestroyMode.Vanish);
            }
        }

        public override string CompInspectStringExtra()
        {
            return $"Remaining uses: {RemainingUses}/{CalculateMaxUses()}";
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            yield return new StatDrawEntry(
                StatCategoryDefOf.Apparel,
                "Remaining Uses",
                RemainingUses.ToString(),
                "How many times this rune can be used before it crumbles.",
                5000
            );
        }
    }
}
