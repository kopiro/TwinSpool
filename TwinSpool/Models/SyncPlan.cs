using System.Collections.Generic;

namespace TwinSpool.Models
{
    public sealed class SyncPlan
    {
        public List<SyncPlanItem> FilesToCopy { get; set; } = new List<SyncPlanItem>();

        public List<SyncPlanItem> FilesToSkip { get; set; } = new List<SyncPlanItem>();

        public List<SyncPlanItem> Conflicts { get; set; } = new List<SyncPlanItem>();
    }
}
