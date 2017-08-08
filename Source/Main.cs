using System;
using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using WorkTab;

namespace ForceDoJob
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = HarmonyInstance.Create("com.forcedojob.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("ForceDoJob: Adding Harmony Prefix to FloatMenuMakerMap.ChoicesAtFor");
            Log.Message("ForceDoJob: Adding Harmony Postfix to FloatMenuMakerMap.ChoicesAtFor");
        }
    }

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    static class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        static Patch_FloatMenuMakerMap_ChoicesAtFor()
        {
            pfi = typeof(Pawn_WorkSettings).GetField("priorities", BindingFlags.Instance | BindingFlags.NonPublic);

            try
            {
                //Need a wrapper method/lambda to be able to catch the TypeLoadException when WorkTab isn't present
                //All credits for this black magic go to Zhentar (https://github.com/Zhentar/ZhentarTweaks/blob/3da46894781c851b2260a8ef7933665a5e03436b/Source/LetterStackDetour.cs)
                ((Action) (() =>
                {
                    // access worktab to force an error
                    var test = MainTabWindow_WorkTab.Instance;

                    PrioritySetter = (pawn, def, priority) => pawn.SetPriority(def as WorkGiverDef, priority, null);
                    PriorityGetter = (pawn, def) => pawn.GetPriority(def as WorkGiverDef, -1);
                    DisabledGetter = (pawn, def) => pawn.story.WorkTypeIsDisabled((def as WorkGiverDef).workType);
                }))();

                Log.Message("ForceDoJob: WorkTab detected, using compatibility mode.");
                useWorkTab = true;
            }
            catch (TypeLoadException)
            {
                PrioritySetter = (pawn, def, priority) => SetPriority(pawn.workSettings, def as WorkTypeDef, priority);
                PriorityGetter = (pawn, def) => pawn.workSettings.GetPriority(def as WorkTypeDef);
                DisabledGetter = (pawn, def) => pawn.story.WorkTypeIsDisabled(def as WorkTypeDef);

#if DEBUG
                Log.Message("ForceDoJob: using vanilla priority getter/setter.");
#endif
            }
        }

        private static FieldInfo pfi;
        private static bool useWorkTab;

        private static void SetPriority(Pawn_WorkSettings workSettings, WorkTypeDef workTypeDef, int priority)
        {
            ((DefMap<WorkTypeDef, int>)pfi.GetValue(workSettings))[workTypeDef] = priority;
        }

        internal static Action<Pawn, Def, int> PrioritySetter;
        internal static Func<Pawn, Def, int> PriorityGetter;
        internal static Func<Pawn, Def, bool> DisabledGetter;

        static void Prefix(Pawn pawn, ref List<Pair<Def, int>> __state)
        {
            __state = new List<Pair<Def, int>>();

            // we have to use workgivers for worktab compatibility, worktypes suffice for vanilla.
            List<Def> defs = useWorkTab
                ? DefDatabase<WorkGiverDef>.AllDefsListForReading.Cast<Def>().ToList()
                : DefDatabase<WorkTypeDef>.AllDefsListForReading.Cast<Def>().ToList();

            foreach (var def in defs)
            {
                if (!DisabledGetter(pawn, def))
                {
                    __state.Add(new Pair<Def, int>(def, PriorityGetter(pawn, def)));
                    PrioritySetter(pawn, def, 3);
                }
            }
        }
        static void Postfix(Pawn pawn, ref List<Pair<Def, int>> __state)
        {
            foreach (Pair<Def, int> p in __state)
            {
                PrioritySetter(pawn, p.First, p.Second);
            }
            __state.Clear();
        }
    }
}
