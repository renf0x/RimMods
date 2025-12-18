using Verse;
using HarmonyLib;

namespace WintersWrathHolidayCheer
{
    [StaticConstructorOnStartup]
    public static class Main
    {
        static Main()
        {
            var harmony = new HarmonyLib.Harmony("com.winterswrath.holidaycheer");
            harmony.PatchAll();
            Log.Message("[Winter's Wrath] Harmony patches applied. EvilSnowman ranged preference enabled.");
        }
    }
}
