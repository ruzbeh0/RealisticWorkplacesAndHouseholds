
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
    public partial class DeleteHomelessSystem : GameSystemBase
    {


        EntityQuery m_TriggerQuery;
        SimulationSystem m_SimulationSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        EntityQuery m_HomelessHouseholdsQuery;

        public static bool delete { get; private set; }

        public static void TriggerDelete()
        {            
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var trigger = new DeleteHomelessTrigger();            
            em.CreateSingleton(trigger);
        }        

        protected override void OnCreate()
        {
            base.OnCreate();
            delete = false;
            m_HomelessHouseholdsQuery = GetEntityQuery(
                ComponentType.ReadWrite<Household>(),
                ComponentType.ReadOnly<HouseholdCitizen>(),
                ComponentType.Exclude<PropertyRenter>(),
                ComponentType.Exclude<TouristHousehold>(),
                ComponentType.Exclude<CommuterHousehold>(),
                ComponentType.Exclude<CurrentBuilding>(),
                ComponentType.Exclude<PropertySeeker>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>()
            );

            this.m_TriggerQuery = GetEntityQuery(ComponentType.ReadWrite<DeleteHomelessTrigger>());

            this.m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_EndFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>(); 

            this.RequireForUpdate(m_TriggerQuery);
            this.RequireForUpdate(m_HomelessHouseholdsQuery);
        }

        protected override void OnUpdate()
        {
            var trigger = SystemAPI.GetSingleton<ResetHouseholdsTrigger>();

            

            var temp = m_HomelessHouseholdsQuery.ToEntityArray(Allocator.Temp);
            int householdsCount = temp.Length;
            Mod.log.Info("Deleting " +  householdsCount + " homeless households");
            temp.Dispose();
            var ecb = m_EndFrameBarrier.CreateCommandBuffer();

            //var resetResidencesJob = new ResetResidencesJob()
            //{
            //    ecb = ecb,
            //    commercialPropertyLookup = SystemAPI.GetComponentLookup<CommercialProperty>(true),
            //    entityTypeHandle = SystemAPI.GetEntityTypeHandle(),
            //    prefabRefTypeHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
            //    buildingTypeHandle = SystemAPI.GetComponentTypeHandle<Building>(true),
            //    propertyDataLookup = SystemAPI.GetComponentLookup<BuildingPropertyData>(true),
            //    randomSeed = RandomSeed.Next(),
            //    renterTypeHandle = SystemAPI.GetBufferTypeHandle<Renter>(false),
            //    workProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(true),
            //    resetType = trigger.resetType,
            //    propertyToBeOnMarketLookup = SystemAPI.GetComponentLookup<PropertyToBeOnMarket>(false),
            //    propertyOnMarketLookup = SystemAPI.GetComponentLookup<PropertyOnMarket>(true),
            //    consumptionDataLookup = SystemAPI.GetComponentLookup<ConsumptionData>(true),
            //    landValueLookup = SystemAPI.GetComponentLookup<LandValue>(true),
            //    buildingDataLookup = SystemAPI.GetComponentLookup<BuildingData>(true),
            //    evictedList = evictedHouseholds,
            //    m_RentEventArchetype = m_RentEventArchetype,
            //    rentQueue = rentQueue                
            //};            
            //EntityManager.DestroyEntity(m_TriggerQuery.GetSingletonEntity());
            //JobHandle resetHandle = resetResidencesJob.Schedule(m_BuildingsQuery, this.Dependency);            
            //
            //
            //cityStatsSystem.AddWriter(this.Dependency);
            //triggerSystem.AddActionBufferWriter(this.Dependency);
            //this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }       
    }
}
