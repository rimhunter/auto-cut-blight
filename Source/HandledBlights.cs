using System.Collections.Generic;

namespace Rimhunter.AutoCutBlight
{
    sealed class HandledBlights
    {
        readonly HashSet<int> ids = new HashSet<int>();

        public IEnumerable<int> Ids => ids;

        public bool Contains(int blightId) => ids.Contains(blightId);

        public bool TryRecord(int blightId) => ids.Add(blightId);

        public int Trim(IEnumerable<int> liveBlightIds) {
            var live = new HashSet<int>(liveBlightIds);
            return ids.RemoveWhere(id => !live.Contains(id));
        }

        public void Replace(IEnumerable<int> saved) {
            ids.Clear();
            foreach (var id in saved) ids.Add(id);
        }
    }
}
