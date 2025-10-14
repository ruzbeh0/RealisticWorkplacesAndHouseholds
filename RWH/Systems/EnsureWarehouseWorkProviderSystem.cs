// File: EnsureWarehouseWorkProviderSystem.cs
using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    /// Cheap, idempotent: every frame, add WorkProvider to StorageCompany *instances* that still miss it.
    [BurstCompile]
    public partial class EnsureWarehouseWorkProviderSystem : GameSystemBase
    {
        private EntityQuery _missingQ;
        private EndFrameBarrier _endFrame;

        protected override void OnCreate()
        {
            base.OnCreate();

            _endFrame = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            _missingQ = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<CompanyData>(),
                    ComponentType.ReadOnly<PropertyRenter>(),
                    ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
                },
                None = new[]
                {
                    ComponentType.Exclude<WorkProvider>(), // only those without provider
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                }
            });
            // Do NOT RequireForUpdate; we want to run and check every tick.
        }

        protected override void OnUpdate()
        {
            int missing = _missingQ.CalculateEntityCount();
            if (missing == 0) return;

            var ecb = _endFrame.CreateCommandBuffer();
            var companies = _missingQ.ToEntityArray(Allocator.Temp);

            foreach (var c in companies)
            {
                ecb.AddComponent(c, new WorkProvider { m_MaxWorkers = 1, m_EfficiencyCooldown = 0 });
                // Optional: Mod.log.Info($"[EnsureWarehouse] added WorkProvider to {c.Index}");
            }

            companies.Dispose();
            _endFrame.AddJobHandleForProducer(Dependency);
        }
    }
}
