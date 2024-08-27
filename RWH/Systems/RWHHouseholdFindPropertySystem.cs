
using Colossal.Entities;
using Game;
using Game.Simulation;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using System;

#nullable disable
namespace RealisticWorkplacesAndHouseholds.Systems
{
    public partial class RWHHouseholdFindPropertySystem : GameSystemBase
    {
        public bool debugDisableHomeless;
        private const int UPDATE_INTERVAL = 16;
        public static readonly int kMaxProcessEntitiesPerUpdate = 128;
        [DebugWatchValue]
        private DebugWatchDistribution m_DefaultDistribution;
        [DebugWatchValue]
        private DebugWatchDistribution m_EvaluateDistributionLow;
        [DebugWatchValue]
        private DebugWatchDistribution m_EvaluateDistributionMedium;
        [DebugWatchValue]
        private DebugWatchDistribution m_EvaluateDistributionHigh;
        [DebugWatchValue]
        private DebugWatchDistribution m_EvaluateDistributionLowrent;
        private EntityQuery m_HouseholdQuery;
        private EntityQuery m_FreePropertyQuery;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_DemandParameterQuery;
        private EndFrameBarrier m_EndFrameBarrier;
        private PathfindSetupSystem m_PathfindSetupSystem;
        private ResourceSystem m_ResourceSystem;
        private TaxSystem m_TaxSystem;
        private NativeQueue<Game.Buildings.PropertyUtils.RentAction> m_RentQueue;
        private NativeList<Entity> m_ReservedProperties;
        private EntityArchetype m_RentEventArchetype;
        private TriggerSystem m_TriggerSystem;
        private GroundPollutionSystem m_GroundPollutionSystem;
        private AirPollutionSystem m_AirPollutionSystem;
        private NoisePollutionSystem m_NoisePollutionSystem;
        private TelecomCoverageSystem m_TelecomCoverageSystem;
        private CitySystem m_CitySystem;
        private CityStatisticsSystem m_CityStatisticsSystem;
        private CountEmploymentSystem m_CountEmploymentSystem;
        private EntityQuery m_HealthcareParameterQuery;
        private EntityQuery m_ParkParameterQuery;
        private EntityQuery m_EducationParameterQuery;
        private EntityQuery m_TelecomParameterQuery;
        private EntityQuery m_GarbageParameterQuery;
        private EntityQuery m_PoliceParameterQuery;
        private EntityQuery m_CitizenHappinessParameterQuery;
        private RWHHouseholdFindPropertySystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_RentQueue = new NativeQueue<Game.Buildings.PropertyUtils.RentAction>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_ReservedProperties = new NativeList<Entity>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_PathfindSetupSystem = this.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
            this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
            this.m_GroundPollutionSystem = this.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            this.m_AirPollutionSystem = this.World.GetOrCreateSystemManaged<AirPollutionSystem>();
            this.m_NoisePollutionSystem = this.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
            this.m_TelecomCoverageSystem = this.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
            this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_CityStatisticsSystem = this.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            this.m_TaxSystem = this.World.GetOrCreateSystemManaged<TaxSystem>();
            this.m_TriggerSystem = this.World.GetOrCreateSystemManaged<TriggerSystem>();
            this.m_CountEmploymentSystem = this.World.GetOrCreateSystemManaged<CountEmploymentSystem>();
            this.m_RentEventArchetype = this.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<RentersUpdated>());
            this.m_HouseholdQuery = this.GetEntityQuery(ComponentType.ReadWrite<Household>(), ComponentType.ReadWrite<PropertySeeker>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            this.m_DemandParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            this.m_HealthcareParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
            this.m_ParkParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<ParkParameterData>());
            this.m_EducationParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EducationParameterData>());
            this.m_TelecomParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<TelecomParameterData>());
            this.m_GarbageParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
            this.m_PoliceParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
            this.m_CitizenHappinessParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
            this.m_FreePropertyQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1]
              {
          ComponentType.ReadOnly<Building>()
              },
                Any = new ComponentType[2]
              {
          ComponentType.ReadOnly<Abandoned>(),
          ComponentType.ReadOnly<Game.Buildings.Park>()
              },
                None = new ComponentType[3]
              {
          ComponentType.ReadOnly<Deleted>(),
          ComponentType.ReadOnly<Destroyed>(),
          ComponentType.ReadOnly<Temp>()
              }
            }, new EntityQueryDesc()
            {
                All = new ComponentType[3]
              {
          ComponentType.ReadOnly<PropertyOnMarket>(),
          ComponentType.ReadOnly<ResidentialProperty>(),
          ComponentType.ReadOnly<Building>()
              },
                None = new ComponentType[5]
              {
          ComponentType.ReadOnly<Abandoned>(),
          ComponentType.ReadOnly<Deleted>(),
          ComponentType.ReadOnly<Destroyed>(),
          ComponentType.ReadOnly<Temp>(),
          ComponentType.ReadOnly<Condemned>()
              }
            });
            this.RequireForUpdate(this.m_EconomyParameterQuery);
            this.RequireForUpdate(this.m_HealthcareParameterQuery);
            this.RequireForUpdate(this.m_ParkParameterQuery);
            this.RequireForUpdate(this.m_EducationParameterQuery);
            this.RequireForUpdate(this.m_TelecomParameterQuery);
            this.RequireForUpdate(this.m_HouseholdQuery);
            this.RequireForUpdate(this.m_DemandParameterQuery);
            this.m_DefaultDistribution = new DebugWatchDistribution(true, true);
            this.m_EvaluateDistributionLow = new DebugWatchDistribution(true, true);
            this.m_EvaluateDistributionMedium = new DebugWatchDistribution(true, true);
            this.m_EvaluateDistributionHigh = new DebugWatchDistribution(true, true);
            this.m_EvaluateDistributionLowrent = new DebugWatchDistribution(true, true);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnDestroy()
        {
            this.m_DefaultDistribution.Dispose();
            this.m_EvaluateDistributionLow.Dispose();
            this.m_EvaluateDistributionMedium.Dispose();
            this.m_EvaluateDistributionHigh.Dispose();
            this.m_EvaluateDistributionLowrent.Dispose();
            this.m_RentQueue.Dispose();
            this.m_ReservedProperties.Dispose();
            base.OnDestroy();
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            NativeParallelHashMap<Entity, RWHHouseholdFindPropertySystem.CachedPropertyInformation> nativeParallelHashMap = new NativeParallelHashMap<Entity, RWHHouseholdFindPropertySystem.CachedPropertyInformation>(this.m_FreePropertyQuery.CalculateEntityCount(), (AllocatorManager.AllocatorHandle)Allocator.TempJob);
            JobHandle dependencies1;
            NativeArray<GroundPollution> map1 = this.m_GroundPollutionSystem.GetMap(true, out dependencies1);
            JobHandle dependencies2;
            NativeArray<AirPollution> map2 = this.m_AirPollutionSystem.GetMap(true, out dependencies2);
            JobHandle dependencies3;
            NativeArray<NoisePollution> map3 = this.m_NoisePollutionSystem.GetMap(true, out dependencies3);
            JobHandle dependencies4;
            CellMapData<TelecomCoverage> data = this.m_TelecomCoverageSystem.GetData(true, out dependencies4);
            this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Park_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);

            RWHHouseholdFindPropertySystem.PreparePropertyJob jobData = new RWHHouseholdFindPropertySystem.PreparePropertyJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_BuildingProperties = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RW_ComponentLookup,
                m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup,
                m_ParkDatas = this.__TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup,
                m_Renters = this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup,
                m_Households = this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup,
                m_Abandoneds = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup,
                m_Parks = this.__TypeHandle.__Game_Buildings_Park_RO_ComponentLookup,
                m_SpawnableDatas = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup,
                m_BuildingPropertyData = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup,
                m_Buildings = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup,
                m_ServiceCoverages = this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup,
                m_Crimes = this.__TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup,
                m_Locked = this.__TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup,
                m_Transforms = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup,
                m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup,
                m_ElectricityConsumers = this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup,
                m_WaterConsumers = this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup,
                m_GarbageProducers = this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup,
                m_MailProducers = this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup,
                m_PollutionMap = map1,
                m_AirPollutionMap = map2,
                m_NoiseMap = map3,
                m_TelecomCoverages = data,
                m_HealthcareParameters = this.m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
                m_ParkParameters = this.m_ParkParameterQuery.GetSingleton<ParkParameterData>(),
                m_EducationParameters = this.m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
                m_TelecomParameters = this.m_TelecomParameterQuery.GetSingleton<TelecomParameterData>(),
                m_GarbageParameters = this.m_GarbageParameterQuery.GetSingleton<GarbageParameterData>(),
                m_PoliceParameters = this.m_PoliceParameterQuery.GetSingleton<PoliceConfigurationData>(),
                m_CitizenHappinessParameterData = this.m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
                m_City = this.m_CitySystem.City,
                m_PropertyData = nativeParallelHashMap.AsParallelWriter()
            };
            this.__TypeHandle.__Game_Agents_PropertySeeker_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Park_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Pathfind_PathInformations_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            JobHandle outJobHandle;
            JobHandle deps1;
    
            JobHandle jobHandle1 = new RWHHouseholdFindPropertySystem.FindPropertyJob()
            {
                m_Entities = this.m_HouseholdQuery.ToEntityListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle),
                m_CachedPropertyInfo = nativeParallelHashMap,
                m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup,
                m_PropertiesOnMarket = this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup,
                m_Availabilities = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup,
                m_SpawnableDatas = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup,
                m_BuildingProperties = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup,
                m_Buildings = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup,
                m_PathInformationBuffers = this.__TypeHandle.__Game_Pathfind_PathInformations_RO_BufferLookup,
                m_PrefabRefs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_ServiceCoverages = this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup,
                m_Workers = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup,
                m_Students = this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup,
                m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup,
                m_HomelessHouseholds = this.__TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup,
                m_Citizens = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup,
                m_Crimes = this.__TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup,
                m_Lockeds = this.__TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup,
                m_Transforms = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup,
                m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup,
                m_HealthProblems = this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup,
                m_Abandoneds = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup,
                m_Parks = this.__TypeHandle.__Game_Buildings_Park_RO_ComponentLookup,
                m_OwnedVehicles = this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup,
                m_ElectricityConsumers = this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup,
                m_WaterConsumers = this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup,
                m_GarbageProducers = this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup,
                m_MailProducers = this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup,
                m_Households = this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup,
                m_CurrentBuildings = this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup,
                m_CurrentTransports = this.__TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup,
                m_CitizenBuffers = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup,
                m_PropertySeekers = this.__TypeHandle.__Game_Agents_PropertySeeker_RW_ComponentLookup,
                m_PollutionMap = map1,
                m_AirPollutionMap = map2,
                m_NoiseMap = map3,
                m_TelecomCoverages = data,
                m_HealthcareParameters = this.m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
                m_ParkParameters = this.m_ParkParameterQuery.GetSingleton<ParkParameterData>(),
                m_EducationParameters = this.m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
                m_TelecomParameters = this.m_TelecomParameterQuery.GetSingleton<TelecomParameterData>(),
                m_GarbageParameters = this.m_GarbageParameterQuery.GetSingleton<GarbageParameterData>(),
                m_PoliceParameters = this.m_PoliceParameterQuery.GetSingleton<PoliceConfigurationData>(),
                m_CitizenHappinessParameterData = this.m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_TaxRates = this.m_TaxSystem.GetTaxRates(),
                m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_DemandParameters = this.m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
                m_BaseConsumptionSum = ((float)this.m_ResourceSystem.BaseConsumptionSum),
                m_RentQueue = this.m_RentQueue.AsParallelWriter(),
                m_City = this.m_CitySystem.City,
                m_PathfindQueue = this.m_PathfindSetupSystem.GetQueue((object)this, 80, 16).AsParallelWriter(),
                m_TriggerBuffer = this.m_TriggerSystem.CreateActionBuffer().AsParallelWriter(),
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer(),
                m_StatisticsQueue = this.m_CityStatisticsSystem.GetStatisticsEventQueue(out deps1),
                hour = World.GetExistingSystemManaged<TimeSystem>().GetCurrentDateTime().Hour,
                limit_factor = Mod.m_Setting.find_property_limit_factor,
                double_night_limit = Mod.m_Setting.find_property_night
            }.Schedule<RWHHouseholdFindPropertySystem.FindPropertyJob>(JobHandle.CombineDependencies(jobData.ScheduleParallel<RWHHouseholdFindPropertySystem.PreparePropertyJob>(this.m_FreePropertyQuery, JobUtils.CombineDependencies(this.Dependency, dependencies1, dependencies3, dependencies2, dependencies4)), outJobHandle, deps1));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle1);
            this.m_PathfindSetupSystem.AddQueueWriter(jobHandle1);
            this.m_ResourceSystem.AddPrefabsReader(jobHandle1);
            this.m_AirPollutionSystem.AddReader(jobHandle1);
            this.m_NoisePollutionSystem.AddReader(jobHandle1);
            this.m_GroundPollutionSystem.AddReader(jobHandle1);
            this.m_TelecomCoverageSystem.AddReader(jobHandle1);
            this.m_TriggerSystem.AddActionBufferWriter(jobHandle1);
            this.m_CityStatisticsSystem.AddWriter(jobHandle1);
            this.m_CountEmploymentSystem.AddReader(jobHandle1);
            this.m_TaxSystem.AddReader(jobHandle1);
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_ExtractorCompany_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Park_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_WorkProvider_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_IndustrialCompany_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Renter_RW_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            JobHandle deps2;

            JobHandle jobHandle2 = new Game.Buildings.PropertyUtils.RentJob()
            {
                m_RentEventArchetype = this.m_RentEventArchetype,
                m_PropertiesOnMarket = this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup,
                m_Renters = this.__TypeHandle.__Game_Buildings_Renter_RW_BufferLookup,
                m_BuildingProperties = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup,
                m_ParkDatas = this.__TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup,
                m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RW_ComponentLookup,
                m_Companies = this.__TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup,
                m_Households = this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup,
                m_Industrials = this.__TypeHandle.__Game_Companies_IndustrialCompany_RO_ComponentLookup,
                m_Commercials = this.__TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup,
                m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup,
                m_ServiceCompanyDatas = this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup,
                m_ProcessDatas = this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup,
                m_WorkProviders = this.__TypeHandle.__Game_Companies_WorkProvider_RW_ComponentLookup,
                m_HouseholdCitizens = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup,
                m_Abandoneds = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup,
                m_HomelessHouseholds = this.__TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup,
                m_Parks = this.__TypeHandle.__Game_Buildings_Park_RO_ComponentLookup,
                m_Employees = this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup,
                m_SpawnableBuildings = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup,
                m_Attacheds = this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup,
                m_ExtractorCompanies = this.__TypeHandle.__Game_Companies_ExtractorCompany_RO_ComponentLookup,
                m_SubAreas = this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup,
                m_Geometries = this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup,
                m_Lots = this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup,
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_Resources = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_StatisticsQueue = this.m_CityStatisticsSystem.GetStatisticsEventQueue(out deps2),
                m_TriggerQueue = this.m_TriggerSystem.CreateActionBuffer(),
                m_AreaType = Game.Zones.AreaType.Residential,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer(),
                m_RentQueue = this.m_RentQueue,
                m_ReservedProperties = this.m_ReservedProperties,
                m_DebugDisableHomeless = this.debugDisableHomeless
            }.Schedule<Game.Buildings.PropertyUtils.RentJob>(JobHandle.CombineDependencies(deps2, jobHandle1));
            this.m_TriggerSystem.AddActionBufferWriter(jobHandle2);
            this.m_CityStatisticsSystem.AddWriter(jobHandle2);
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
            this.Dependency = jobHandle2;
            nativeParallelHashMap.Dispose(this.Dependency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        [UnityEngine.Scripting.Preserve]
        public RWHHouseholdFindPropertySystem()
        {
        }

        public struct CachedPropertyInformation
        {
            public HouseholdFindPropertySystem.GenericApartmentQuality quality;
            public int free;
        }

        public struct GenericApartmentQuality
        {
            public float apartmentSize;
            public float2 educationBonus;
            public float welfareBonus;
            public float score;
            public int level;
        }

        [BurstCompile]
        private struct PreparePropertyJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingProperties;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;
            [ReadOnly]
            public BufferLookup<Renter> m_Renters;
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoneds;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Park> m_Parks;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> m_BuildingDatas;
            [ReadOnly]
            public ComponentLookup<ParkData> m_ParkDatas;
            [ReadOnly]
            public ComponentLookup<Household> m_Households;
            [ReadOnly]
            public ComponentLookup<Building> m_Buildings;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingPropertyData;
            [ReadOnly]
            public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;
            [ReadOnly]
            public ComponentLookup<CrimeProducer> m_Crimes;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_Transforms;
            [ReadOnly]
            public ComponentLookup<Locked> m_Locked;
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;
            [ReadOnly]
            public ComponentLookup<WaterConsumer> m_WaterConsumers;
            [ReadOnly]
            public ComponentLookup<GarbageProducer> m_GarbageProducers;
            [ReadOnly]
            public ComponentLookup<MailProducer> m_MailProducers;
            [ReadOnly]
            public NativeArray<AirPollution> m_AirPollutionMap;
            [ReadOnly]
            public NativeArray<GroundPollution> m_PollutionMap;
            [ReadOnly]
            public NativeArray<NoisePollution> m_NoiseMap;
            [ReadOnly]
            public CellMapData<TelecomCoverage> m_TelecomCoverages;
            public HealthcareParameterData m_HealthcareParameters;
            public ParkParameterData m_ParkParameters;
            public EducationParameterData m_EducationParameters;
            public TelecomParameterData m_TelecomParameters;
            public GarbageParameterData m_GarbageParameters;
            public PoliceConfigurationData m_PoliceParameters;
            public CitizenHappinessParameterData m_CitizenHappinessParameterData;
            public Entity m_City;
            public NativeParallelHashMap<Entity, RWHHouseholdFindPropertySystem.CachedPropertyInformation>.ParallelWriter m_PropertyData;

            private int CalculateFree(Entity property)
            {
                Entity prefab = this.m_Prefabs[property].m_Prefab;
                int free = 0;

                if (this.m_BuildingDatas.HasComponent(prefab) && (this.m_Abandoneds.HasComponent(property) || this.m_Parks.HasComponent(property) && this.m_ParkDatas[prefab].m_AllowHomeless))
                {
                    free = HomelessShelterAISystem.GetShelterCapacity(this.m_BuildingDatas[prefab], this.m_BuildingPropertyData.HasComponent(prefab) ? this.m_BuildingPropertyData[prefab] : new BuildingPropertyData()) - this.m_Renters[property].Length;
                }
                else
                {
                    if (this.m_BuildingProperties.HasComponent(prefab))
                    {
                        BuildingPropertyData buildingProperty = this.m_BuildingProperties[prefab];
                        DynamicBuffer<Renter> renter = this.m_Renters[property];
                        free = buildingProperty.CountProperties(Game.Zones.AreaType.Residential);
                        for (int index = 0; index < renter.Length; ++index)
                        {
                            if (this.m_Households.HasComponent(renter[index].m_Renter))
                                --free;
                        }
                    }
                }
                return free;
            }

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                
                for (int index = 0; index < nativeArray.Length; ++index)
                {
                    Entity entity = nativeArray[index];
                    int free = this.CalculateFree(entity);
                    
                    if (free > 0)
                    {
                        Entity prefab = this.m_Prefabs[entity].m_Prefab;
                        Building building = this.m_Buildings[entity];
                        Entity healthcareServicePrefab = this.m_HealthcareParameters.m_HealthcareServicePrefab;
                        Entity parkServicePrefab = this.m_ParkParameters.m_ParkServicePrefab;
                        Entity educationServicePrefab = this.m_EducationParameters.m_EducationServicePrefab;
                        Entity telecomServicePrefab = this.m_TelecomParameters.m_TelecomServicePrefab;
                        Entity garbageServicePrefab = this.m_GarbageParameters.m_GarbageServicePrefab;
                        Entity policeServicePrefab = this.m_PoliceParameters.m_PoliceServicePrefab;
                        DynamicBuffer<CityModifier> cityModifier = this.m_CityModifiers[this.m_City];

                        HouseholdFindPropertySystem.GenericApartmentQuality apartmentQuality = Game.Buildings.PropertyUtils.GetGenericApartmentQuality(entity, prefab, ref building, ref this.m_BuildingProperties, ref this.m_BuildingDatas, ref this.m_SpawnableDatas, ref this.m_Crimes, ref this.m_ServiceCoverages, ref this.m_Locked, ref this.m_ElectricityConsumers, ref this.m_WaterConsumers, ref this.m_GarbageProducers, ref this.m_MailProducers, ref this.m_Transforms, ref this.m_Abandoneds, this.m_PollutionMap, this.m_AirPollutionMap, this.m_NoiseMap, this.m_TelecomCoverages, cityModifier, healthcareServicePrefab, parkServicePrefab, educationServicePrefab, telecomServicePrefab, garbageServicePrefab, policeServicePrefab, this.m_CitizenHappinessParameterData, this.m_GarbageParameters);
                        this.m_PropertyData.TryAdd(entity, new RWHHouseholdFindPropertySystem.CachedPropertyInformation()
                        {
                            free = free,
                            quality = apartmentQuality
                        });
                    }
                }
            }

            void IJobChunk.Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        [BurstCompile]
        private struct FindPropertyJob : IJob
        {
            public NativeList<Entity> m_Entities;
            public NativeParallelHashMap<Entity, RWHHouseholdFindPropertySystem.CachedPropertyInformation> m_CachedPropertyInfo;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> m_BuildingDatas;
            [ReadOnly]
            public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;
            [ReadOnly]
            public BufferLookup<PathInformations> m_PathInformationBuffers;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefs;
            [ReadOnly]
            public ComponentLookup<Building> m_Buildings;
            [ReadOnly]
            public ComponentLookup<Worker> m_Workers;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> m_Students;
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingProperties;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly]
            public BufferLookup<ResourceAvailability> m_Availabilities;
            [ReadOnly]
            public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;
            [ReadOnly]
            public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;
            [ReadOnly]
            public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly]
            public ComponentLookup<CrimeProducer> m_Crimes;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_Transforms;
            [ReadOnly]
            public ComponentLookup<Locked> m_Lockeds;
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;
            [ReadOnly]
            public ComponentLookup<HealthProblem> m_HealthProblems;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Park> m_Parks;
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoneds;
            [ReadOnly]
            public BufferLookup<OwnedVehicle> m_OwnedVehicles;
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;
            [ReadOnly]
            public ComponentLookup<WaterConsumer> m_WaterConsumers;
            [ReadOnly]
            public ComponentLookup<GarbageProducer> m_GarbageProducers;
            [ReadOnly]
            public ComponentLookup<MailProducer> m_MailProducers;
            [ReadOnly]
            public ComponentLookup<Household> m_Households;
            [ReadOnly]
            public ComponentLookup<CurrentBuilding> m_CurrentBuildings;
            [ReadOnly]
            public ComponentLookup<CurrentTransport> m_CurrentTransports;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> m_CitizenBuffers;
            public ComponentLookup<PropertySeeker> m_PropertySeekers;
            [ReadOnly]
            public NativeArray<AirPollution> m_AirPollutionMap;
            [ReadOnly]
            public NativeArray<GroundPollution> m_PollutionMap;
            [ReadOnly]
            public NativeArray<NoisePollution> m_NoiseMap;
            [ReadOnly]
            public CellMapData<TelecomCoverage> m_TelecomCoverages;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            [ReadOnly]
            public NativeArray<int> m_TaxRates;
            public HealthcareParameterData m_HealthcareParameters;
            public ParkParameterData m_ParkParameters;
            public EducationParameterData m_EducationParameters;
            public TelecomParameterData m_TelecomParameters;
            public GarbageParameterData m_GarbageParameters;
            public PoliceConfigurationData m_PoliceParameters;
            public CitizenHappinessParameterData m_CitizenHappinessParameterData;
            public float m_BaseConsumptionSum;
            public EntityCommandBuffer m_CommandBuffer;
            [ReadOnly]
            public Entity m_City;
            public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
            public NativeQueue<Game.Buildings.PropertyUtils.RentAction>.ParallelWriter m_RentQueue;
            public EconomyParameterData m_EconomyParameters;
            public DemandParameterData m_DemandParameters;
            public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;
            public NativeQueue<StatisticsEvent> m_StatisticsQueue;
            public int hour;
            public int limit_factor;
            public bool double_night_limit;

            private void StartHomeFinding(
              Entity household,
              Entity commuteCitizen,
              Entity originLocation,
              Entity oldHome,
              float minimumScore,
              bool movingIn,
              DynamicBuffer<HouseholdCitizen> citizens)
            {
                this.m_CommandBuffer.AddComponent<PathInformation>(household, new PathInformation()
                {
                    m_State = PathFlags.Pending
                });
                Household household1 = this.m_Households[household];
                PathfindWeights pathfindWeights = new PathfindWeights();
                Citizen componentData;
                if (this.m_Citizens.TryGetComponent(commuteCitizen, out componentData))
                {
                    pathfindWeights = CitizenUtils.GetPathfindWeights(componentData, household1, citizens.Length);
                }
                else
                {
                    for (int index = 0; index < citizens.Length; ++index)
                        pathfindWeights.m_Value += CitizenUtils.GetPathfindWeights(componentData, household1, citizens.Length).m_Value;
                    pathfindWeights.m_Value *= 1f / (float)citizens.Length;
                }
                PathfindParameters parameters = new PathfindParameters()
                {
                    m_MaxSpeed = (float2)111.111115f,
                    m_WalkSpeed = (float2)1.66666675f,
                    m_Weights = pathfindWeights,
                    m_Methods = PathMethod.Pedestrian | PathMethod.PublicTransportDay,
                    m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost,
                    m_PathfindFlags = PathfindFlags.Simplified | PathfindFlags.IgnorePath
                };
                SetupQueueTarget setupQueueTarget = new SetupQueueTarget();
                setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                setupQueueTarget.m_Methods = PathMethod.Pedestrian;
                setupQueueTarget.m_Entity = originLocation;
                SetupQueueTarget a = setupQueueTarget;
                setupQueueTarget = new SetupQueueTarget();
                setupQueueTarget.m_Type = SetupTargetType.FindHome;
                setupQueueTarget.m_Methods = PathMethod.Pedestrian;
                setupQueueTarget.m_Entity = household;
                setupQueueTarget.m_Entity2 = oldHome;
                setupQueueTarget.m_Value2 = minimumScore;
                SetupQueueTarget b = setupQueueTarget;
                DynamicBuffer<OwnedVehicle> bufferData;
                if (this.m_OwnedVehicles.TryGetBuffer(household, out bufferData) && bufferData.Length != 0)
                {
                    parameters.m_Methods |= movingIn ? PathMethod.Road : PathMethod.Road | PathMethod.Parking;
                    parameters.m_ParkingLength = float.MinValue;
                    parameters.m_IgnoredRules |= RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidSlowTraffic;
                    a.m_Methods |= PathMethod.Road;
                    a.m_RoadTypes |= RoadTypes.Car;
                    b.m_Methods |= PathMethod.Road;
                    b.m_RoadTypes |= RoadTypes.Car;
                }
                if (movingIn)
                {
                    parameters.m_MaxSpeed.y = 277.777771f;
                    parameters.m_Methods |= PathMethod.Taxi | PathMethod.PublicTransportNight;
                    parameters.m_SecondaryIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults();
                }
                else if (originLocation != Entity.Null)
                    CommonUtils.Swap<SetupQueueTarget>(ref a, ref b);
                parameters.m_MaxResultCount = 10;
                parameters.m_PathfindFlags |= movingIn ? PathfindFlags.MultipleDestinations : PathfindFlags.MultipleOrigins;
                this.m_CommandBuffer.AddBuffer<PathInformations>(household).Add(new PathInformations()
                {
                    m_State = PathFlags.Pending
                });
                this.m_PathfindQueue.Enqueue(new SetupQueueItem(household, parameters, a, b));
            }

            private Entity GetFirstWorkplaceOrSchool(
              DynamicBuffer<HouseholdCitizen> citizens,
              ref Entity citizen)
            {
                for (int index = 0; index < citizens.Length; ++index)
                {
                    citizen = citizens[index].m_Citizen;
                    if (this.m_Workers.HasComponent(citizen))
                    {
                        return this.m_Workers[citizen].m_Workplace;
                    }
                    if (this.m_Students.HasComponent(citizen))
                    {
                        return this.m_Students[citizen].m_School;
                    }
                }
                return Entity.Null;
            }

            private Entity GetCurrentLocation(DynamicBuffer<HouseholdCitizen> citizens)
            {
                for (int index = 0; index < citizens.Length; ++index)
                {
                    CurrentBuilding componentData1;
                    if (this.m_CurrentBuildings.TryGetComponent(citizens[index].m_Citizen, out componentData1))
                        return componentData1.m_CurrentBuilding;
                    CurrentTransport componentData2;
                    if (this.m_CurrentTransports.TryGetComponent(citizens[index].m_Citizen, out componentData2))
                        return componentData2.m_CurrentTransport;
                }
                return Entity.Null;
            }

            private void MoveAway(Entity household, DynamicBuffer<HouseholdCitizen> citizens)
            {
                this.m_CommandBuffer.AddComponent<MovingAway>(household, new MovingAway());
                this.m_CommandBuffer.RemoveComponent<PropertySeeker>(household);
            }

            public void Execute()
            {
                //Mod.log.Info($"Process Entities:{this.m_Entities.Length}");
                int maxProcessEntities = kMaxProcessEntitiesPerUpdate;
                //At night more computer resources are available
                if (double_night_limit && (hour > 23 || hour < 6))
                {
                    maxProcessEntities *= 2;
                }
                if (this.m_Entities.Length < maxProcessEntities)
                {
                    maxProcessEntities = this.m_Entities.Length;
                } else
                {
                    int maxProcess_factor2 = limit_factor;
                    int maxProcess_factor1 = 1;
                    if(limit_factor > 2)
                    {
                        maxProcess_factor1 = limit_factor - 1;
                    }
                    int ratio = this.m_Entities.Length / maxProcessEntities;
                    if(ratio > 10)
                    {
                        maxProcessEntities *= maxProcess_factor2;
                    } else if(ratio > 5)
                    {
                        maxProcessEntities *= maxProcess_factor1;
                    }
                }
                //Mod.log.Info($"Process Entities used:{maxProcessEntities}");
                for (int index1 = 0; index1 < math.min(maxProcessEntities, this.m_Entities.Length); ++index1)
                {
                    Entity entity1 = this.m_Entities[index1];
                    DynamicBuffer<HouseholdCitizen> citizenBuffer = this.m_CitizenBuffers[entity1];
                    if (citizenBuffer.Length != 0)
                    {
                        int householdIncome = EconomyUtils.GetHouseholdIncome(citizenBuffer, ref this.m_Workers, ref this.m_Citizens, ref this.m_HealthProblems, ref this.m_EconomyParameters, this.m_TaxRates);
                        bool flag1 = this.m_HomelessHouseholds.HasComponent(entity1) || this.m_PropertyRenters.HasComponent(entity1) && this.m_PropertyRenters[entity1].m_Property != Entity.Null;
                        PropertySeeker propertySeeker = this.m_PropertySeekers[entity1];
                        DynamicBuffer<PathInformations> bufferData;
                        if (this.m_PathInformationBuffers.TryGetBuffer(entity1, out bufferData))
                        {
                            int index2 = 0;
                            PathInformations pathInformations1 = bufferData[index2];
                            if ((pathInformations1.m_State & PathFlags.Pending) == (PathFlags)0)
                            {
                                this.m_CommandBuffer.RemoveComponent<PathInformations>(entity1);
                                bool flag2 = flag1 && propertySeeker.m_TargetProperty != Entity.Null;
                                Entity entity2 = flag2 ? pathInformations1.m_Origin : pathInformations1.m_Destination;
                                bool flag3 = false;
                                PathInformations pathInformations2;
                                for (; !this.m_CachedPropertyInfo.ContainsKey(entity2) || this.m_CachedPropertyInfo[entity2].free <= 0; entity2 = flag2 ? pathInformations2.m_Origin : pathInformations2.m_Destination)
                                {
                                    ++index2;
                                    if (bufferData.Length > index2)
                                    {
                                        pathInformations2 = bufferData[index2];
                                    }
                                    else
                                    {
                                        entity2 = Entity.Null;
                                        flag3 = true;
                                        break;
                                    }
                                }
                                if (!flag3 || bufferData.Length == 0 || !(bufferData[0].m_Destination != Entity.Null))
                                {
                                    float num = float.NegativeInfinity;
                                    if (entity2 != Entity.Null && this.m_CachedPropertyInfo.ContainsKey(entity2) && this.m_CachedPropertyInfo[entity2].free > 0)
                                    {
                                        num = Game.Buildings.PropertyUtils.GetPropertyScore(entity2, entity1, citizenBuffer, ref this.m_PrefabRefs, ref this.m_BuildingProperties, ref this.m_Buildings, ref this.m_BuildingDatas, ref this.m_Households, ref this.m_Citizens, ref this.m_Students, ref this.m_Workers, ref this.m_SpawnableDatas, ref this.m_Crimes, ref this.m_ServiceCoverages, ref this.m_Lockeds, ref this.m_ElectricityConsumers, ref this.m_WaterConsumers, ref this.m_GarbageProducers, ref this.m_MailProducers, ref this.m_Transforms, ref this.m_Abandoneds, ref this.m_Parks, ref this.m_Availabilities, this.m_TaxRates, this.m_PollutionMap, this.m_AirPollutionMap, this.m_NoiseMap, this.m_TelecomCoverages, this.m_CityModifiers[this.m_City], this.m_HealthcareParameters.m_HealthcareServicePrefab, this.m_ParkParameters.m_ParkServicePrefab, this.m_EducationParameters.m_EducationServicePrefab, this.m_TelecomParameters.m_TelecomServicePrefab, this.m_GarbageParameters.m_GarbageServicePrefab, this.m_PoliceParameters.m_PoliceServicePrefab, this.m_CitizenHappinessParameterData, this.m_GarbageParameters);
                                    }
                                    if ((double)num < (double)propertySeeker.m_BestPropertyScore)
                                        entity2 = propertySeeker.m_BestProperty;
                                    bool flag4 = (this.m_Households[entity1].m_Flags & HouseholdFlags.MovedIn) != 0;
                                    bool flag5 = entity2 != Entity.Null && (this.m_Parks.HasComponent(entity2) || this.m_Abandoneds.HasComponent(entity2));

                                    if (((!this.m_PropertiesOnMarket.HasComponent(entity2) ? 0 : (this.m_PropertiesOnMarket[entity2].m_AskingRent < householdIncome ? 1 : 0)) & (!this.m_PropertyRenters.HasComponent(entity1) ? (true ? 1 : 0) : (!this.m_PropertyRenters[entity1].m_Property.Equals(entity2) ? 1 : 0))) != 0 || flag4 & flag5)
                                    {
                                        this.m_RentQueue.Enqueue(new Game.Buildings.PropertyUtils.RentAction()
                                        {
                                            m_Property = entity2,
                                            m_Renter = entity1
                                        });
                                        if (this.m_CachedPropertyInfo.ContainsKey(entity2))
                                        {
                                            RWHHouseholdFindPropertySystem.CachedPropertyInformation propertyInformation = this.m_CachedPropertyInfo[entity2];
                                            --propertyInformation.free;
                                            this.m_CachedPropertyInfo[entity2] = propertyInformation;
                                        }
                                        this.m_CommandBuffer.RemoveComponent<PropertySeeker>(entity1);
                                    }
                                    else if (entity2 == Entity.Null)
                                    {
                                        this.MoveAway(entity1, citizenBuffer);
                                    }
                                    else
                                    {
                                        if (this.m_PropertyRenters.HasComponent(entity1) && this.m_PropertyRenters[entity1].m_Property == entity2)
                                        {
                                            if (householdIncome < this.m_PropertyRenters[entity1].m_Rent)
                                            {
                                                this.MoveAway(entity1, citizenBuffer);
                                            }
                                            else
                                            {
                                                this.m_CommandBuffer.RemoveComponent<PropertySeeker>(entity1);
                                            }
                                        }
                                        else
                                        {
                                            propertySeeker.m_BestProperty = new Entity();
                                            propertySeeker.m_BestPropertyScore = float.NegativeInfinity;
                                            this.m_PropertySeekers[entity1] = propertySeeker;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Entity entity3 = this.m_PropertyRenters.HasComponent(entity1) ? this.m_PropertyRenters[entity1].m_Property : Entity.Null;

                            float num = entity3 != Entity.Null ? Game.Buildings.PropertyUtils.GetPropertyScore(entity3, entity1, citizenBuffer, ref this.m_PrefabRefs, ref this.m_BuildingProperties, ref this.m_Buildings, ref this.m_BuildingDatas, ref this.m_Households, ref this.m_Citizens, ref this.m_Students, ref this.m_Workers, ref this.m_SpawnableDatas, ref this.m_Crimes, ref this.m_ServiceCoverages, ref this.m_Lockeds, ref this.m_ElectricityConsumers, ref this.m_WaterConsumers, ref this.m_GarbageProducers, ref this.m_MailProducers, ref this.m_Transforms, ref this.m_Abandoneds, ref this.m_Parks, ref this.m_Availabilities, this.m_TaxRates, this.m_PollutionMap, this.m_AirPollutionMap, this.m_NoiseMap, this.m_TelecomCoverages, this.m_CityModifiers[this.m_City], this.m_HealthcareParameters.m_HealthcareServicePrefab, this.m_ParkParameters.m_ParkServicePrefab, this.m_EducationParameters.m_EducationServicePrefab, this.m_TelecomParameters.m_TelecomServicePrefab, this.m_GarbageParameters.m_GarbageServicePrefab, this.m_PoliceParameters.m_PoliceServicePrefab, this.m_CitizenHappinessParameterData, this.m_GarbageParameters) : float.NegativeInfinity;
                            Entity citizen = Entity.Null;
                            Entity workplaceOrSchool = this.GetFirstWorkplaceOrSchool(citizenBuffer, ref citizen);
                            bool movingIn = !this.m_HomelessHouseholds.HasComponent(entity1) && entity3 == Entity.Null;
                            Entity originLocation = workplaceOrSchool != Entity.Null ? workplaceOrSchool : this.GetCurrentLocation(citizenBuffer);
                            if (originLocation == Entity.Null)
                            {
                                UnityEngine.Debug.LogWarning((object)string.Format("No valid origin location to start home path finding for household:{0}, move away", (object)entity1.Index));
                                this.MoveAway(entity1, citizenBuffer);
                            }
                            else
                            {
                                propertySeeker.m_TargetProperty = originLocation;
                                propertySeeker.m_BestProperty = entity3;
                                propertySeeker.m_BestPropertyScore = num;
                                this.m_PropertySeekers[entity1] = propertySeeker;
                                this.StartHomeFinding(entity1, citizen, originLocation, entity3, propertySeeker.m_BestPropertyScore, movingIn, citizenBuffer);
                            }
                        }
                    }
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ParkData> __Game_Prefabs_ParkData_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<PathInformations> __Game_Pathfind_PathInformations_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            public ComponentLookup<PropertySeeker> __Game_Agents_PropertySeeker_RW_ComponentLookup;
            public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CompanyData> __Game_Companies_CompanyData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<IndustrialCompany> __Game_Companies_IndustrialCompany_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CommercialCompany> __Game_Companies_CommercialCompany_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
            public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RW_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Companies.ExtractorCompany> __Game_Companies_ExtractorCompany_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Prefabs_BuildingPropertyData_RW_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>();
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.BuildingData>(true);
                this.__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(true);
                this.__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(true);
                this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
                this.__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(true);
                this.__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(true);
                this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                this.__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(true);
                this.__Game_Buildings_CrimeProducer_RO_ComponentLookup = state.GetComponentLookup<CrimeProducer>(true);
                this.__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
                this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
                this.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(true);
                this.__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(true);
                this.__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(true);
                this.__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(true);
                this.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>(true);
                this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(true);
                this.__Game_Pathfind_PathInformations_RO_BufferLookup = state.GetBufferLookup<PathInformations>(true);
                this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
                this.__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(true);
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(true);
                this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
                this.__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(true);
                this.__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(true);
                this.__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(true);
                this.__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(true);
                this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
                this.__Game_Agents_PropertySeeker_RW_ComponentLookup = state.GetComponentLookup<PropertySeeker>();
                this.__Game_Buildings_Renter_RW_BufferLookup = state.GetBufferLookup<Renter>();
                this.__Game_Buildings_PropertyRenter_RW_ComponentLookup = state.GetComponentLookup<PropertyRenter>();
                this.__Game_Companies_CompanyData_RO_ComponentLookup = state.GetComponentLookup<CompanyData>(true);
                this.__Game_Companies_IndustrialCompany_RO_ComponentLookup = state.GetComponentLookup<IndustrialCompany>(true);
                this.__Game_Companies_CommercialCompany_RO_ComponentLookup = state.GetComponentLookup<CommercialCompany>(true);
                this.__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(true);
                this.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(true);
                this.__Game_Companies_WorkProvider_RW_ComponentLookup = state.GetComponentLookup<WorkProvider>();
                this.__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(true);
                this.__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(true);
                this.__Game_Companies_ExtractorCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.ExtractorCompany>(true);
                this.__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(true);
                this.__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(true);
                this.__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(true);
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
            }
        }
    }
}
