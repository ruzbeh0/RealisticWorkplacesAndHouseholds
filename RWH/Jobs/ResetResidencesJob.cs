//Original code by Trejak from the Building Occupancy Mod
using Game.Agents;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Net;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealisticWorkplacesAndHouseholds.Components;
using Unity.Burst.Intrinsics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds.Jobs
{
    [BurstCompile]
    public partial struct ResetResidencesJob : IJobChunk
    {

        public EntityCommandBuffer ecb;

        public EntityTypeHandle entityTypeHandle;
        public BufferTypeHandle<Renter> renterTypeHandle;
        public ComponentTypeHandle<PrefabRef> prefabRefTypeHandle;
        public ComponentTypeHandle<Building> buildingTypeHandle;

        public ComponentLookup<BuildingData> buildingDataLookup;
        public ComponentLookup<BuildingPropertyData> propertyDataLookup;
        public ComponentLookup<CommercialProperty> commercialPropertyLookup;
        public ComponentLookup<WorkProvider> workProviderLookup;
        public ComponentLookup<PropertyToBeOnMarket> propertyToBeOnMarketLookup;
        public ComponentLookup<PropertyOnMarket> propertyOnMarketLookup;
        public ComponentLookup<ConsumptionData> consumptionDataLookup;
        public ComponentLookup<LandValue> landValueLookup;

        public RandomSeed randomSeed;
        public ResetType resetType;
        public EntityArchetype m_RentEventArchetype;
        public NativeList<Entity> evictedList;
        public NativeQueue<RentAction> rentQueue;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityTypeHandle);
            var renterAccessor = chunk.GetBufferAccessor(ref renterTypeHandle);
            var prefabRefs = chunk.GetNativeArray(ref prefabRefTypeHandle);
            var buildings = chunk.GetNativeArray(ref buildingTypeHandle);

            var random = randomSeed.GetRandom(1);

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
                int householdsCount;
                bool isCommercialOffice = commercialPropertyLookup.HasComponent(entity);   // TODO: Check for office as well                 
                if (isCommercialOffice)
                {
                    // TODO: change to counting the number of commercial properties instead of -1 after implementing multi-tenant commercial/office
                    householdsCount = renters.Length - 1;
                }
                else
                {
                    householdsCount = renters.Length;
                }

                // Too many households
                if (householdsCount > propertyData.m_ResidentialProperties)
                {
                    RemoveHouseholds(householdsCount - propertyData.m_ResidentialProperties, entity, renters, ref random);
                    if (resetType == ResetType.FindNewHome)
                    {
                        Entity e = ecb.CreateEntity(this.m_RentEventArchetype);
                        ecb.SetComponent(e, new RentersUpdated(entity));
                    }
                }
                else if (householdsCount < propertyData.m_ResidentialProperties && propertyOnMarketLookup.TryGetComponent(entity, out var onMarketInfo))
                {
                    // Doesn't seem to be working how I'd like. Just gonna skip this for now.
                    // Only run this if the "Relocate Evictions" setting is toggled.
                    //if (evictedList.Length > 0)
                    //{
                    //    var delta = propertyData.m_ResidentialProperties - householdsCount;
                    //    while (delta > 0 && evictedList.Length > 0)
                    //    {
                    //        var tenant = evictedList[0];
                    //        //renters.Add(new Renter() { m_Renter = tenant });
                    //        //ecb.RemoveComponent<PropertySeeker>(tenant);
                    //        // TODO: Add to list for the RentJob
                    //        rentQueue.Enqueue(new PropertyUtils.RentAction()
                    //        {
                    //            m_Property = entity,
                    //            m_Renter = tenant
                    //        });
                    //        ecb.RemoveComponent<PropertySeeker>(tenant);
                    //        evictedList.RemoveAt(0);
                    //        delta--;
                    //    }                        
                    //}
                    Entity e = ecb.CreateEntity(this.m_RentEventArchetype);
                    ecb.SetComponent(e, new RentersUpdated(entity));
                }
            }
        }

        private void RemoveHouseholds(int extraHouseholds, Entity property, DynamicBuffer<Renter> renters, ref Unity.Mathematics.Random random)
        {
            //NativeHashSet<Entity> marked = new NativeHashSet<Entity>(extraHouseholds, Allocator.Temp);
            for (int i = 0; i < extraHouseholds && extraHouseholds > 0; i++)
            {
                // was while(extraHouseholds > 0) but that might take too long if the set already contains it
                //var entity = renters[random.NextInt(0,renters.Length)].m_Renter; // remove a random household so the newer ones aren't always removed
                var entity = renters[i].m_Renter;
                if (workProviderLookup.HasComponent(entity)) continue;
                switch (resetType)
                {
                    case ResetType.Delete:
                        ecb.AddComponent<Deleted>(entity);
                        break;
                    case ResetType.FindNewHome:
                        ecb.AddComponent(entity, new PropertySeeker()
                        {
                            m_BestProperty = default(Entity),
                            m_BestPropertyScore = float.NegativeInfinity
                        }); 
                        ecb.RemoveComponent<PropertyRenter>(entity);
                        evictedList.Add(entity);
                        ecb.AddComponent(entity, new Evicted() { from = property });
                        break;
                    default:
                        throw new System.Exception($"Invalid ResetType provided: \"{resetType}\"!");
                }
            }
        }
    }
}
