using Verse;
using HarmonyLib;
using System.Reflection;

namespace WintersWrathHolidayCheer
{
    [StaticConstructorOnStartup]
    public class WintersWrathMod : Mod
    {
        public const string HARMONY_ID = "yourname.winterswrath";
        public const string MOD_NAME = "Winter's Wrath & Holiday Cheer";
        public const string VERSION = "1.0.0";

        public WintersWrathMod(ModContentPack content) : base(content)
        {
            var harmony = new HarmonyLib.Harmony(HARMONY_ID); 
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message($"[{MOD_NAME}] v{VERSION} loaded successfully!");
        }
    }
}