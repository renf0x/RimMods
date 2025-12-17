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

        private int CalculateMaxUses()
        {
            if (parent.TryGetQuality(out QualityCategory quality))
            {
                int maxUses;
                switch (quality)
                {
                    case QualityCategory.Awful:
                        maxUses = 1;
                        break;
                    case QualityCategory.Poor:
                        maxUses = 2;
                        break;
                    case QualityCategory.Normal:
                        maxUses = Props.baseUses;
                        break;
                    case QualityCategory.Good:
                        maxUses = Props.baseUses + 2;
                        break;
                    case QualityCategory.Excellent:
                        maxUses = Props.baseUses + 4;
                        break;
                    case QualityCategory.Masterwork:
                        maxUses = Props.baseUses + 6;
                        break;
                    case QualityCategory.Legendary:
                        maxUses = Props.baseUses + 8;
                        break;
                    default:
                        maxUses = Props.baseUses;
                        break;
                }
                
                return maxUses;
            }
            
            return Props.baseUses;
        }

        public void ConsumeUse()
        {
            // ВАЖНО: Проверяем, что remainingUses инициализирован
            if (remainingUses < 0)
            {
                remainingUses = CalculateMaxUses();
            }

            // ВРЕМЕННОЕ ЛОГИРОВАНИЕ для отладки
            Log.Warning($"RuneRim DEBUG: {parent.Label} consuming charge. Before: {remainingUses}, Max: {CalculateMaxUses()}");
            
            remainingUses--;
            
            Log.Warning($"RuneRim DEBUG: {parent.Label} after consumption: {remainingUses}");
            
            if (remainingUses <= 0)
            {
                string runeLabel = parent.Label;
                
                // Определяем позицию для дропа осколков
                IntVec3 dropPosition = IntVec3.Invalid;
                Map map = null;
                
                // Проверяем, где находится руна
                if (parent.Spawned)
                {
                    dropPosition = parent.Position;
                    map = parent.Map;
                }
                else if (parent.ParentHolder is Pawn_ApparelTracker apparelTracker)
                {
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
            string qualityStr = parent.TryGetQuality(out QualityCategory quality) ? quality.ToString() : "No Quality";
            return $"Remaining uses: {RemainingUses}/{CalculateMaxUses()} (Quality: {qualityStr})";
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
