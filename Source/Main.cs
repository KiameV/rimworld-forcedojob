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
        static void Prefix(Pawn pawn, ref List<Pair<WorkTypeDef, int>> __state)
        {
            __state = new List<Pair<WorkTypeDef, int>>();
            foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.story.WorkTypeIsDisabled(def))
                {
                    __state.Add(new Pair<WorkTypeDef, int> (def, pawn.workSettings.GetPriority(def)));
                    pawn.workSettings.SetPriority(def, 3);

                }
            }
        }
        static void Postfix(Pawn pawn, ref List<Pair<WorkTypeDef, int>> __state)
        {
            foreach (Pair<WorkTypeDef, int> p in __state)
            {
                pawn.workSettings.SetPriority(p.First, p.Second);
            }
            __state.Clear();
        }
    }
}