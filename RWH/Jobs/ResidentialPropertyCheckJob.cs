//Original code by Trejak from the Building Occupancy Mod
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealisticWorkplacesAndHouseholds.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using static Game.Rendering.Utilities.State;
using Game.Agents;
using Game.Rendering;
using Game.Citizens;

namespace RealisticWorkplacesAndHouseholds.Jobs
{
    [BurstCompile]
    public partial struct ResidentialPropertyCheckJob : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public EconomyParameterData economyParameterData;

        public EntityTypeHandle entityTypeHandle;        
        public BufferTypeHandle<Renter> renterTypeHandle;
        public ComponentTypeHandle<PrefabRef> prefabRefTypeHandle;
        public ComponentTypeHandle<Building> buildingTypeHandle;

        public ComponentLookup<BuildingData> buildingDataLookup;
        public ComponentLookup<BuildingPropertyData> propertyDataLookup;
        public ComponentLookup<CommercialProperty> commercialPropertyLookup;
        public ComponentLookup<PropertyToBeOnMarket> propertyToBeOnMarketLookup;
        public ComponentLookup<PropertyOnMarket> propertyOnMarketLookup;
        public ComponentLookup<ConsumptionData> consumptionDataLookup;
        public ComponentLookup<LandValue> landValueLookup;
        public ComponentLookup<SpawnableBuildingData> spawnableBuildingDataLookup;
        public ComponentLookup<ZoneData> zoneDataLookup;
        public ComponentLookup<WorkProvider> workProviderLookup;
        public ComponentLookup<PropertyRenter> propertyRenterLookup;
        public ComponentLookup<PropertySeeker> propertySeekerLookup;
        public EntityArchetype m_RentEventArchetype;
        public ResetType resetType = Mod.m_Setting.evicted_reset_type;
        public Unity.Mathematics.Random random;
        public BufferLookup<HouseholdCitizen> m_CitizenBufs;
        public ComponentLookup<HealthProblem> m_HealthProblems;
        public bool allowEvictions;
        public int maxEvictionsPerBuilding;


        public ResidentialPropertyCheckJob()
        {
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityTypeHandle);
            var renterAccessor = chunk.GetBufferAccessor(ref renterTypeHandle);
            var prefabRefs = chunk.GetNativeArray(ref prefabRefTypeHandle);
            var buildings = chunk.GetNativeArray(ref buildingTypeHandle);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var prefabRef = prefabRefs[i];
                var renters = renterAccessor[i];
                var building = buildings[i];
                if (!propertyDataLookup.TryGetComponent(prefabRef.m_Prefab, out var propertyData))
                {
                    return;
                }
                int householdsCount = CountBufferedHouseholdRenters(entity, renters);
                if (householdsCount > propertyData.m_ResidentialProperties)
                    householdsCount = CountActiveHouseholdRenters(entity, renters);

