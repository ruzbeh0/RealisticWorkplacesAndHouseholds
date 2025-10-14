// File: StorageWorkplacePrefabSeederSystem.cs
using Game;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    /// Run once in PrefabUpdate: add WorkplaceData to all StorageCompany PREFABS.
    [BurstCompile]
    public partial class StorageWorkplacePrefabSeederSystem : GameSystemBase
    {
        private EntityQuery _q;
        private bool _done;

        protected override void OnCreate()
        {
            base.OnCreate();
            _q = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<CompanyData>(),
                    ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                }
            });
        }

        protected override void OnUpdate()
        {
            if (_done) return;

            var prefabs = _q.ToEntityArray(Allocator.Temp);
            if (prefabs.Length == 0) { prefabs.Dispose(); return; }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var hasWorkplace = GetComponentLookup<WorkplaceData>(true);
            var hasProcess = GetComponentLookup<IndustrialProcessData>(true);

            foreach (var p in prefabs)
            {
                if (!hasWorkplace.HasComponent(p))
                    ecb.AddComponent(p, new WorkplaceData { m_MaxWorkers = 1 }); // UI visibility; will be scaled later

                if (!hasProcess.HasComponent(p))
                    ecb.AddComponent<IndustrialProcessData>(p);                 // default struct; prevents stats crash
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            prefabs.Dispose();

            _done = true;        // one-shot per load
            Enabled = false;
        }
    }
}
