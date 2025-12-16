using RimWorld;
using Verse;
using System.Collections.Generic;

namespace RuneRim
{
    public class CompRune : ThingComp
    {
        private int remainingUses = -1;
        private Pawn lastWearer;
        public List<ApparelLayerDef> originalLayers;

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
            Scribe_Collections.Look(ref originalLayers, "originalLayers", LookMode.Def);
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
            
            Log.Warning($"RuneRim: {parent.Label} has no quality! Using base charges: {Props.baseUses}");
            return Props.baseUses;
        }

        public void ConsumeUse()
        {
            remainingUses--;
            
            if (remainingUses <= 0)
            {
                string runeLabel = parent.Label;
                
                // Определяем позицию для дропа осколков
                IntVec3 dropPosition = IntVec3.Invalid;
                Map map = null;
                
                // Проверяем, где находится руна
                if (parent.Spawned)
                {
                    // Руна на карте (не надета)
                    dropPosition = parent.Position;
                    map = parent.Map;
                }
                else if (parent.ParentHolder is Pawn_ApparelTracker apparelTracker)
                {
                    // Руна надета на колонисте
                    Pawn wearer = apparelTracker.pawn;
                    if (wearer != null && wearer.Spawned)
                    {
                        dropPosition = wearer.Position;
                        map = wearer.Map;
                    }
                }
                
                Messages.Message(
                    $"{runeLabel} has crumbled to dust after exhausting its power.", 
                    MessageTypeDefOf.NeutralEvent
                );

                // 100% шанс выпадения для теста (потом смени на 0.05f)
                if (Rand.Chance(1f))
                {
                    ThingDef fragmentDef = DefDatabase<ThingDef>.GetNamedSilentFail("RuneRim_RuneFragment");
                    
                    if (fragmentDef != null)
                    {
                        int fragmentCount = Rand.RangeInclusive(1, 3);
                        
                        Thing fragments = ThingMaker.MakeThing(fragmentDef);
                        fragments.stackCount = fragmentCount;
                        
                        // Дроп осколков
                        if (map != null && dropPosition.IsValid)
                        {
                            GenPlace.TryPlaceThing(fragments, dropPosition, map, ThingPlaceMode.Near);
                            
                            Messages.Message(
                                $"{fragmentCount}x rune fragment{(fragmentCount > 1 ? "s" : "")} dropped from {runeLabel}!",
                                new TargetInfo(dropPosition, map),
                                MessageTypeDefOf.PositiveEvent
                            );
                            
                            Log.Message($"RuneRim: Dropped {fragmentCount} rune fragment(s) at {dropPosition}");
                        }
                        else
                        {
                            Log.Warning($"RuneRim: Cannot drop fragments - invalid position or map. Position: {dropPosition}, Map: {map}");
                        }
                    }
                    else
                    {
                        Log.Error("RuneRim: RuneRim_RuneFragment ThingDef not found!");
                    }
                }
                
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
