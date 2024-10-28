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
            NativeQueue<PropertyUtils.RentAction> rentQueue = new NativeQueue<PropertyUtils.RentAction>(Allocator.TempJob);

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

            JobHandle statsEventQueueHandle;
            NativeList<Entity> reservedProperties = new NativeList<Entity>(Allocator.TempJob);
            var cityStatsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            var triggerSystem = World.GetOrCreateSystemManaged<TriggerSystem>();
            PropertyUtils.RentJob rentJob = new PropertyUtils.RentJob()
            {
                m_RentEventArchetype = m_RentEventArchetype,
                m_PropertiesOnMarket = SystemAPI.GetComponentLookup<PropertyOnMarket>(true),
                m_Renters = SystemAPI.GetBufferLookup<Renter>(false),
                m_BuildingPropertyDatas = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup,
                m_ParkDatas = SystemAPI.GetComponentLookup<ParkData>(true),
                m_PrefabRefs = SystemAPI.GetComponentLookup<PrefabRef>(true),
                m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(false),
                m_Companies = SystemAPI.GetComponentLookup<CompanyData>(true),
                m_Households = SystemAPI.GetComponentLookup<Household>(true),
                m_Industrials = SystemAPI.GetComponentLookup<IndustrialCompany>(true),
                m_Commercials = SystemAPI.GetComponentLookup<CommercialCompany>(true),
                m_BuildingDatas = SystemAPI.GetComponentLookup<BuildingData>(true),
                m_ServiceCompanyDatas = SystemAPI.GetComponentLookup<ServiceCompanyData>(true),
                m_IndustrialProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true),
                m_WorkProviders = SystemAPI.GetComponentLookup<WorkProvider>(false),                
                m_HouseholdCitizens = SystemAPI.GetBufferLookup<HouseholdCitizen>(true),
                m_Abandoneds = SystemAPI.GetComponentLookup<Abandoned>(true),
                m_HomelessHouseholds = SystemAPI.GetComponentLookup<HomelessHousehold>(true),
                m_Parks = SystemAPI.GetComponentLookup<Game.Buildings.Park>(true),
                m_Employees = SystemAPI.GetBufferLookup<Employee>(true),
                m_SpawnableBuildingDatas = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true),
                m_Attacheds = SystemAPI.GetComponentLookup<Attached>(true),
                m_ExtractorCompanyDatas = SystemAPI.GetComponentLookup<ExtractorCompanyData>(true),
                m_SubAreaBufs = SystemAPI.GetBufferLookup<Game.Areas.SubArea>(true),
                m_Geometries = SystemAPI.GetComponentLookup<Geometry>(true),
                m_Lots = SystemAPI.GetComponentLookup<Game.Areas.Lot>(true),
                m_ResourcePrefabs = World.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs(),
                m_Resources = SystemAPI.GetComponentLookup<Game.Prefabs.ResourceData>(true),
                m_StatisticsQueue = cityStatsSystem.GetStatisticsEventQueue(out statsEventQueueHandle),
                m_TriggerQueue = triggerSystem.CreateActionBuffer(),
                m_AreaType = Game.Zones.AreaType.Residential,
                m_CommandBuffer = ecb,
                m_RentQueue = rentQueue,
                m_ReservedProperties = reservedProperties,
                m_DebugDisableHomeless = m_householdFindPropertySystem.debugDisableHomeless                
            };
            this.Dependency = rentJob.Schedule(JobHandle.CombineDependencies(statsEventQueueHandle, resetHandle));
            cityStatsSystem.AddWriter(this.Dependency);
            triggerSystem.AddActionBufferWriter(this.Dependency);
            this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }       
    }
}
