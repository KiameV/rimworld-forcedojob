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
        private static FieldInfo pfi;
        private static bool useWorkTab;

        private static void SetPriority(Pawn_WorkSettings workSettings, WorkTypeDef workTypeDef, int priority)
        {
            ((DefMap<WorkTypeDef, int>)pfi.GetValue(workSettings))[workTypeDef] = priority;
        }

        internal static Action<Pawn, Def, int> VanillaPrioritySetter = null;
        internal static Action<Pawn, Def, int, int> WorkTabPrioritySetter = null;
        internal static Func<Pawn, Def, int> VanillaPriorityGetter = null;
        internal static Func<Pawn, Def, int, int> WorkTabPriorityGetter = null;
        internal static Func<Pawn, Def, bool> DisabledGetter = null;

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

                    WorkTabPrioritySetter = (pawn, def, priority, hour) => pawn.SetPriority(def as WorkGiverDef, priority, hour);
                    WorkTabPriorityGetter = (pawn, def, hour) => pawn.GetPriority(def as WorkGiverDef, hour);
                    DisabledGetter = (pawn, def) => pawn.story.WorkTypeIsDisabled((def as WorkGiverDef).workType);
                }))();

                useWorkTab = true;

                Log.Warning("ForceDoJob: WorkTab detected, using compatibility mode.");
            }
            catch (TypeLoadException)
            {
                VanillaPrioritySetter = (pawn, def, priority) => SetPriority(pawn.workSettings, def as WorkTypeDef, priority);
                VanillaPriorityGetter = (pawn, def) => pawn.workSettings.GetPriority(def as WorkTypeDef);
                DisabledGetter = (pawn, def) => pawn.story.WorkTypeIsDisabled(def as WorkTypeDef);

                useWorkTab = false;

                Log.Warning("ForceDoJob: using vanilla priority getter/setter.");
            }
        }

        static void Prefix(Pawn pawn, ref List<Pair<Def, int>> __state)
        {
            __state = new List<Pair<Def, int>>();

            // we have to use workgivers for worktab compatibility, worktypes suffice for vanilla.
            List<Def> defs = useWorkTab
                ? DefDatabase<WorkGiverDef>.AllDefsListForReading.Cast<Def>().ToList()
                : DefDatabase<WorkTypeDef>.AllDefsListForReading.Cast<Def>().ToList();
            int hour = GenLocalDate.HourOfDay(pawn);
            foreach (var def in defs)
            {
                if (!DisabledGetter(pawn, def))
                {
                    if (useWorkTab)
                    {
                        if (PriorityManager.Get.ShowScheduler)
                        {
                            __state.Add(new Pair<Def, int>(def, WorkTabPriorityGetter(pawn, def, hour)));
                            WorkTabPrioritySetter(pawn, def, 3, hour);
                        }
                    }
                    else
                    {
                        __state.Add(new Pair<Def, int>(def, VanillaPriorityGetter(pawn, def)));
                        VanillaPrioritySetter(pawn, def, 3);
                    }
                }
            }
        }
        static void Postfix(Pawn pawn, ref List<Pair<Def, int>> __state)
        {
            int hour = GenLocalDate.HourOfDay(pawn);
            foreach (Pair<Def, int> p in __state)
            {
                if (useWorkTab)
                {
                    WorkTabPrioritySetter(pawn, p.First, p.Second, hour);
                }
                else
                {
                    VanillaPrioritySetter(pawn, p.First, p.Second);
                }
            }
            __state.Clear();
        }
    }
}
