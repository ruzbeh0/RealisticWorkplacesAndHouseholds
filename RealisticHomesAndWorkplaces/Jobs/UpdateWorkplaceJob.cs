
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
using RealisticWorkplacesAndHouseholds.Components;
using UnityEngine;
using Game.Citizens;

namespace RealisticWorkplacesAndHouseholds.Jobs
{
    public struct UpdateWorkplaceJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdateWorkplaceJobQuery()
        {
            Query =
            [
                new()
                {
                    All =
                    [
                        ComponentType.ReadOnly<PrefabRef>(),
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
        public ComponentLookup<ServiceCompanyData> ServiceCompanyDataLookup;
        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> PropertyRenterHandle;
        public ComponentTypeHandle<WorkProvider> WorkProviderHandle;
        public ComponentLookup<IndustrialProcessData> IndustrialProcessDataLookup;
        public ComponentLookup<WorkplaceData> WorkplaceDataLookup;
        [ReadOnly]
        public ComponentLookup<PrefabRef> PrefabRefLookup;
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> PrefabRefHandle;
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
        [ReadOnly]
        public int office_sqm_per_elevator;

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
            var prefabRefArr = chunk.GetNativeArray(ref PrefabRefHandle);

            for (int i = 0; i < workProviderArr.Length; i++)
            {
                var entity = entities[i];
                PropertyRenter propertyRenter = propertyRenterArr[i];
                WorkProvider workProvider = workProviderArr[i];
                PrefabRef prefab1 = prefabRefArr[i];
                PrefabRef prefab2;

                if (PrefabRefLookup.TryGetComponent(propertyRenter.m_Property, out prefab2))
                {
                    if (SpawnableBuildingDataLookup.TryGetComponent(prefab2.m_Prefab, out var spawnBuildingData))
                    {
                        if (spawnBuildingData.m_ZonePrefab != Entity.Null)
                        {
                            if (ZoneDataLookup.TryGetComponent(spawnBuildingData.m_ZonePrefab, out var zonedata))
                            {
                                if (PrefabSubMeshesLookup.TryGetBuffer(prefab2.m_Prefab, out var subMeshes))
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

                                        if (zonedata.m_AreaType == Game.Zones.AreaType.Residential)
                                        {
                                            new_workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, commercial_sqm_per_employee, false, office_sqm_per_elevator);
                                        }
                                        else
                                            {
                                                //Office
                                                if ((zonedata.m_ZoneFlags & ZoneFlags.Office) != 0)
                                                {
                                                    new_workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, office_sqm_per_employee, !lowDensity, office_sqm_per_elevator);
                                                    //Mod.log.Info($"Original number of Workers:{original_workers}, New:{new_workers}");
                                                }
                                                else
                                                {
                                                    //Industry
                                                    new_workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, industry_sqm_per_employee, !lowDensity, office_sqm_per_elevator);
                                                    //Mod.log.Info($"Original number of Workers:{original_workers}, New:{new_workers}");
                                                }
                                            }

                                        if (new_workers != original_workers)
                                        {
                                            RealisticWorkplaceData realisticWorkplaceData = new();
                                            realisticWorkplaceData.max_workers = new_workers;

                                            workProvider.m_MaxWorkers = new_workers;
                                            workProviderArr[i] = workProvider;

                                            //Calculate factor
                                            float factor = 1f;
                                            if (original_workers > 0)
                                            {
                                                factor = new_workers / original_workers;
                                            }

                                            if (BuildingPropertyDataLookup.TryGetComponent(prefab2.m_Prefab, out var buildingPropertyData))
                                            {
                                                buildingPropertyData.m_SpaceMultiplier *= factor;
                                                realisticWorkplaceData.space_multiplier = buildingPropertyData.m_SpaceMultiplier;

                                                ecb.SetComponent(unfilteredChunkIndex, prefab2.m_Prefab, buildingPropertyData);
                                            }

                                            if (WorkplaceDataLookup.TryGetComponent(prefab1.m_Prefab, out var workplaceData))
                                            {
                                                //Mod.log.Info($"Workplace:{workplaceData.m_MaxWorkers}, new:{workplaceData.m_MaxWorkers * factor}");
                                                workplaceData.m_MaxWorkers = (int)(((float)workplaceData.m_MaxWorkers) * factor);

                                                ecb.SetComponent(unfilteredChunkIndex, prefab1.m_Prefab, workplaceData);
                                            }

                                            if (ServiceCompanyDataLookup.TryGetComponent(prefab1.m_Prefab, out var serviceCompanyData))
                                            {
                                                //Mod.log.Info($"Service Company old workers per cell:{serviceCompanyData.m_MaxWorkersPerCell}, new:{serviceCompanyData.m_MaxWorkersPerCell * factor}");
                                                serviceCompanyData.m_MaxWorkersPerCell *= factor;
                                                serviceCompanyData.m_WorkPerUnit = (int)(((float)(serviceCompanyData.m_WorkPerUnit))*factor);

                                                ecb.SetComponent(unfilteredChunkIndex, prefab1.m_Prefab, serviceCompanyData);
                                            }

                                            if (IndustrialProcessDataLookup.TryGetComponent(prefab1.m_Prefab, out var industrialProcessData))
                                            {
                                                //Mod.log.Info($"Industrial Process old workers per cell:{industrialProcessData.m_MaxWorkersPerCell}, new:{industrialProcessData.m_MaxWorkersPerCell * factor}");
                                                industrialProcessData.m_MaxWorkersPerCell *= factor;
                                                industrialProcessData.m_WorkPerUnit = (int)(((float)(industrialProcessData.m_WorkPerUnit)) * factor);

                                                ecb.SetComponent(unfilteredChunkIndex, prefab1.m_Prefab, industrialProcessData);
                                            }

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