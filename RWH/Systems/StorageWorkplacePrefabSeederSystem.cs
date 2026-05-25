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
                },
                Any = new[]
                {
                    ComponentType.ReadOnly<StorageCompanyData>(),
                    ComponentType.ReadOnly<WarehouseData>(),
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
            if (prefabs.Length == 0)
            {
                prefabs.Dispose();
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var hasWorkplace = GetComponentLookup<WorkplaceData>(true);
            var hasProcess = GetComponentLookup<IndustrialProcessData>(true);
            var storageCompanyDataLookup = GetComponentLookup<StorageCompanyData>(true);

            foreach (var p in prefabs)
            {
                if (!hasWorkplace.HasComponent(p))
                {
                    ecb.AddComponent(p, new WorkplaceData
                    {
                        m_Complexity = WorkplaceComplexity.Manual,
                        m_MaxWorkers = 1,
                        m_MinimumWorkersLimit = 1,
                        m_WorkConditions = 0
                    });
                }

                if (storageCompanyDataLookup.HasComponent(p) && !hasProcess.HasComponent(p))
                    ecb.AddComponent<IndustrialProcessData>(p);                 // default struct; prevents stats crash
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            prefabs.Dispose();

            _done = true;        // one-shot per load
            Enabled = false;
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (mode != GameMode.Game)
                return;

            _done = false;
            Enabled = true;
        }
    }
}
