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
        public static Pawn ChoicesForPawn = null;

        static Main()
        {
            var harmony = HarmonyInstance.Create("com.forcedojob.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("ForceDoJob: Adding Harmony Prefix to FloatMenuMakerMap.ChoicesAtFor - not blocking");
            Log.Message("ForceDoJob: Adding Harmony Postfix to FloatMenuMakerMap.ChoicesAtFor - must not be blocked otherwise all work assignments will be set to 3");
            Log.Message("ForceDoJob: Adding Harmony Prefix[HarmonyBefore(\"fluffy.worktab\")] to Pawn_WorkSettings.GetPriority - will block in the case of user right click for pawn actions");
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    static class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        static void Prefix(Pawn pawn, ref List<Pair<WorkTypeDef, int>> __state)
        {
            Main.ChoicesForPawn = pawn;
        }

        static void Postfix(Pawn pawn, ref List<Pair<WorkTypeDef, int>> __state)
        {
            Main.ChoicesForPawn = null;
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), "GetPriority")]
    static class Patch_Pawn_WorkSettings_GetPriority
    {
        [HarmonyBefore("fluffy.worktab")]
        static bool Prefix(ref int __result, WorkTypeDef w)
        {
            if (Main.ChoicesForPawn != null &&
                !Main.ChoicesForPawn.story.WorkTypeIsDisabled(w))
            {
                __result = 3;
                return false;
            }
            return true;
        }
    }

    /* Old Version
    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    static class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        private static FieldInfo pfi = null;
        private static void SetPriority(Pawn_WorkSettings workSettings, WorkTypeDef workTypeDef, int priority)
        {
            if (pfi == null)
            {
                pfi = typeof(Pawn_WorkSettings).GetField("priorities", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            ((DefMap<WorkTypeDef, int>)pfi.GetValue(workSettings))[workTypeDef] = priority;
        }

        static void Prefix(Pawn pawn, ref List<Pair<WorkTypeDef, int>> __state)
        {
            __state = new List<Pair<WorkTypeDef, int>>();
            foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.story.WorkTypeIsDisabled(def))
                {
                    __state.Add(new Pair<WorkTypeDef, int>(def, pawn.workSettings.GetPriority(def)));
                    SetPriority(pawn.workSettings, def, 3);
                }
            }
        }

        static void Postfix(Pawn pawn, ref List<Pair<WorkTypeDef, int>> __state)
        {
            foreach (Pair<WorkTypeDef, int> p in __state)
            {
                SetPriority(pawn.workSettings, p.First, p.Second);
            }
            __state.Clear();
        }
    }*/
}