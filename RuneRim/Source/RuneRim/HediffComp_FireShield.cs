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
        
        // ✅ ДОБАВЛЕНО: Визуализация щита
        private Material shieldMat;
        private int lastDamageTick = -9999;
        private Vector3 impactAngle = Vector3.zero;
        private const float ShieldSize = 2.2f;
        private const float JitterDuration = 8f;
        
        public HediffCompProperties_FireShield Props => (HediffCompProperties_FireShield)props;

        private HediffComp_Disappears disappearsComp;
        
        // ДОБАВЛЕНО: Материал для ауры
        private Material ShieldMat
        {
            get
            {
                if (shieldMat == null)
                {
                    shieldMat = MaterialPool.MatFrom(
                        "Effects/Shield/FireShieldBubble",
                        ShaderDatabase.MoteGlow, // ✅ Вместо Transparent
                        Color.white
                    );
                }
                return shieldMat;
            }
        }
        
        public override void CompPostMake()
        {
            base.CompPostMake();
            disappearsComp = parent.TryGetComp<HediffComp_Disappears>();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            
            // ✅ ДОБАВЛЕНО: Отрисовка щита каждый кадр
            if (Pawn != null && Pawn.Spawned && Pawn.Map != null)
            {
                DrawShield();
            }
            
            if (Pawn.IsHashIntervalTick(15))
            {
                TryBurnNearbyEnemies();
            }
        }

        // ✅ НОВЫЙ МЕТОД: Отрисовка огненной ауры
        private void DrawShield()
        {
            Vector3 drawPos = Pawn.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            
            float currentSize = ShieldSize;
            
            // Эффект дрожания при получении урона
            int ticksSinceDamage = Find.TickManager.TicksGame - lastDamageTick;
            if (ticksSinceDamage < JitterDuration)
            {
                float jitterAmount = (JitterDuration - ticksSinceDamage) / JitterDuration * 0.05f;
                drawPos += impactAngle * jitterAmount;
                currentSize -= jitterAmount;
            }
            
            // Пульсация щита (для живого эффекта)
            float pulse = Mathf.Sin(Time.time * 2f) * 0.05f;
            currentSize += pulse;
            
            // ДОБАВЛЕНО: Вращение по часовой стрелке
            float rotationSpeed = 30f; // Скорость вращения (градусов в секунду)
            float angle = (Time.time * rotationSpeed) % 360f; // Угол вращения
    
            // Создание матрицы трансформации с вращением
            Vector3 scale = new Vector3(currentSize, 1f, currentSize);
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up); // ✅ Вращение вокруг оси Y
            Matrix4x4 matrix = default;
            matrix.SetTRS(drawPos, rotation, scale); // ✅ Применяем вращение
    
            // Отрисовка меша
            Graphics.DrawMesh(MeshPool.plane10, matrix, ShieldMat, 0);
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

            // ✅ УЛУЧШЕНО: Эффект дрожания щита при контакте
            lastDamageTick = Find.TickManager.TicksGame;
            impactAngle = (enemy.Position - Pawn.Position).ToVector3();

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
                        return $"{secondsLeft:F1}s";
                    }
                }
                return null;
            }
        }

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
                        return $"Time remaining: {secondsLeft:F1} seconds\nBurn chance: 5%\nRadius: 3 tiles";
                    }
                }
                return "Burn chance: 5%\nRadius: 3 tiles";
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastBurnTick, "lastBurnTick", 0);
            Scribe_Values.Look(ref lastDamageTick, "lastDamageTick", -9999);
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
