
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using RealisticWorkplacesAndHouseholds;
using Game.Companies;
using Game.Buildings;
using RealisticWorkplacesAndHouseholds.Prefabs;
using UnityEngine;
using Game.Citizens;

namespace RealisticWorkplacesAndHouseholds.Jobs
{
    public struct UpdateCommercialWorkplaceJobQuery
    {
        public EntityQueryDesc[] Query;
        public EntityQueryDesc[] BuildingWorkerQuery;

        public UpdateCommercialWorkplaceJobQuery()
        {
            Query =
            [
                new()
                {
                    All =
                    [
                        ComponentType.ReadOnly<CompanyData>(),
                        ComponentType.ReadOnly<PropertyRenter>(),
                        ComponentType.ReadWrite<WorkProvider>(),
                    ],
                    Any = [
                    ],
                    None =
                    [
                        ComponentType.Exclude<RealisticWorkplaceData>(),
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
                }
            ];
        }
    }

    [BurstCompile]
    public struct UpdateWorkplaceJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly]
        public ComponentTypeHandle<CompanyData> CompanyDataHandle;
        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> PropertyRenterHandle;
        public ComponentTypeHandle<WorkProvider> WorkProviderHandle;
        [ReadOnly]
        public ComponentLookup<PrefabRef> PrefabRefLookup;
        [ReadOnly]
        public BufferLookup<SubMesh> PrefabSubMeshesLookup;
        [ReadOnly]
        public ComponentLookup<MeshData> meshDataLookup;
        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDataLookup;
        [ReadOnly]
        public ComponentLookup<ZoneData> ZoneDataLookup;
        public ComponentLookup<BuildingPropertyData> BuildingPropertyDataLookup;
        [ReadOnly]
        public float commercial_sqm_per_employee;
        [ReadOnly]
        public float office_sqm_per_employee;
        [ReadOnly]
        public float commercial_avg_floor_height;
        [ReadOnly]
        public float industry_sqm_per_employee;
        [ReadOnly]
        public float industry_avg_floor_height;

        public UpdateWorkplaceJob()
        {

        }

        public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var workProviderArr = chunk.GetNativeArray(ref WorkProviderHandle);
            var propertyRenterArr = chunk.GetNativeArray(ref PropertyRenterHandle);

            for (int i = 0; i < workProviderArr.Length; i++)
            {
                var entity = entities[i];
                PropertyRenter propertyRenter = propertyRenterArr[i];
                WorkProvider workProvider = workProviderArr[i];
                PrefabRef prefabData;

                if (PrefabRefLookup.TryGetComponent(propertyRenter.m_Property, out prefabData))
                {
                    if (SpawnableBuildingDataLookup.TryGetComponent(prefabData.m_Prefab, out var spawnBuildingData))
                    {
                        if (spawnBuildingData.m_ZonePrefab != Entity.Null)
                        {
                            if (ZoneDataLookup.TryGetComponent(spawnBuildingData.m_ZonePrefab, out var zonedata))
                            {
                                if (PrefabSubMeshesLookup.TryGetBuffer(prefabData.m_Prefab, out var subMeshes))
                                {
                                    var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                                    var size = ObjectUtils.GetSize(dimensions);
                                    float width = size.x;
                                    float length = size.z;
                                    float height = size.y;

                                    int original_workers = workProvider.m_MaxWorkers;
                                    int new_workers = original_workers;

                                    bool lowDensity = true;
                                    if (zonedata.m_MaxHeight > 25)
                                    {
                                        lowDensity = false;
                                    }

                                    if (zonedata.m_AreaType != Game.Zones.AreaType.Residential)
                                    {

                                        if (zonedata.m_AreaType == Game.Zones.AreaType.Commercial)
                                        {
                                            new_workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, commercial_sqm_per_employee, false);
                                        }
                                        else
                                            {
                                                //Office
                                                if ((zonedata.m_ZoneFlags & ZoneFlags.Office) != 0)
                                                {
                                                    new_workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, office_sqm_per_employee, !lowDensity);
                                                    //Mod.log.Info($"Original number of Workers:{original_workers}, New:{new_workers}");
                                                }
                                                else
                                                {
                                                    //Industry
                                                    new_workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, industry_sqm_per_employee, !lowDensity);
                                                    //Mod.log.Info($"Original number of Workers:{original_workers}, New:{new_workers}");
                                                }
                                            }

                                        if (new_workers != original_workers)
                                        {
                                            RealisticWorkplaceData realisticWorkplaceData = new();
                                            realisticWorkplaceData.max_workers = new_workers;

                                            workProvider.m_MaxWorkers = new_workers;
                                            workProviderArr[i] = workProvider;

                                            if (BuildingPropertyDataLookup.TryGetComponent(prefabData.m_Prefab, out var buildingPropertyData))
                                            {
                                                float factor = new_workers / original_workers;
                                                buildingPropertyData.m_SpaceMultiplier *= factor;
                                                realisticWorkplaceData.space_multiplier = buildingPropertyData.m_SpaceMultiplier;

                                                ecb.SetComponent(unfilteredChunkIndex, prefabData.m_Prefab, buildingPropertyData);
                                                ecb.AddComponent(unfilteredChunkIndex, entity, realisticWorkplaceData);

                                            }
                                        } 
                                    }
                                }                                  
                            }
                        }
                    } 
                }

            }   
        }
    }
}