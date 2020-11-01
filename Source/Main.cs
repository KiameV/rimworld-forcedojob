using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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
        public readonly static Dictionary<Pawn, long> SelfTendTimer = new Dictionary<Pawn, long>();

        static Main()
        {
            var harmony = new Harmony("com.forcedojob.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            /*Log.Message(
                "ForceDoJob Harmony Patches:" + Environment.NewLine +
                "  Prefix:" + Environment.NewLine +
                "    FloatMenuMakerMap.ChoicesAtFor  - not blocking" + Environment.NewLine +
                "    Pawn_PlayerSettings.EffectiveAreaRestrictionInPawnCurrentMap { get; } - blocking if set in settings" + Environment.NewLine +
                "    Pawn_WorkSettings.GetPriority - will block in the case of user right click for pawn actions [HarmonyBefore(\"fluffy.worktab\")]" + Environment.NewLine +
                "  Postfix:" + Environment.NewLine +
                "    FloatMenuMakerMap.ChoicesAtFor - must not be blocked otherwise all work assignments will be set to 3");*/
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    static class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        [HarmonyPriority(Priority.HigherThanNormal)]
        static void Prefix(Pawn pawn)
        {
            Main.ChoicesForPawn = pawn;
            if (pawn.playerSettings.selfTend == false && Settings.AllowPawnsToSelfTend)
            {
                Main.SelfTendTimer[pawn] = DateTime.Now.Ticks;
                pawn.playerSettings.selfTend = Settings.AllowPawnsToDoAllJobs || (Main.ChoicesForPawn != null && Main.ChoicesForPawn.story != null && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor));
            }
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
                    (Main.ChoicesForPawn != null && Main.ChoicesForPawn.story != null && !Main.ChoicesForPawn.WorkTypeIsDisabled(w)))
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


    [HarmonyPatch(typeof(GameDataSaveLoader), "SaveGame")]
    static class Patch_GameDataSaveLoader_SaveGame
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix()
        {
            if (Settings.AllowPawnsToSelfTend)
            {
                foreach(var kv in Main.SelfTendTimer)
                {
                    kv.Key.playerSettings.selfTend = false;
                }
            }
        }

        [HarmonyPriority(Priority.First)]
        static void Postfix()
        {
            if (Settings.AllowPawnsToSelfTend)
            {
                foreach (var kv in Main.SelfTendTimer)
                {
                    kv.Key.playerSettings.selfTend = true;
                }
            }
        }
    }

    class WorldComp : WorldComponent
    {
        private const long FIVE_SECONDS = 5 * TimeSpan.TicksPerSecond;
        private long lastUpdate = -1;

        public WorldComp(World world) : base(world)
        {
            Main.SelfTendTimer.Clear();
        }
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (Settings.AllowPawnsToSelfTend && Main.SelfTendTimer.Count > 0)
            {
                long now = DateTime.Now.Ticks;
                if (now - lastUpdate > TimeSpan.TicksPerSecond)
                {
                    lastUpdate = now;
                    Stack<Pawn> toRemove = new Stack<Pawn>();
                    foreach (var kv in Main.SelfTendTimer)
                    {
                        if (now - kv.Value > FIVE_SECONDS &&
                            !kv.Key.health.HasHediffsNeedingTend())
                        {
                            kv.Key.playerSettings.selfTend = false;
                            toRemove.AddItem(kv.Key);
                        }
                    }
                    foreach (Pawn p in toRemove)
                    {
                        Main.SelfTendTimer.Remove(p);
                    }
                }
            }
        }
    }
}