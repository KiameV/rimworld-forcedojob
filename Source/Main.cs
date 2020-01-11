using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;


namespace ForceDoJob
{
    [StaticConstructorOnStartup]
    class Main
    {
        public static Pawn ChoicesForPawn = null;
        public static bool WithinGetPriority = false;

        static Main()
        {
            var harmony = HarmonyInstance.Create("com.forcedojob.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message(
                "ForceDoJob Harmony Patches:" + Environment.NewLine +
                "  Prefix:" + Environment.NewLine +
                "    FloatMenuMakerMap.ChoicesAtFor  - not blocking" + Environment.NewLine +
                "    Pawn_PlayerSettings.EffectiveAreaRestrictionInPawnCurrentMap { get; } - blocking if set in settings" + Environment.NewLine +
                "    Pawn_WorkSettings.GetPriority - will block in the case of user right click for pawn actions [HarmonyBefore(\"fluffy.worktab\")]" + Environment.NewLine +
                "  Postfix:" + Environment.NewLine +
                "    FloatMenuMakerMap.ChoicesAtFor - must not be blocked otherwise all work assignments will be set to 3");
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    static class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        [HarmonyPriority(Priority.HigherThanNormal)]
        static void Prefix(Pawn pawn)
        {
            Main.ChoicesForPawn = pawn;
            pawn.playerSettings.selfTend = Settings.AllowPawnsToDoAllJobs || !Main.ChoicesForPawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Doctor);
        }

        [HarmonyPriority(Priority.HigherThanNormal)]
        static void Postfix(Pawn pawn)
        {
            Main.ChoicesForPawn = null;
        }
    }

    [HarmonyPatch(typeof(Pawn_PlayerSettings), "get_EffectiveAreaRestrictionInPawnCurrentMap")]
    static class Patch_Pawn_PlayerSettings_EffectiveAreaRestrictionInPawnCurrentMap
    {
        [HarmonyPriority(Priority.HigherThanNormal)]
        static bool Prefix(ref Area __result)
        {
            if (Settings.AllowOutsideAllowedArea && Main.ChoicesForPawn != null)
            {
                __result = null;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), "GetPriority")]
    static class Patch_Pawn_WorkSettings_GetPriority
    {
        [HarmonyBefore("fluffy.worktab")]
        static bool Prefix(ref int __result, WorkTypeDef w)
        {
            if (Main.ChoicesForPawn != null)
            {
                if (Settings.AllowPawnsToDoAllJobs ||
                    !Main.ChoicesForPawn.story.WorkTypeIsDisabled(w))
                {
                    __result = 3;
                    return false;
                }
            }
            Main.WithinGetPriority = true;
            return true;
        }

        static void Postfix(ref int __result, WorkTypeDef w)
        {
            Main.WithinGetPriority = false;
        }
    }
}