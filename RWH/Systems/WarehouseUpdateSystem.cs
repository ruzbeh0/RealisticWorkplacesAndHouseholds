using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    //[BurstCompile]
    public partial class WarehouseUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateWarehouseJobQuery;
        private EntityQuery m_ResetWarehouseJobQuery;

        private bool m_TriggerInitialWarehouseUpdate = false;
        private EndFrameBarrier m_EndFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();   

            // Query: excludes already updated entities
            UpdateWarehouseJobQuery standardQuery = new();
            m_UpdateWarehouseJobQuery = GetEntityQuery(standardQuery.Query);

            // Query: allows all entities (including already updated ones) for reset
            EntityQueryDesc resetQuery = new()
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
                    ComponentType.Exclude<WorkProvider>(),
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            };
            m_ResetWarehouseJobQuery = GetEntityQuery(resetQuery);

            //this.RequireForUpdate(m_UpdateWarehouseJobQuery);
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_TriggerInitialWarehouseUpdate = true;
        }

        [Preserve]
        protected override void OnUpdate()
        {
            //Mod.log.Info($"OnUpdate, Reset:{m_TriggerInitialWarehouseUpdate}");
            if (m_TriggerInitialWarehouseUpdate)
            {
                UpdateWarehouse(true); // Reset mode
                m_TriggerInitialWarehouseUpdate = false;
            }
            else
            {
                UpdateWarehouse(false); // Standard update
            }
        }

        private void UpdateWarehouse(bool reset)
        {
            var ecbSys = m_EndFrameBarrier;

            var activeQuery = reset ? m_ResetWarehouseJobQuery : m_UpdateWarehouseJobQuery;

            //Mod.log.Info($"UpdateWarehouse, Reset:{reset}, Entities:{activeQuery.CalculateEntityCount()}");

            if (reset)
            {
                activeQuery.ResetFilter(); // include all entities
            }
            else
            {
                activeQuery.SetChangedVersionFilter(typeof(Game.Companies.StorageCompany));
            }

            var commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            UpdateWarehouseJob updateZonableWarehouse = new UpdateWarehouseJob
            {
                ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentLookup<WorkplaceData>(true),
            };

            this.Dependency = updateZonableWarehouse.ScheduleParallel(activeQuery, this.Dependency);
            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
