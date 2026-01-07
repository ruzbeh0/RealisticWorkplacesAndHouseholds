
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
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
using static Game.Rendering.OverlayRenderSystem;

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
                        ComponentType.ReadOnly<GroupAmbienceData>(),
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
        public ComponentLookup<RealisticWorkplacesAndHouseholds.Components.UsableFootprintFactor> UffLookup;

        public EntityTypeHandle EntityTypeHandle;
        const float HALLWAY_BUFFER = 1f; // 1 sq metres of space in front of the unit's door

        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly]
        public bool reset;
        [ReadOnly]
        public ComponentLookup<RealisticHouseholdData> RealisticHouseholdDataLookup;

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
        public ComponentLookup<PrefabData> PrefabDataLookup;
        [ReadOnly]
        public BufferLookup<AssetPackElement> AssetPackElementBufferLookup;
        [ReadOnly]
        public ComponentLookup<AssetPackData> AssetPackDataLookup;
        [ReadOnly]
        public ComponentTypeHandle<GroupAmbienceData> GroupAmbienceDataHandle;
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
        [ReadOnly]
        public float lv4_increase;
        [ReadOnly]
        public float lv5_increase;
        [ReadOnly]
        public float hallway_pct;
        [ReadOnly]
        public float global_reduction;
        [ReadOnly]
        public int sqm_per_apartment_lowdensity;
        [ReadOnly]
        public bool enable_rh_apt_per_floor;
        [ReadOnly] public NativeParallelHashMap<Entity, FixedString64Bytes> PrefabNameMap;
        [ReadOnly] public NativeParallelHashMap<Entity, float3> AssetPackFactorsLookup;
        [ReadOnly]
        public ComponentLookup<RowHomeLike> RowHomeLikeLookup;



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
            var groupAmbienceDataArr = chunk.GetNativeArray(ref GroupAmbienceDataHandle);
            var entityArr = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < buildingDataArr.Length; i++)
            {
                SpawnableBuildingData spawnBuildingData = spawnBuildingDataArr[i];
                BuildingPropertyData property = buildingPropertyDataArr[i];
                GroupAmbienceData group = groupAmbienceDataArr[i];
                DynamicBuffer<SubMesh> subMeshes = subMeshBufferAccessor[i];
                BuildingData buildingData = buildingDataArr[i];
                Entity entity = entityArr[i];

                if (spawnBuildingData.m_ZonePrefab != Entity.Null)
                {
                    if (ZoneDataLookup.TryGetComponent(spawnBuildingData.m_ZonePrefab, out var zonedata))
                    {
                        if (zonedata.m_AreaType == Game.Zones.AreaType.Residential)
                        {
                            float3 asset_pack_factor = 1f;
                            //string asset_pack_names = "";
                            if (AssetPackElementBufferLookup.TryGetBuffer(entity, out var assetPackElements1))
                            {
                                for (int index = 0; index < assetPackElements1.Length; ++index)
                                {
                                    Entity pack = assetPackElements1[index].m_Pack;
                                    if (AssetPackDataLookup.HasComponent(pack))
                                    {
                                        if (PrefabDataLookup.TryGetComponent(pack, out PrefabData prefabData))
                                        {
                                            if (AssetPackFactorsLookup.TryGetValue(pack, out var asset_factor))
                                            {
                                                asset_pack_factor *= asset_factor;
                                                //asset_pack_names = asset_pack_names + PrefabNameMap[pack].ToString() + " ";
                                            }
                                        }
                                    }
                                }    
                            } else
                            {
                                if (AssetPackElementBufferLookup.TryGetBuffer(spawnBuildingData.m_ZonePrefab, out var assetPackElements2))
                                {
                                    for (int index = 0; index < assetPackElements2.Length; ++index)
                                    {
                                        Entity pack = assetPackElements2[index].m_Pack;
                                        if (AssetPackDataLookup.HasComponent(pack))
                                        {
                                            if (PrefabDataLookup.TryGetComponent(pack, out PrefabData prefabData))
                                            {
                                                if (AssetPackFactorsLookup.TryGetValue(pack, out var asset_factor))
                                                {
                                                    asset_pack_factor *= asset_factor;
                                                    //asset_pack_names = asset_pack_names + PrefabNameMap[pack].ToString() + " ";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                            var size = ObjectUtils.GetSize(dimensions);
                            float width = size.x;
                            float length = size.z;
                            float height = size.y;
                            int households = property.m_ResidentialProperties;
                            float lotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
                            float uff = UffLookup.HasComponent(entity) ? UffLookup[entity].Value : 1f;

                            int original_households = property.m_ResidentialProperties;

                            uff = Math.Max(uff, 0.75f);
                            width *= (float)Math.Sqrt(uff);
                            length *= (float)Math.Sqrt(uff);

                            //Check if it is a row home
                            bool isRowHome =
                                ((zonedata.m_ZoneFlags & ZoneFlags.SupportNarrow) == ZoneFlags.SupportNarrow);
                            if(asset_pack_factor.y != 1f)
                                isRowHome = RowHomeLikeLookup.HasComponent(entity);
                            if (isRowHome && height / residential_avg_floor_height < 5)
                            {
                                if (rowhome_basement)
                                {
                                    height += residential_avg_floor_height;
                                }

                                float rowhome_area = sqm_per_apartment * (1 + hallway_pct);
                                if (enable_rh_apt_per_floor)
                                {
                                    rowhome_area = width * length / rowhome_apt_per_floor + HALLWAY_BUFFER;
                                }

                                uff = Math.Max(uff, 0.8f);
                                width *= (float)Math.Sqrt(uff);
                                length *= (float)Math.Sqrt(uff);
                                households = BuildingUtils.GetPeople(true, width, length, height, residential_avg_floor_height, rowhome_area, 0, 0);
                                //households = (int)(households * asset_pack_factor.y);
                            } else
                            {
                                //Checking for single family homes
                                if(!single_family || property.m_ResidentialProperties > 1)
                                {
                                    int floorOffset = 0;
                                    //Adding hallway area to apt area
                                    float apt_area = sqm_per_apartment*(1+hallway_pct);
                                    if (luxury_highrise_less_apt && group.m_AmbienceType != GroupAmbienceType.ResidentialLowRent)
                                    {
                                        //High rise buildings that are level 4 or 5 will have less apartments
                                        if (spawnBuildingData.m_Level == 4) 
                                        {
                                            apt_area *= (1 + lv4_increase);
                                        }
                                        if (spawnBuildingData.m_Level == 5)
                                        {
                                            apt_area *= (1 + lv5_increase);
                                        }
                                    }

                                    //Checking if it is Mixed Use
                                    if(property.m_AllowedSold != Resource.NoResource)
                                    {
                                        //remove one floor
                                        floorOffset++;
                                    }

                                    //If less than 5 floors, assuming low density
                                    if (height/ residential_avg_floor_height < 5)
                                    {
                                        households = BuildingUtils.GetPeople(true, width, length, height, residential_avg_floor_height, sqm_per_apartment_lowdensity * (1 + hallway_pct), 0, 0);
                                        households = (int)(households*asset_pack_factor.x);
                                    } else
                                    {
                                        households = BuildingUtils.GetPeople(true, width, length, height, residential_avg_floor_height, apt_area, floorOffset, units_per_elevator * (int)sqm_per_apartment);
                                        households = (int)(households * asset_pack_factor.z);
                                    }
                                }
                                //Set low density households to 1 if they are higher than that and single family option is true
                                if (single_family && property.m_ResidentialProperties > 1 &&
                                    (zonedata.m_ZoneFlags & ZoneFlags.SupportNarrow) != ZoneFlags.SupportNarrow)
                                {
                                    //If less than 3 floors and at least 20% of the lot area is free area, assuming low density
                                    if (height / residential_avg_floor_height < 3 && width*length/lotSize < 0.8f)
                                    {
                                        households = 1;
                                    } 
                                }
                            }

                            //Apply global reduction factor
                            property.m_ResidentialProperties = Math.Max(1,(int)(households * (1f - global_reduction)));
                            float factor = 1f;
                            if (original_households > 0)
                            {
                                factor = property.m_ResidentialProperties / (float)original_households;
                            }
                            if (factor == 0f)
                            {
                                factor = 1f;
                            }
                            if(property.m_SpaceMultiplier == 0)
                            {
                                property.m_SpaceMultiplier = 1;
                            }
                            //Mod.log.Info($"Households updated for building {entity.Index} from {original_households} to {property.m_ResidentialProperties} (factor {factor:0.###}), uff:{uff}, rowhome:{isRowHome}, asset_pack_factor:{asset_pack_factor}, floors:{height / residential_avg_floor_height}, asset_pack_names:{asset_pack_names}");
                            property.m_SpaceMultiplier /= factor;
                            
                            buildingPropertyDataArr[i] = property;
                            if (!RealisticHouseholdDataLookup.HasComponent(entity))
                            {
                                RealisticHouseholdData realisticHouseholdData = new()
                                {
                                    households = property.m_ResidentialProperties
                                };
                                ecb.AddComponent<RealisticHouseholdData>(unfilteredChunkIndex, entity, realisticHouseholdData);
                            }      

                        }

                    }
                }
 
            }
        }
    }
}