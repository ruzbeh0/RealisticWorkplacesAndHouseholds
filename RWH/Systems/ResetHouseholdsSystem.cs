//Original code by Trejak from the Building Occupancy Mod
using Game;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Triggers;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Jobs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    public partial class ResetHouseholdsSystem : GameSystemBase
    {


        EntityQuery m_TriggerQuery;
        SimulationSystem m_SimulationSystem;
        HouseholdFindPropertySystem m_householdFindPropertySystem; 
        private EndFrameBarrier m_EndFrameBarrier;
        EntityQuery m_HouseholdsQuery;
        EntityQuery m_HomelessHouseholdsQuery;
        EntityQuery m_BuildingsQuery;
        EntityArchetype m_RentEventArchetype;

        public static bool reset { get; private set; }

        public static void TriggerReset(ResetType resetType)
        {            
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var trigger = new ResetHouseholdsTrigger()
            {
                resetType = resetType
            };            
            em.CreateSingleton(trigger);
        }        

        protected override void OnCreate()
        {
            m_BuildingsQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<ResidentialProperty>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<Renter>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<PropertyOnMarket>()
            );

            base.OnCreate();
            reset = false;
            m_HouseholdsQuery = GetEntityQuery(            
                ComponentType.ReadWrite<Household>(),                
                ComponentType.ReadOnly<HouseholdCitizen>(),
                ComponentType.ReadOnly<PropertyRenter>(),
                ComponentType.Exclude<TouristHousehold>(),
                ComponentType.Exclude<CommuterHousehold>(),
                ComponentType.Exclude<CurrentBuilding>(),
                ComponentType.Exclude<PropertySeeker>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>()
            );

            m_BuildingsQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<ResidentialProperty>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<Renter>(),                
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>()
            );
            this.m_TriggerQuery = GetEntityQuery(ComponentType.ReadWrite<ResetHouseholdsTrigger>());

            m_RentEventArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<RentersUpdated>());
            m_householdFindPropertySystem = World.GetOrCreateSystemManaged<HouseholdFindPropertySystem>();
            this.m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_EndFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>(); 

            this.RequireForUpdate(m_TriggerQuery);
            this.RequireForUpdate(m_HouseholdsQuery);
            this.RequireForUpdate(m_BuildingsQuery);
        }

        protected override void OnUpdate()
        {
            var trigger = SystemAPI.GetSingleton<ResetHouseholdsTrigger>();

            

            var temp = m_HouseholdsQuery.ToEntityArray(Allocator.Temp);
            int householdsCount = temp.Length;
            Mod.log.Info("Scheduling household reset of type " + trigger.ToString() + " Household count:" + householdsCount);
            temp.Dispose();
            NativeList<Entity> evictedHouseholds = new NativeList<Entity>(householdsCount/2, Allocator.TempJob);
            var ecb = m_EndFrameBarrier.CreateCommandBuffer();
            NativeQueue<RentAction> rentQueue = new NativeQueue<RentAction>(Allocator.TempJob);

            var resetResidencesJob = new ResetResidencesJob()
            {
                ecb = ecb,
                commercialPropertyLookup = SystemAPI.GetComponentLookup<CommercialProperty>(true),
                entityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                prefabRefTypeHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                buildingTypeHandle = SystemAPI.GetComponentTypeHandle<Building>(true),
                propertyDataLookup = SystemAPI.GetComponentLookup<BuildingPropertyData>(true),
                randomSeed = RandomSeed.Next(),
                renterTypeHandle = SystemAPI.GetBufferTypeHandle<Renter>(false),
                workProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(true),
                resetType = trigger.resetType,
                propertyToBeOnMarketLookup = SystemAPI.GetComponentLookup<PropertyToBeOnMarket>(false),
                propertyOnMarketLookup = SystemAPI.GetComponentLookup<PropertyOnMarket>(true),
                consumptionDataLookup = SystemAPI.GetComponentLookup<ConsumptionData>(true),
                landValueLookup = SystemAPI.GetComponentLookup<LandValue>(true),
                buildingDataLookup = SystemAPI.GetComponentLookup<BuildingData>(true),
                evictedList = evictedHouseholds,
                m_RentEventArchetype = m_RentEventArchetype,
                rentQueue = rentQueue                
            };            
            EntityManager.DestroyEntity(m_TriggerQuery.GetSingletonEntity());
            JobHandle resetHandle = resetResidencesJob.Schedule(m_BuildingsQuery, this.Dependency);            

            var cityStatsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            var triggerSystem = World.GetOrCreateSystemManaged<TriggerSystem>();
            cityStatsSystem.AddWriter(this.Dependency);
            triggerSystem.AddActionBufferWriter(this.Dependency);
            this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }       
    }
}
