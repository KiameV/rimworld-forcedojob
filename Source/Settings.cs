﻿using UnityEngine;
using Verse;

namespace ForceDoJob
{
    public class SettingsController : Mod
    {
        public SettingsController(ModContentPack content) : base(content)
        {
            base.GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "Force Do Job";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }

    public class Settings : ModSettings
    {
        public static bool AllowOutsideAllowedArea = true;
        public static bool AllowPawnsToDoAllJobs = false;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref AllowOutsideAllowedArea, "ForceDoJob.AllowOutsideAllowedArea", true, false);
            Scribe_Values.Look<bool>(ref AllowPawnsToDoAllJobs, "ForceDoJob.AllowPawnsToDoAllJobs", false, false);
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard(GameFont.Small);
            l.ColumnWidth = System.Math.Min(400, rect.width / 2);
            l.Begin(rect);
            l.CheckboxLabeled("Allow Orders Outside Allowed Area", ref AllowOutsideAllowedArea);
            l.CheckboxLabeled("Allow All Pawns To Do All Jobs (when manually told)", ref AllowPawnsToDoAllJobs);
            //l.Gap(8);
            l.End();
        }
    }
}
