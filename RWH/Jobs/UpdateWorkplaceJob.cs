
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Mono.Cecil;
using RealisticWorkplacesAndHouseholds;
using RealisticWorkplacesAndHouseholds.Components;
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using static Game.Prefabs.TriggerPrefabData;

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
                },
            ];
        }
    }

    [BurstCompile]
    public struct UpdateWorkplaceJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;
        public ComponentLookup<RealisticWorkplacesAndHouseholds.Components.UsableFootprintFactor> UffLookup;


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
        [ReadOnly]
        public ComponentLookup<RealisticWorkplaceData> RealisticWorkplaceDataLookup;
        public ComponentLookup<BuildingPropertyData> BuildingPropertyDataLookup;
        [ReadOnly]
        public ComponentLookup<ExtractorCompanyData> ExtractorCompanyDataLookup;
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
        [ReadOnly]
        public bool commercial_self_service_gas;
        [ReadOnly]
        public float non_usable_space_pct;
        [ReadOnly]
        public float commercial_sqm_per_worker_restaurants;
        [ReadOnly]
        public float commercial_sqm_per_worker_supermarket;
        [ReadOnly]
        public float commercial_sqm_per_worker_rec_entertainment;
        [ReadOnly]
        public float global_reduction;
        [ReadOnly]
        public bool reset;
        [ReadOnly]
        public int industry_base_threshold;
        [ReadOnly]
        public int office_height_threshold;

        public UpdateWorkplaceJob()
        {

        }

        float GetIndustryFactor(Game.Economy.Resource resource)
        {
            return resource switch
            {
                Game.Economy.Resource.Furniture => 1.2f,
                Game.Economy.Resource.Textiles => 1.3f,
                Game.Economy.Resource.Chemicals => 0.8f,
                Game.Economy.Resource.Plastics => 0.9f,
                Game.Economy.Resource.Electronics => 0.7f,
                Game.Economy.Resource.Vehicles => 0.6f,
                Game.Economy.Resource.Food => 1.1f,
                Game.Economy.Resource.Beverages => 1.05f,
                Game.Economy.Resource.Paper => 0.9f,
                Game.Economy.Resource.Pharmaceuticals => 0.75f,
                Game.Economy.Resource.Fish => 3f,
                _ => 1.0f
            };
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

            //Mod.log.Info($"reset:{reset}, length:{workProviderArr.Length}");
            for (int i = 0; i < workProviderArr.Length; i++)
            {
                var entity = entities[i];
                PropertyRenter propertyRenter = propertyRenterArr[i];
                WorkProvider workProvider = workProviderArr[i];
                PrefabRef prefab1 = prefabRefArr[i];

                if (!PrefabRefLookup.TryGetComponent(propertyRenter.m_Property, out var prefab2))
                    continue;

                var prefab2Entity = prefab2.m_Prefab;
                var prefab1Entity = prefab1.m_Prefab;

                bool hasSpawnableBuildingData = SpawnableBuildingDataLookup.TryGetComponent(prefab2Entity, out var spawnBuildingData);
                bool hasBuildingPropertyData = BuildingPropertyDataLookup.TryGetComponent(prefab2Entity, out var buildingPropertyData);
                bool hasWorkplaceData = WorkplaceDataLookup.TryGetComponent(prefab1Entity, out var workplaceData);
                bool hasServiceCompanyData = ServiceCompanyDataLookup.TryGetComponent(prefab1Entity, out var serviceCompanyData);
                bool hasIndustrialProcessData = IndustrialProcessDataLookup.TryGetComponent(prefab1Entity, out var industrialProcessData);

                if (!hasSpawnableBuildingData || spawnBuildingData.m_ZonePrefab == Entity.Null)
                    continue;

                if (!ZoneDataLookup.TryGetComponent(spawnBuildingData.m_ZonePrefab, out var zonedata))
                    continue;

                if (!PrefabSubMeshesLookup.TryGetBuffer(prefab2Entity, out var subMeshes))
                    continue;

                var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                var size = ObjectUtils.GetSize(dimensions);
                float width = size.x;
                float length = size.z;
                float height = size.y;

                int original_workers = workProvider.m_MaxWorkers;
                int new_workers = original_workers;

                // High density buildings have lobby
                int floor_offset = (zonedata.m_MaxHeight > 25) ? 1 : 0;

                float uff = UffLookup.HasComponent(entity) ? UffLookup[entity].Value : 1f;
                width *= (float)Math.Sqrt(uff);
                length *= (float)Math.Sqrt(uff);


                if (zonedata.m_AreaType == Game.Zones.AreaType.Residential)
                {
                    new_workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, commercial_sqm_per_employee, 0, 0, 1);
                }
                else if (zonedata.m_AreaType == Game.Zones.AreaType.Commercial)
                {
                    float area = commercial_sqm_per_employee;

                    if (hasBuildingPropertyData)
                    {
                        var resource = buildingPropertyData.m_AllowedSold;

                        if (resource == Game.Economy.Resource.Petrochemicals && commercial_self_service_gas)
                            area *= 1.8f;
                        else if (resource == Game.Economy.Resource.Meals)
                            area = commercial_sqm_per_worker_restaurants;
                        else if (resource == Game.Economy.Resource.Beverages || resource == Game.Economy.Resource.ConvenienceFood || resource == Game.Economy.Resource.Food)
                            area = commercial_sqm_per_worker_supermarket;
                        else if (resource == Game.Economy.Resource.Recreation || resource == Game.Economy.Resource.Entertainment)
                            area = commercial_sqm_per_worker_rec_entertainment;

                        area *= BuildingUtils.smooth_area_factor(70*70, width, length);

                    }

                    new_workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area, 0, office_sqm_per_elevator);
                }
                else if ((zonedata.m_ZoneFlags & ZoneFlags.Office) != 0)
                {
                    float employee_area = office_sqm_per_employee;
                    float area = employee_area * (1 + non_usable_space_pct);
                    float height_factor = BuildingUtils.smooth_height_factor(office_height_threshold, height / commercial_avg_floor_height);

                    new_workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area * height_factor, floor_offset, office_sqm_per_elevator);
                }
                else
                {

                    float area_factor = BuildingUtils.smooth_area_factor4(industry_base_threshold, width, length);

                    if (hasIndustrialProcessData)
                    {
                        
                        var resource = industrialProcessData.m_Output.m_Resource;
                        
                        if (resource == Game.Economy.Resource.Wood || resource == Game.Economy.Resource.Vegetables || resource == Game.Economy.Resource.Cotton || resource == Game.Economy.Resource.Grain ||
                            resource == Game.Economy.Resource.Livestock || resource == Game.Economy.Resource.Oil || resource == Game.Economy.Resource.Ore || resource == Game.Economy.Resource.Coal || resource == Game.Economy.Resource.Stone)
                        {
                            continue; // Skip extractors
                        }

                        area_factor *= GetIndustryFactor(resource);

                    }
                    //Using max floors of 2 for industry
                    new_workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, industry_sqm_per_employee * area_factor, 0, 0, 2);
                }

                new_workers = (int)(new_workers * (1f - global_reduction));

                if (new_workers != original_workers)
                {
                    workProvider.m_MaxWorkers = new_workers;
                    workProviderArr[i] = workProvider;

                    float factor = (original_workers > 0) ? new_workers / (float)original_workers : 1f;
                    if (factor == 0f)
                        factor = 1f;

                    if (factor == 1f)
                        continue; // Skip updates if nothing changed

                    RealisticWorkplaceData realisticWorkplaceData = new()
                    {
                        max_workers = new_workers
                    };
                    if(!RealisticWorkplaceDataLookup.HasComponent(entity))
                    {
                        ecb.AddComponent(unfilteredChunkIndex, entity, realisticWorkplaceData);
                    }
                    
                    if (hasBuildingPropertyData)
                    {
                        ecb.SetComponent(unfilteredChunkIndex, prefab2Entity, buildingPropertyData);
                    }

                    if (hasWorkplaceData)
                    {
                        workplaceData.m_MaxWorkers = (int)(workplaceData.m_MaxWorkers * factor);
                        ecb.SetComponent(unfilteredChunkIndex, prefab1Entity, workplaceData);
                    }

                    if (hasServiceCompanyData)
                    {
                        serviceCompanyData.m_WorkPerUnit = (int)(serviceCompanyData.m_WorkPerUnit * factor);
                        ecb.SetComponent(unfilteredChunkIndex, prefab1Entity, serviceCompanyData);
                    }

                    if (hasIndustrialProcessData)
                    {
                        industrialProcessData.m_WorkPerUnit = (int)(industrialProcessData.m_WorkPerUnit * factor);
                        ecb.SetComponent(unfilteredChunkIndex, prefab1Entity, industrialProcessData);
                    }
                }
            }
        }

    }
}