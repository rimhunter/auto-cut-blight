using System.Linq;
using RimWorld;
using Verse;

namespace Rimhunter.AutoCutBlight
{
    public class BlightWatcher : MapComponent
    {
        const int ScanIntervalTicks = 60;

        readonly HandledBlights handled = new HandledBlights();

        public BlightWatcher(Map map) : base(map) {
        }

        public override void ExposeData() {
            base.ExposeData();
            var saved = handled.Ids.ToList();
            Scribe_Collections.Look(ref saved, "handledBlights", LookMode.Value);
            if (Scribe.mode != LoadSaveMode.LoadingVars) return;
            if (saved == null) handled.Replace(Enumerable.Empty<int>());
            else handled.Replace(saved);
        }

        public override void MapComponentTick() {
            if (Find.TickManager.TicksGame % ScanIntervalTicks != 0) return;
            if (!map.IsPlayerHome) return;
            var blights = map.listerThings.ThingsOfDef(ThingDefOf.Blight);
            handled.Trim(blights.Select(b => b.thingIDNumber));
            foreach (var thing in blights) {
                if (handled.Contains(thing.thingIDNumber)) continue;
                var plant = ((Blight) thing).Plant;
                if (plant == null) continue;
                if (!map.designationManager.HasMapDesignationOn(plant))
                    map.designationManager.AddDesignation(new Designation(plant, DesignationDefOf.CutPlant));
                handled.TryRecord(thing.thingIDNumber);
            }
        }
    }
}
