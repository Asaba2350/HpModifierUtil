using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HpModifierUtil
{
    public class HpModifierInitializer : ModInitializer
    {
        public override void OnInitializeMod()
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