                if (householdsCount < propertyData.m_ResidentialProperties)
                {

                    if (!propertyOnMarketLookup.HasComponent(entity))
                    {
                        Entity roadEdge = building.m_RoadEdge;
                        BuildingData buildingData = buildingDataLookup[prefabRef.m_Prefab];
                        int lotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
                        float landValue = 0;
                        if (landValueLookup.HasComponent(roadEdge))
                        {
                            landValue = lotSize * landValueLookup[roadEdge].m_LandValue;
                        }
                        var areaType = Game.Zones.AreaType.None;
                        if (this.spawnableBuildingDataLookup.TryGetComponent(prefabRef.m_Prefab, out var spawnableBldgData))
                        {
                            areaType = zoneDataLookup[spawnableBldgData.m_ZonePrefab].m_AreaType;
                        }
                        var consumptionData = consumptionDataLookup[prefabRef.m_Prefab];
                        var buildingLevel = PropertyUtils.GetBuildingLevel(prefabRef.m_Prefab, spawnableBuildingDataLookup);
                        var askingRent = PropertyUtils.GetRentPricePerRenter(propertyData, buildingLevel, lotSize, landValue, areaType, ref this.economyParameterData);
                        ecb.AddComponent(unfilteredChunkIndex, entity, new PropertyOnMarket { m_AskingRent = askingRent });
                    }
                    else
                    {

                    }
                }
                else if (allowEvictions && householdsCount > propertyData.m_ResidentialProperties)
                {
                    int removed = RemoveHouseholds(
                        Math.Min(householdsCount - propertyData.m_ResidentialProperties, Math.Max(1, maxEvictionsPerBuilding)),
                        entity,
                        renters,
                        unfilteredChunkIndex);

                    if (removed > 0 && resetType == ResetType.FindNewHome)
                    {
                        Entity e = ecb.CreateEntity(unfilteredChunkIndex, this.m_RentEventArchetype);
                        ecb.SetComponent(unfilteredChunkIndex, e, new RentersUpdated(entity));
                    }
                }
                else if (householdsCount == propertyData.m_ResidentialProperties && propertyToBeOnMarketLookup.HasComponent(entity))
                {
                    ecb.RemoveComponent<PropertyToBeOnMarket>(unfilteredChunkIndex, entity);
                }
            }
        }

        private int CountBufferedHouseholdRenters(Entity property, DynamicBuffer<Renter> renters)
        {
            int householdsCount = renters.Length;
            if (commercialPropertyLookup.HasComponent(property))
                householdsCount--;

            return Math.Max(0, householdsCount);
        }

        private int CountActiveHouseholdRenters(Entity property, DynamicBuffer<Renter> renters)
        {
            int householdsCount = 0;

            for (int i = 0; i < renters.Length; i++)
            {
                Entity renter = renters[i].m_Renter;
                if (!m_CitizenBufs.HasBuffer(renter))
                    continue;

                if (!propertyRenterLookup.TryGetComponent(renter, out PropertyRenter propertyRenter) ||
                    propertyRenter.m_Property != property)
                    continue;

                householdsCount++;
            }

            return householdsCount;
        }

        private void RemoveHousehold(Entity property, Renter renter, ResetType reset, int unfilteredChunkIndex)
        {
            var entity = renter;
            switch (reset)
            {
                case ResetType.Delete:
                    ecb.AddComponent<Deleted>(unfilteredChunkIndex, entity);
                    break;
                case ResetType.FindNewHome:
                    SetFindNewHome(property, entity, unfilteredChunkIndex);
                    //evictedList.Add(entity);
                    break;
                default:
                    throw new System.Exception($"Invalid ResetType provided: \"{resetType}\"!");
            }
        }

        private int RemoveHouseholds(int extraHouseholds, Entity property, DynamicBuffer<Renter> renters, int unfilteredChunkIndex)
        {
            //NativeHashSet<Entity> marked = new NativeHashSet<Entity>(extraHouseholds, Allocator.Temp);
            int removed = 0;
            for (int i = 0; i < renters.Length && removed < extraHouseholds; i++)
            {
                // was while(extraHouseholds > 0) but that might take too long if the set already contains it
                //var entity = renters[random.NextInt(0,renters.Length)].m_Renter; // remove a random household so the newer ones aren't always removed
                var entity = renters[i].m_Renter;
                if (workProviderLookup.HasComponent(entity)) continue;
                if (!m_CitizenBufs.HasBuffer(entity)) continue;
                if (!propertyRenterLookup.TryGetComponent(entity, out PropertyRenter propertyRenter) ||
                    propertyRenter.m_Property != property)
                    continue;

                switch (resetType)
                {
                    case ResetType.Delete:
                        ecb.AddComponent<Deleted>(unfilteredChunkIndex, entity);
                        removed++;
                        break;
                    case ResetType.FindNewHome:
                        SetFindNewHome(property, entity, unfilteredChunkIndex);
                        //evictedList.Add(entity);
                        removed++;
                        break;
                    default:
                        throw new System.Exception($"Invalid ResetType provided: \"{resetType}\"!");
                }
            }

            return removed;
        }

        private void SetFindNewHome(Entity property, Entity renter, int unfilteredChunkIndex)
        {
            PropertySeeker seeker = new PropertySeeker()
            {
                m_BestProperty = default(Entity),
                m_BestPropertyScore = float.NegativeInfinity
            };

            if (propertySeekerLookup.HasComponent(renter))
            {
                ecb.SetComponent(unfilteredChunkIndex, renter, seeker);
                ecb.SetComponentEnabled<PropertySeeker>(unfilteredChunkIndex, renter, true);
            }
            else
            {
                ecb.AddComponent(unfilteredChunkIndex, renter, seeker);
            }

            ecb.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, renter);
            ecb.AddComponent(unfilteredChunkIndex, renter, new Evicted() { from = property });
        }
    }
}
