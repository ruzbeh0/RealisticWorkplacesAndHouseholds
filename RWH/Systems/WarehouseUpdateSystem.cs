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
            // Non-company renters also need their property target checked: another
            // entity's components cannot be tested by a storage-entity query.
            var propertyTargets = CreateRepairQuery(ComponentType.Exclude<CompanyData>());
            System.Array.Resize(ref propertyTargets.All, propertyTargets.All.Length + 1);
            propertyTargets.All[propertyTargets.All.Length - 1] = ComponentType.ReadOnly<PropertyRenter>();
            m_RepairWarehouseQuery = GetEntityQuery(
                CreateRepairQuery(ComponentType.Exclude<WorkProvider>()),
                CreateRepairQuery(ComponentType.Exclude<Employee>()),
                CreateRepairQuery(ComponentType.Exclude<RealisticWorkplaceData>()),
                propertyTargets);

            RequireForUpdate(m_UpdateWarehouseJobQuery);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            UpdateWarehouse();
        }

        private static EntityQueryDesc CreateRepairQuery(ComponentType excludedComponent)
        {
            // Derive every repair branch from the same service/deletion exclusions
            // as the capacity query, rather than maintaining a second copy.
            var query = new UpdateWarehouseJobQuery().Query[0];
            System.Array.Resize(ref query.None, query.None.Length + 1);
            query.None[query.None.Length - 1] = excludedComponent;
            return query;
        }

        private void UpdateWarehouse()
        {
            EnsureWarehouseWorkProviderTargets();

            UpdateWarehouseJob updateZonableWarehouse = new UpdateWarehouseJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
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
                CityServiceUpkeepLookup = SystemAPI.GetComponentLookup<CityServiceUpkeep>(true),
                ResourceConsumerLookup = SystemAPI.GetComponentLookup<Game.Buildings.ResourceConsumer>(true),
                HospitalLookup = SystemAPI.GetComponentLookup<Game.Buildings.Hospital>(true),
                ServiceDispatchLookup = SystemAPI.GetBufferLookup<ServiceDispatch>(true),
                ServiceObjectDataLookup = SystemAPI.GetComponentLookup<ServiceObjectData>(true),
                ServiceUpkeepDataLookup = SystemAPI.GetBufferLookup<ServiceUpkeepData>(true),
                ResourceConsumerDataLookup = SystemAPI.GetComponentLookup<ResourceConsumerData>(true),
                HospitalDataLookup = SystemAPI.GetComponentLookup<HospitalData>(true),
                ABCWorkplaceOverrideLookup = SystemAPI.GetComponentLookup<ABCWorkplaceOverride>(true),
                industry_avg_floor_height = Mod.m_Setting.industry_avg_floor_height,
                warehouse_sqm_per_worker = Mod.m_Setting.warehouse_sqm_per_worker,
                global_reduction = Mod.m_Setting.results_reduction / 100f,
            };

            this.Dependency = updateZonableWarehouse.ScheduleParallel(m_UpdateWarehouseJobQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }

        private void EnsureWarehouseWorkProviderTargets()
        {
            // This query has no change/shared filters. The empty check needs no
            // dependency completion or entity-array allocation in the common case.
            if (m_RepairWarehouseQuery.IsEmptyIgnoreFilter)
                return;

            // Initialization must remain immediate: vanilla WorkProviderSystem runs
            // before EndFrameBarrier playback, and property targets can be shared.
            Dependency.Complete();

            using var storageEntities = m_RepairWarehouseQuery.ToEntityArray(Allocator.Temp);

            foreach (var storageEntity in storageEntities)
            {
                Entity targetEntity = storageEntity;
                Entity propertyEntity = Entity.Null;
                if (EntityManager.HasComponent<PropertyRenter>(storageEntity))
                {
                    propertyEntity = EntityManager.GetComponentData<PropertyRenter>(storageEntity).m_Property;
                }

                if (IsServiceOrTransportTarget(storageEntity) || IsServiceOrTransportTarget(propertyEntity))
                    continue;

                if (!EntityManager.HasComponent<CompanyData>(storageEntity) && propertyEntity != Entity.Null)
                    targetEntity = propertyEntity;

                EnsureTargetComponents(storageEntity);

                if (targetEntity != storageEntity)
                    EnsureTargetComponents(targetEntity);
            }
        }

        private bool IsServiceOrTransportTarget(Entity entity)
        {
            if (entity == Entity.Null)
                return false;

            if (EntityManager.HasComponent<CityServiceUpkeep>(entity) ||
                EntityManager.HasComponent<Game.Buildings.ResourceConsumer>(entity) ||
                EntityManager.HasComponent<Game.Buildings.Hospital>(entity) ||
                EntityManager.HasBuffer<ServiceDispatch>(entity))
            {
                return true;
            }

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
                (EntityManager.HasComponent<ServiceObjectData>(prefab) ||
                 EntityManager.HasBuffer<ServiceUpkeepData>(prefab) ||
                 EntityManager.HasComponent<ResourceConsumerData>(prefab) ||
                 EntityManager.HasComponent<HospitalData>(prefab) ||
                 EntityManager.HasComponent<TransportStationData>(prefab) ||
                 EntityManager.HasComponent<CargoTransportStationData>(prefab) ||
                 EntityManager.HasComponent<TransportDepotData>(prefab) ||
                 EntityManager.HasComponent<MaintenanceDepotData>(prefab));
        }

        private void EnsureTargetComponents(Entity entity)
        {
            if (entity == Entity.Null ||
                !EntityManager.HasComponent<PrefabRef>(entity) ||
                IsServiceOrTransportTarget(entity) ||
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
    }
}
