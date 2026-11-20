
using Game.Common;
using Game.City;
using Game.Buildings;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using RealisticWorkplacesAndHouseholds.Components;
using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds.Jobs
{
    public struct UpdateWarehouseJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdateWarehouseJobQuery()
        {
            Query =
            [
                new()
                {
                    All =
                    [
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
                        // Zoned warehouses only: city-service storage (cargo stations) never has CompanyData.
                        ComponentType.ReadOnly<CompanyData>(),
                    ],
                    Any = [
               
                    ],
                    None =
                    [
                        ComponentType.Exclude<Game.Objects.OutsideConnection>(),
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
                },
            ];
        }
    }

    [BurstCompile]
    public struct UpdateWarehouseJob : IJobChunk
    {
        public const int MinWorkers = 1;

        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly] public ComponentTypeHandle<PropertyRenter> PropertyRenterHandle;

        [ReadOnly] public ComponentLookup<WorkProvider> WorkProviderLookup;
        [ReadOnly] public BufferLookup<Employee> EmployeeLookup;
        [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefLookup;
        [ReadOnly] public BufferLookup<SubMesh> PrefabSubMeshesLookup;
        [ReadOnly] public ComponentLookup<MeshData> MeshDataLookup;
        [ReadOnly] public ComponentLookup<UsableFootprintFactor> UffLookup;
        [ReadOnly] public ComponentLookup<RealisticWorkplaceData> RealisticWorkplaceDataLookup;
        [ReadOnly] public ComponentLookup<ABCWorkplaceOverride> ABCWorkplaceOverrideLookup;

        [ReadOnly] public float warehouse_sqm_per_worker;
        [ReadOnly] public float global_reduction;

        public UpdateWarehouseJob()
        {

        }

        public void Execute(in ArchetypeChunk chunk,
    int unfilteredChunkIndex,
    bool useEnabledMask,
    in v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
            bool hasPropertyRenter = chunk.Has(ref PropertyRenterHandle);
            NativeArray<PropertyRenter> propertyRenters = hasPropertyRenter
                ? chunk.GetNativeArray(ref PropertyRenterHandle)
                : default;

            // Reuse consecutive prefab calculations within this chunk only. Nothing
            // survives Execute, so settings, meshes and footprint changes remain live.
            Entity lastBuildingPrefab = Entity.Null;
            int lastMaxWorkers = MinWorkers;

            for (int i = 0; i < entities.Length; i++)
            {
                var storageEntity = entities[i];
                var propertyEntity = hasPropertyRenter ? propertyRenters[i].m_Property : storageEntity;

                if (HasABCWorkplaceOverride(storageEntity) || HasABCWorkplaceOverride(propertyEntity))
                    continue;

                Entity buildingPrefab = Entity.Null;
                if (propertyEntity != Entity.Null && PrefabRefLookup.TryGetComponent(propertyEntity, out var propertyPrefabRef))
                    buildingPrefab = propertyPrefabRef.m_Prefab;

                if (buildingPrefab != lastBuildingPrefab)
                {
                    lastMaxWorkers = CalculateWarehouseWorkers(buildingPrefab);
                    lastBuildingPrefab = buildingPrefab;
                }

                ApplyWarehouseWorkplaceData(storageEntity, lastMaxWorkers, unfilteredChunkIndex);
            }
        }

        private bool HasABCWorkplaceOverride(Entity entity)
        {
            if (entity == Entity.Null || !PrefabRefLookup.TryGetComponent(entity, out var prefabRef))
                return false;

            return ABCWorkplaceOverrideLookup.HasComponent(prefabRef.m_Prefab);
        }

        private void ApplyWarehouseWorkplaceData(Entity entity, int maxWorkers, int sortKey)
        {
            if (WorkProviderLookup.TryGetComponent(entity, out var workProvider))
            {
                if (workProvider.m_MaxWorkers != maxWorkers)
                {
                    // Preserve the current cooldown and any other provider fields.
                    workProvider.m_MaxWorkers = maxWorkers;
                    ecb.SetComponent(sortKey, entity, workProvider);
                }
            }
            else
            {
                workProvider = new WorkProvider { m_MaxWorkers = maxWorkers, m_EfficiencyCooldown = 0 };
                ecb.AddComponent(sortKey, entity, workProvider);
            }

            if (!EmployeeLookup.HasBuffer(entity))
                ecb.AddBuffer<Employee>(sortKey, entity);

            RealisticWorkplaceData realisticWorkplaceData = new()
            {
                max_workers = maxWorkers
            };

            if (RealisticWorkplaceDataLookup.TryGetComponent(entity, out var currentWorkplaceData))
            {
                // Preserve the old replacement semantics for all serialized fields,
                // including normalization of version and space_multiplier.
                if (currentWorkplaceData.max_workers != realisticWorkplaceData.max_workers ||
                    currentWorkplaceData.version != realisticWorkplaceData.version ||
                    currentWorkplaceData.space_multiplier != realisticWorkplaceData.space_multiplier)
                {
                    ecb.SetComponent(sortKey, entity, realisticWorkplaceData);
                }
            }
            else
                ecb.AddComponent(sortKey, entity, realisticWorkplaceData);
        }

        private int CalculateWarehouseWorkers(Entity buildingPrefab)
        {
            if (buildingPrefab == Entity.Null)
                return MinWorkers;

            if (!PrefabSubMeshesLookup.TryGetBuffer(buildingPrefab, out var subMeshes))
                return MinWorkers;

            var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, MeshDataLookup);
            var size = BuildingUtils.GetBuildingSize(dimensions);
            float width = size.x;
            float length = size.z;

            float uff = UffLookup.HasComponent(buildingPrefab) ? UffLookup[buildingPrefab].Value : 1f;
            width *= (float)Math.Sqrt(uff);
            length *= (float)Math.Sqrt(uff);

            int workers = BuildingUtils.warehouseWorkers(width, length, warehouse_sqm_per_worker);
            workers = (int)(workers * (1f - global_reduction));

            return Math.Max(MinWorkers, workers);
        }

    }
}
