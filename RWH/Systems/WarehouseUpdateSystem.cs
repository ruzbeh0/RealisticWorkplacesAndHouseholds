using Game;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    public partial class WarehouseUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateWarehouseJobQuery;
        private EntityQuery m_RepairWarehouseQuery;

        private EndFrameBarrier m_EndFrameBarrier;

        private const int kCapacityCheckInterval = 64;
        private int m_TicksUntilCapacityCheck;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Keep checking capacities so settings, prefab changes and ABC overrides
            // take effect without reloading. Initialization uses a narrower query below.
            UpdateWarehouseJobQuery standardQuery = new();
            m_UpdateWarehouseJobQuery = GetEntityQuery(standardQuery.Query);

            // OR query: fully initialized companies never enter the synchronous pass.
            m_RepairWarehouseQuery = GetEntityQuery(
                CreateRepairQuery(ComponentType.Exclude<WorkProvider>()),
                CreateRepairQuery(ComponentType.Exclude<Employee>()),
                CreateRepairQuery(ComponentType.Exclude<RealisticWorkplaceData>()));

            RequireForUpdate(m_UpdateWarehouseJobQuery);
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            // Rescan right after load so every warehouse in the save gets its capacity.
            m_TicksUntilCapacityCheck = 0;
        }

        [Preserve]
        protected override void OnUpdate()
        {
            UpdateWarehouse();
        }

        private static EntityQueryDesc CreateRepairQuery(ComponentType excludedComponent)
        {
            // Derive every repair branch from the capacity query, rather than maintaining a second copy.
            var query = new UpdateWarehouseJobQuery().Query[0];
            System.Array.Resize(ref query.None, query.None.Length + 1);
            query.None[query.None.Length - 1] = excludedComponent;
            return query;
        }

        private void UpdateWarehouse()
        {
            // Repairs must stay immediate; the full capacity scan only needs to follow them
            // or pick up setting/ABC changes, so it no longer runs on every simulation tick.
            bool repaired = EnsureWarehouseWorkProviderTargets();
            if (!repaired && --m_TicksUntilCapacityCheck > 0)
                return;
            m_TicksUntilCapacityCheck = kCapacityCheckInterval;

            UpdateWarehouseJob updateZonableWarehouse = new UpdateWarehouseJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                PropertyRenterHandle = SystemAPI.GetComponentTypeHandle<PropertyRenter>(true),
                WorkProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(true),
                EmployeeLookup = SystemAPI.GetBufferLookup<Employee>(true),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                PrefabSubMeshesLookup = SystemAPI.GetBufferLookup<SubMesh>(true),
                MeshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                UffLookup = SystemAPI.GetComponentLookup<UsableFootprintFactor>(true),
                RealisticWorkplaceDataLookup = SystemAPI.GetComponentLookup<RealisticWorkplaceData>(true),
                ABCWorkplaceOverrideLookup = SystemAPI.GetComponentLookup<ABCWorkplaceOverride>(true),
                industry_avg_floor_height = Mod.m_Setting.industry_avg_floor_height,
                warehouse_sqm_per_worker = Mod.m_Setting.warehouse_sqm_per_worker,
                global_reduction = Mod.m_Setting.results_reduction / 100f,
            };

            this.Dependency = updateZonableWarehouse.ScheduleParallel(m_UpdateWarehouseJobQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }

        private bool EnsureWarehouseWorkProviderTargets()
        {
            // This query has no change/shared filters. The empty check needs no
            // dependency completion or entity-array allocation in the common case.
            if (m_RepairWarehouseQuery.IsEmptyIgnoreFilter)
                return false;

            // Initialization must remain immediate: vanilla WorkProviderSystem runs
            // before EndFrameBarrier playback.
            Dependency.Complete();

            // Every candidate is missing a component, so each one leaves the query here.
            using var storageEntities = m_RepairWarehouseQuery.ToEntityArray(Allocator.Temp);
            foreach (var storageEntity in storageEntities)
                EnsureTargetComponents(storageEntity);

            return true;
        }

        private void EnsureTargetComponents(Entity entity)
        {
            if (!EntityManager.HasComponent<WorkProvider>(entity))
            {
                EntityManager.AddComponentData(entity, new WorkProvider
                {
                    m_MaxWorkers = 1,
                    m_EfficiencyCooldown = 0
                });
            }

            if (!EntityManager.HasComponent<Employee>(entity))
            {
                EntityManager.AddBuffer<Employee>(entity);
            }

            if (!EntityManager.HasComponent<RealisticWorkplaceData>(entity))
            {
                EntityManager.AddComponentData(entity, new RealisticWorkplaceData
                {
                    max_workers = 1
                });
            }
        }
    }
}
