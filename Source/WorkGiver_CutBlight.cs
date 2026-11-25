using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimhunter.AutoCutBlight
{
    public class WorkGiver_CutBlight : WorkGiver_PlantsCut
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) =>
            pawn.Map.designationManager.designationsByDef[DesignationDefOf.CutPlant]
                .Select(d => d.target.Thing)
                .Where(IsBlighted);

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
            if (!IsBlighted(t)) return null;
            var job = base.JobOnThing(pawn, t, forced);
            return job != null && job.def == JobDefOf.CutPlantDesignated ? job : null;
        }

        static bool IsBlighted(Thing t) => t is Plant plant && plant.Blighted;
    }
}
