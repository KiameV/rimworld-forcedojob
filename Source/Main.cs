using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ForceDoJob
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = HarmonyInstance.Create("com.forcedojob.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("Force Do Job: Adding Harmony Prefix to FloatMenuMakerMap.ChoicesAtFor");
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    static class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        static void Prefix(Pawn pawn, ref Dictionary<WorkTypeDef, int> __state)
        {
            __state = new Dictionary<WorkTypeDef, int>();
            foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.story.WorkTypeIsDisabled(def))
                {
                    __state.Add(def, pawn.workSettings.GetPriority(def));
                    pawn.workSettings.SetPriority(def, 3);

                }
            }
        }
        static void Postfix(Pawn pawn, ref Dictionary<WorkTypeDef, int> __state)
        {
            foreach (KeyValuePair<WorkTypeDef, int> kv in __state)
            {
                pawn.workSettings.SetPriority(kv.Key, kv.Value);
            }
            __state.Clear();
        }
    }
}