using HarmonyLib;
using System;

namespace HpModifierUtil
{
    public class HpModifierInitializer : DeviceOfHermes.Boot.HermesInitializer
    {
        public void OnInitMod()
        {
            ApplyHarmonyPatch();
        }

        public static void ApplyHarmonyPatch()
        {
            Harmony harmony = new("HpModifierUtil");
            foreach (Type type in typeof(PatchClass).GetNestedTypes(AccessTools.all))
            {
                harmony.CreateClassProcessor(type).Patch();
            }
        }
    }
}