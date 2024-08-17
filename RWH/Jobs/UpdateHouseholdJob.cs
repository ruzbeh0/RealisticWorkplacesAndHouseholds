﻿
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
using static Game.Input.InputRecorder;
using System;
using Game.Economy;

namespace RealisticWorkplacesAndHouseholds.Jobs
{

    public struct UpdateHouseholdJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdateHouseholdJobQuery()
        {
            Query =
            [
                new()
                {
                    All =
                    [
                        ComponentType.ReadOnly<PrefabData>(),
                        ComponentType.ReadOnly<BuildingData>(),
                        ComponentType.ReadOnly<BuildingPropertyData>(),
                        ComponentType.ReadWrite<SpawnableBuildingData>(),
                        ComponentType.ReadWrite<SubMesh>(),
                    ],
                    Any = [
                    ],
                    None =
                    [
                        ComponentType.Exclude<RealisticHouseholdData>(),
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
                }
            ];
        }
    }

    [BurstCompile]
    public struct UpdateHouseholdJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;
        const float HALLWAY_BUFFER = 1f; // 1 sq metres of space in front of the unit's door

        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly]
        public ComponentTypeHandle<BuildingData> BuildingDataHandle;
        public ComponentTypeHandle<BuildingPropertyData> BuildingPropertyDataHandle;
        [ReadOnly]
        public BufferTypeHandle<SubMesh> SubMeshHandle;
        [ReadOnly]
        public ComponentLookup<MeshData> meshDataLookup;
        [ReadOnly]
        public ComponentTypeHandle<SpawnableBuildingData> SpawnableBuildingHandle;
        [ReadOnly]
        public ComponentLookup<ZoneData> ZoneDataLookup;
        [ReadOnly]
        public float sqm_per_apartment;
        [ReadOnly]
        public float residential_avg_floor_height;
        [ReadOnly]
        public float rowhome_apt_per_floor;
        [ReadOnly]
        public bool rowhome_basement;
        [ReadOnly]
        public int units_per_elevator;
        [ReadOnly]
        public bool single_family;
        [ReadOnly]
        public bool luxury_highrise_less_apt;

        public UpdateHouseholdJob()
        {

        }

        public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            bool useEnabledMask,
            in v128 chunkEnabledMask)
        {

            var spawnBuildingDataArr = chunk.GetNativeArray(ref SpawnableBuildingHandle);
            var buildingDataArr = chunk.GetNativeArray(ref BuildingDataHandle);
            var subMeshBufferAccessor = chunk.GetBufferAccessor(ref SubMeshHandle);
            var buildingPropertyDataArr = chunk.GetNativeArray(ref BuildingPropertyDataHandle);
            var entityArr = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < buildingDataArr.Length; i++)
            {
                SpawnableBuildingData spawnBuildingData = spawnBuildingDataArr[i];
                BuildingPropertyData property = buildingPropertyDataArr[i];
                DynamicBuffer<SubMesh> subMeshes = subMeshBufferAccessor[i];
                Entity entity = entityArr[i];
                if (spawnBuildingData.m_ZonePrefab != Entity.Null)
                {
                    if (ZoneDataLookup.TryGetComponent(spawnBuildingData.m_ZonePrefab, out var zonedata))
                    {
                        if(zonedata.m_AreaType == Game.Zones.AreaType.Residential)
                        {
                            var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                            var size = ObjectUtils.GetSize(dimensions);
                            float width = size.x;
                            float length = size.z;
                            float height = size.y;

                            //Check if it is a row home
                            if((zonedata.m_ZoneFlags & ZoneFlags.SupportNarrow) == ZoneFlags.SupportNarrow)
                            {
                                if (rowhome_basement)
                                {
                                    height += residential_avg_floor_height;
                                }
                                property.m_ResidentialProperties = BuildingUtils.GetPeople(true, width, length, height, residential_avg_floor_height, width*length/ rowhome_apt_per_floor + HALLWAY_BUFFER, 0, 0);
                            } else
                            {
                                //Checking for single family homes
                                if(!single_family || property.m_ResidentialProperties > 1)
                                {
                                    int floorOffset = 0;
                                    if(luxury_highrise_less_apt && height/residential_avg_floor_height > 10 && spawnBuildingData.m_Level >= 4)
                                    {
                                        //High rise buildings that are level 4 or 5 will have less apartments
                                        //Removing a floor every 15 floors
                                        floorOffset = (int)(height / residential_avg_floor_height) / 15;
                                    }

                                    //Checking if it is Mixed Use
                                    if(property.m_AllowedSold != Resource.NoResource)
                                    {
                                        //remove one floor
                                        floorOffset++;
                                    }
                                    property.m_ResidentialProperties = BuildingUtils.GetPeople(true, width, length, height, residential_avg_floor_height, sqm_per_apartment + HALLWAY_BUFFER, floorOffset, units_per_elevator * (int)sqm_per_apartment);
                                } 
                            }
                            
                            //Mod.log.Info($"Residential: x{width},y{length},z{height},zoneflag{zonedata.m_ZoneFlags}, Households:{property.m_ResidentialProperties}, Sold:{property.m_AllowedSold}");

                            buildingPropertyDataArr[i] = property;
                            RealisticHouseholdData realisticHouseholdData = new();
                            realisticHouseholdData.households = property.m_ResidentialProperties;
                            ecb.AddComponent(unfilteredChunkIndex, entity, realisticHouseholdData);
                        }
                        
                    }
                }
 
            }
        }
    }
}