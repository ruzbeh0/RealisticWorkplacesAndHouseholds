
using Game.Common;
using Game.Buildings;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
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
        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabRefHandle;
        [ReadOnly] public ComponentTypeHandle<PropertyRenter> PropertyRenterHandle;

        [ReadOnly] public ComponentLookup<CompanyData> CompanyDataLookup;
        [ReadOnly] public ComponentLookup<WorkProvider> WorkProviderLookup;
        [ReadOnly] public BufferLookup<Employee> EmployeeLookup;
        [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefLookup;
        [ReadOnly] public BufferLookup<SubMesh> PrefabSubMeshesLookup;
        [ReadOnly] public ComponentLookup<MeshData> MeshDataLookup;
        [ReadOnly] public ComponentLookup<UsableFootprintFactor> UffLookup;
        [ReadOnly] public ComponentLookup<RealisticWorkplaceData> RealisticWorkplaceDataLookup;

        [ReadOnly] public float industry_avg_floor_height;
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

            for (int i = 0; i < entities.Length; i++)
            {
                var storageEntity = entities[i];
                var propertyEntity = hasPropertyRenter ? propertyRenters[i].m_Property : storageEntity;
                int maxWorkers = CalculateWarehouseWorkers(propertyEntity);
                Entity targetEntity = GetWorkProviderTarget(storageEntity, propertyEntity);

                ApplyWarehouseWorkplaceData(storageEntity, maxWorkers, unfilteredChunkIndex);

                if (targetEntity != storageEntity)
                    ApplyWarehouseWorkplaceData(targetEntity, maxWorkers, unfilteredChunkIndex);
            }
        }

        private Entity GetWorkProviderTarget(Entity storageEntity, Entity propertyEntity)
        {
            if (CompanyDataLookup.HasComponent(storageEntity))
                return storageEntity;

            if (propertyEntity != Entity.Null && PrefabRefLookup.HasComponent(propertyEntity))
                return propertyEntity;

            return storageEntity;
        }

        private void ApplyWarehouseWorkplaceData(Entity entity, int maxWorkers, int sortKey)
        {
            WorkProvider workProvider = WorkProviderLookup.HasComponent(entity)
                ? WorkProviderLookup[entity]
                : new WorkProvider { m_EfficiencyCooldown = 0 };
            workProvider.m_MaxWorkers = maxWorkers;

            if (WorkProviderLookup.HasComponent(entity))
                ecb.SetComponent(sortKey, entity, workProvider);
            else
                ecb.AddComponent(sortKey, entity, workProvider);

            if (!EmployeeLookup.HasBuffer(entity))
                ecb.AddBuffer<Employee>(sortKey, entity);

            RealisticWorkplaceData realisticWorkplaceData = new()
            {
                max_workers = maxWorkers
            };

            if (RealisticWorkplaceDataLookup.HasComponent(entity))
                ecb.SetComponent(sortKey, entity, realisticWorkplaceData);
            else
                ecb.AddComponent(sortKey, entity, realisticWorkplaceData);
        }

        private int CalculateWarehouseWorkers(Entity propertyEntity)
        {
            if (propertyEntity == Entity.Null || !PrefabRefLookup.TryGetComponent(propertyEntity, out var buildingPrefabRef))
                return 1;

            Entity buildingPrefab = buildingPrefabRef.m_Prefab;
            if (!PrefabSubMeshesLookup.TryGetBuffer(buildingPrefab, out var subMeshes))
                return 1;

            var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, MeshDataLookup);
            var size = ObjectUtils.GetSize(dimensions);
            float width = size.x;
            float length = size.z;
            float height = size.y;

            float uff = UffLookup.HasComponent(buildingPrefab) ? UffLookup[buildingPrefab].Value : 1f;
            width *= (float)Math.Sqrt(uff);
            length *= (float)Math.Sqrt(uff);

            int workers = BuildingUtils.depotWorkers(width, length, height, industry_avg_floor_height, warehouse_sqm_per_worker);
            workers = (int)(workers * (1f - global_reduction));

            return Math.Max(1, workers);
        }

    }
}
