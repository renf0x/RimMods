using Verse;
using HarmonyLib;

namespace WintersWrathHolidayCheer
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new HarmonyLib.Harmony("com.winterswrath.holidaycheer");
            harmony.PatchAll();
            Log.Message("[Winter's Wrath] Harmony patches applied successfully.");
        }
    }
}
