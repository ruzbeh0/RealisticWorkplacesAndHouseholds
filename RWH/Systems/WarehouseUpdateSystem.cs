using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Jobs;
using Unity.Burst;
using Unity.Collections;
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
                    ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
                },
                None = new[]
                {
                    ComponentType.Exclude<Game.Objects.OutsideConnection>(),
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            };
            m_ResetWarehouseJobQuery = GetEntityQuery(resetQuery);

            this.RequireForUpdate(m_ResetWarehouseJobQuery);
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
                activeQuery.ResetFilter();
            }

            EnsureWarehouseWorkProviderTargets();

            UpdateWarehouseJob updateZonableWarehouse = new UpdateWarehouseJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                PropertyRenterHandle = SystemAPI.GetComponentTypeHandle<PropertyRenter>(true),
                CompanyDataLookup = SystemAPI.GetComponentLookup<CompanyData>(true),
                WorkProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(true),
                EmployeeLookup = SystemAPI.GetBufferLookup<Employee>(true),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                PrefabSubMeshesLookup = SystemAPI.GetBufferLookup<SubMesh>(true),
                MeshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                UffLookup = SystemAPI.GetComponentLookup<UsableFootprintFactor>(true),
                RealisticWorkplaceDataLookup = SystemAPI.GetComponentLookup<RealisticWorkplaceData>(true),
                TransportDepotLookup = SystemAPI.GetComponentLookup<Game.Buildings.TransportDepot>(true),
                CargoTransportStationLookup = SystemAPI.GetComponentLookup<Game.Buildings.CargoTransportStation>(true),
                MaintenanceDepotLookup = SystemAPI.GetComponentLookup<Game.Buildings.MaintenanceDepot>(true),
                TransportStationLookup = SystemAPI.GetComponentLookup<Game.Buildings.TransportStation>(true),
                TransportDepotDataLookup = SystemAPI.GetComponentLookup<TransportDepotData>(true),
                CargoTransportStationDataLookup = SystemAPI.GetComponentLookup<CargoTransportStationData>(true),
                MaintenanceDepotDataLookup = SystemAPI.GetComponentLookup<MaintenanceDepotData>(true),
                TransportStationDataLookup = SystemAPI.GetComponentLookup<TransportStationData>(true),
                industry_avg_floor_height = Mod.m_Setting.industry_avg_floor_height,
                warehouse_sqm_per_worker = Mod.m_Setting.warehouse_sqm_per_worker,
                global_reduction = Mod.m_Setting.results_reduction / 100f,
            };

            this.Dependency = updateZonableWarehouse.ScheduleParallel(activeQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }

        private void EnsureWarehouseWorkProviderTargets()
        {
            Dependency.Complete();

            var storageEntities = m_ResetWarehouseJobQuery.ToEntityArray(Allocator.Temp);
            var propertyRenterLookup = SystemAPI.GetComponentLookup<PropertyRenter>(true);
            var companyDataLookup = SystemAPI.GetComponentLookup<CompanyData>(true);

            foreach (var storageEntity in storageEntities)
            {
                Entity targetEntity = storageEntity;
                Entity propertyEntity = Entity.Null;
                if (propertyRenterLookup.TryGetComponent(storageEntity, out var propertyRenter))
                {
                    propertyEntity = propertyRenter.m_Property;
                }

                if (IsTransportServiceTarget(storageEntity) || IsTransportServiceTarget(propertyEntity))
                    continue;

                if (!companyDataLookup.HasComponent(storageEntity) && propertyEntity != Entity.Null)
                    targetEntity = propertyEntity;

                EnsureTargetComponents(storageEntity);

                if (targetEntity != storageEntity)
                    EnsureTargetComponents(targetEntity);
            }

            storageEntities.Dispose();
        }

        private bool IsTransportServiceTarget(Entity entity)
        {
            if (entity == Entity.Null)
                return false;

            if (EntityManager.HasComponent<Game.Buildings.TransportStation>(entity) ||
                EntityManager.HasComponent<Game.Buildings.CargoTransportStation>(entity) ||
                EntityManager.HasComponent<Game.Buildings.TransportDepot>(entity) ||
                EntityManager.HasComponent<Game.Buildings.MaintenanceDepot>(entity))
            {
                return true;
            }

            if (!EntityManager.HasComponent<PrefabRef>(entity))
                return false;

            Entity prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            return prefab != Entity.Null &&
                (EntityManager.HasComponent<TransportStationData>(prefab) ||
                 EntityManager.HasComponent<CargoTransportStationData>(prefab) ||
                 EntityManager.HasComponent<TransportDepotData>(prefab) ||
                 EntityManager.HasComponent<MaintenanceDepotData>(prefab));
        }

        private void EnsureTargetComponents(Entity entity)
        {
            if (entity == Entity.Null ||
                !EntityManager.HasComponent<PrefabRef>(entity) ||
                EntityManager.HasComponent<Deleted>(entity) ||
                EntityManager.HasComponent<Temp>(entity))
            {
                return;
            }

            if (!EntityManager.HasComponent<WorkProvider>(entity))
            {
                EntityManager.AddComponentData(entity, new WorkProvider
                {
                    m_MaxWorkers = 1,
                    m_EfficiencyCooldown = 0
                });
            }

            if (!EntityManager.HasComponent<Employee>(entity))
                EntityManager.AddBuffer<Employee>(entity);

            if (!EntityManager.HasComponent<RealisticWorkplaceData>(entity))
            {
                EntityManager.AddComponentData(entity, new RealisticWorkplaceData
                {
                    max_workers = 1
                });
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
