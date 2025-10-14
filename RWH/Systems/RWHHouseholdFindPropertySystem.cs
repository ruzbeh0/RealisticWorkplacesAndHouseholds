// Decompiled with JetBrains decompiler
// Type: Game.Simulation.RWHHouseholdFindPropertySystem
// Assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: CC2DA331-4A40-4C70-A203-F382286B5D52
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed\Game.dll

using Colossal.Entities;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Debug;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
using Game.Zones;
using Game.Simulation;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using RealisticWorkplacesAndHouseholds;

#nullable disable
namespace RWH.Systems;

//[CompilerGenerated]
public partial class RWHHouseholdFindPropertySystem : GameSystemBase
{
    public bool debugDisableHomeless;
    private const int UPDATE_INTERVAL = 16 /*0x10*/;
    public static readonly int kMaxProcessEntitiesPerUpdate = 1024 /*0x80*/;
    public static readonly int kFindPropertyCoolDown = 5000;
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
    private EntityQuery m_HomelessHouseholdQuery;
    private EntityQuery m_FreePropertyQuery;
    private EntityQuery m_EconomyParameterQuery;
    private EntityQuery m_DemandParameterQuery;
    private EndFrameBarrier m_EndFrameBarrier;
    private PathfindSetupSystem m_PathfindSetupSystem;
    private TaxSystem m_TaxSystem;
    private TriggerSystem m_TriggerSystem;
    private GroundPollutionSystem m_GroundPollutionSystem;
    private AirPollutionSystem m_AirPollutionSystem;
    private NoisePollutionSystem m_NoisePollutionSystem;
    private TelecomCoverageSystem m_TelecomCoverageSystem;
    private CitySystem m_CitySystem;
    private CityStatisticsSystem m_CityStatisticsSystem;
    private SimulationSystem m_SimulationSystem;
    private PropertyProcessingSystem m_PropertyProcessingSystem;
    private CountResidentialPropertySystem m_CountResidentialPropertySystem;
    private EntityQuery m_HealthcareParameterQuery;
    private EntityQuery m_ParkParameterQuery;
    private EntityQuery m_EducationParameterQuery;
    private EntityQuery m_TelecomParameterQuery;
    private EntityQuery m_GarbageParameterQuery;
    private EntityQuery m_PoliceParameterQuery;
    private EntityQuery m_CitizenHappinessParameterQuery;
    private RWHHouseholdFindPropertySystem.TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase) => 16 /*0x10*/;

    [UnityEngine.Scripting.Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        // ISSUE: reference to a compiler-generated field
        this.m_PathfindSetupSystem = this.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_GroundPollutionSystem = this.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_AirPollutionSystem = this.World.GetOrCreateSystemManaged<AirPollutionSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_NoisePollutionSystem = this.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_TelecomCoverageSystem = this.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_CityStatisticsSystem = this.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_TaxSystem = this.World.GetOrCreateSystemManaged<TaxSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_TriggerSystem = this.World.GetOrCreateSystemManaged<TriggerSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_PropertyProcessingSystem = this.World.GetOrCreateSystemManaged<PropertyProcessingSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_CountResidentialPropertySystem = this.World.GetOrCreateSystemManaged<CountResidentialPropertySystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_HomelessHouseholdQuery = this.GetEntityQuery(ComponentType.ReadWrite<HomelessHousehold>(), ComponentType.ReadWrite<PropertySeeker>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        // ISSUE: reference to a compiler-generated field
        this.m_HouseholdQuery = this.GetEntityQuery(ComponentType.ReadWrite<Household>(), ComponentType.ReadWrite<PropertySeeker>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<HomelessHousehold>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        // ISSUE: reference to a compiler-generated field
        this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        // ISSUE: reference to a compiler-generated field
        this.m_DemandParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
        // ISSUE: reference to a compiler-generated field
        this.m_HealthcareParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
        // ISSUE: reference to a compiler-generated field
        this.m_ParkParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<ParkParameterData>());
        // ISSUE: reference to a compiler-generated field
        this.m_EducationParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EducationParameterData>());
        // ISSUE: reference to a compiler-generated field
        this.m_TelecomParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<TelecomParameterData>());
        // ISSUE: reference to a compiler-generated field
        this.m_GarbageParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
        // ISSUE: reference to a compiler-generated field
        this.m_PoliceParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
        // ISSUE: reference to a compiler-generated field
        this.m_CitizenHappinessParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
        // ISSUE: reference to a compiler-generated field
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
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_EconomyParameterQuery);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_HealthcareParameterQuery);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_ParkParameterQuery);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_EducationParameterQuery);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_TelecomParameterQuery);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_HouseholdQuery);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_DemandParameterQuery);
        // ISSUE: reference to a compiler-generated field
        this.m_DefaultDistribution = new DebugWatchDistribution(true, true);
        // ISSUE: reference to a compiler-generated field
        this.m_EvaluateDistributionLow = new DebugWatchDistribution(true, true);
        // ISSUE: reference to a compiler-generated field
        this.m_EvaluateDistributionMedium = new DebugWatchDistribution(true, true);
        // ISSUE: reference to a compiler-generated field
        this.m_EvaluateDistributionHigh = new DebugWatchDistribution(true, true);
        // ISSUE: reference to a compiler-generated field
        this.m_EvaluateDistributionLowrent = new DebugWatchDistribution(true, true);
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnDestroy()
    {
        // ISSUE: reference to a compiler-generated field
        this.m_DefaultDistribution.Dispose();
        // ISSUE: reference to a compiler-generated field
        this.m_EvaluateDistributionLow.Dispose();
        // ISSUE: reference to a compiler-generated field
        this.m_EvaluateDistributionMedium.Dispose();
        // ISSUE: reference to a compiler-generated field
        this.m_EvaluateDistributionHigh.Dispose();
        // ISSUE: reference to a compiler-generated field
        this.m_EvaluateDistributionLowrent.Dispose();
        base.OnDestroy();
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnUpdate()
    {
        // ISSUE: reference to a compiler-generated field
        NativeParallelHashMap<Entity, RWHHouseholdFindPropertySystem.CachedPropertyInformation> nativeParallelHashMap = new NativeParallelHashMap<Entity, RWHHouseholdFindPropertySystem.CachedPropertyInformation>(this.m_FreePropertyQuery.CalculateEntityCount(), (AllocatorManager.AllocatorHandle)Allocator.TempJob);
        JobHandle dependencies1;
        // ISSUE: reference to a compiler-generated field
        NativeArray<GroundPollution> map1 = this.m_GroundPollutionSystem.GetMap(true, out dependencies1);
        JobHandle dependencies2;
        // ISSUE: reference to a compiler-generated field
        NativeArray<AirPollution> map2 = this.m_AirPollutionSystem.GetMap(true, out dependencies2);
        JobHandle dependencies3;
        // ISSUE: reference to a compiler-generated field
        NativeArray<NoisePollution> map3 = this.m_NoisePollutionSystem.GetMap(true, out dependencies3);
        JobHandle dependencies4;
        // ISSUE: reference to a compiler-generated field
        CellMapData<TelecomCoverage> data = this.m_TelecomCoverageSystem.GetData(true, out dependencies4);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        RWHHouseholdFindPropertySystem.PreparePropertyJob jobData1 = new RWHHouseholdFindPropertySystem.PreparePropertyJob()
        {
            m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
            m_BuildingProperties = InternalCompilerInterface.GetComponentLookup<BuildingPropertyData>(ref this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RW_ComponentLookup, ref this.CheckedStateRef),
            m_Prefabs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
            m_BuildingDatas = InternalCompilerInterface.GetComponentLookup<Game.Prefabs.BuildingData>(ref this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ParkDatas = InternalCompilerInterface.GetComponentLookup<ParkData>(ref this.__TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Renters = InternalCompilerInterface.GetBufferLookup<Renter>(ref this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref this.CheckedStateRef),
            m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Abandoneds = InternalCompilerInterface.GetComponentLookup<Abandoned>(ref this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Parks = InternalCompilerInterface.GetComponentLookup<Game.Buildings.Park>(ref this.__TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref this.CheckedStateRef),
            m_SpawnableDatas = InternalCompilerInterface.GetComponentLookup<SpawnableBuildingData>(ref this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_BuildingPropertyData = InternalCompilerInterface.GetComponentLookup<BuildingPropertyData>(ref this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Buildings = InternalCompilerInterface.GetComponentLookup<Building>(ref this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ServiceCoverages = InternalCompilerInterface.GetBufferLookup<Game.Net.ServiceCoverage>(ref this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref this.CheckedStateRef),
            m_Crimes = InternalCompilerInterface.GetComponentLookup<CrimeProducer>(ref this.__TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Locked = InternalCompilerInterface.GetComponentLookup<Locked>(ref this.__TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Transforms = InternalCompilerInterface.GetComponentLookup<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CityModifiers = InternalCompilerInterface.GetBufferLookup<CityModifier>(ref this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref this.CheckedStateRef),
            m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup<ElectricityConsumer>(ref this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_WaterConsumers = InternalCompilerInterface.GetComponentLookup<WaterConsumer>(ref this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_GarbageProducers = InternalCompilerInterface.GetComponentLookup<GarbageProducer>(ref this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_MailProducers = InternalCompilerInterface.GetComponentLookup<MailProducer>(ref this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref this.CheckedStateRef),
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
        JobHandle outJobHandle1;
        JobHandle outJobHandle2;
        JobHandle deps;
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        RWHHouseholdFindPropertySystem.FindPropertyJob jobData2 = new RWHHouseholdFindPropertySystem.FindPropertyJob()
        {
            m_HomelessHouseholdEntities = this.m_HomelessHouseholdQuery.ToEntityListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle1),
            m_MovedInHouseholdEntities = this.m_HouseholdQuery.ToEntityListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle2),
            m_CachedPropertyInfo = nativeParallelHashMap,
            m_BuildingDatas = InternalCompilerInterface.GetComponentLookup<Game.Prefabs.BuildingData>(ref this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PropertiesOnMarket = InternalCompilerInterface.GetComponentLookup<PropertyOnMarket>(ref this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Availabilities = InternalCompilerInterface.GetBufferLookup<ResourceAvailability>(ref this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref this.CheckedStateRef),
            m_SpawnableDatas = InternalCompilerInterface.GetComponentLookup<SpawnableBuildingData>(ref this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_BuildingProperties = InternalCompilerInterface.GetComponentLookup<BuildingPropertyData>(ref this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Buildings = InternalCompilerInterface.GetComponentLookup<Building>(ref this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PathInformationBuffers = InternalCompilerInterface.GetBufferLookup<PathInformations>(ref this.__TypeHandle.__Game_Pathfind_PathInformations_RO_BufferLookup, ref this.CheckedStateRef),
            m_PrefabRefs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ServiceCoverages = InternalCompilerInterface.GetBufferLookup<Game.Net.ServiceCoverage>(ref this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref this.CheckedStateRef),
            m_Workers = InternalCompilerInterface.GetComponentLookup<Worker>(ref this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Students = InternalCompilerInterface.GetComponentLookup<Game.Citizens.Student>(ref this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PropertyRenters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref this.CheckedStateRef),
            m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup<HomelessHousehold>(ref this.__TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Citizens = InternalCompilerInterface.GetComponentLookup<Citizen>(ref this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Crimes = InternalCompilerInterface.GetComponentLookup<CrimeProducer>(ref this.__TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Lockeds = InternalCompilerInterface.GetComponentLookup<Locked>(ref this.__TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Transforms = InternalCompilerInterface.GetComponentLookup<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CityModifiers = InternalCompilerInterface.GetBufferLookup<CityModifier>(ref this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref this.CheckedStateRef),
            m_HealthProblems = InternalCompilerInterface.GetComponentLookup<HealthProblem>(ref this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Abandoneds = InternalCompilerInterface.GetComponentLookup<Abandoned>(ref this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Parks = InternalCompilerInterface.GetComponentLookup<Game.Buildings.Park>(ref this.__TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref this.CheckedStateRef),
            m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup<OwnedVehicle>(ref this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref this.CheckedStateRef),
            m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup<ElectricityConsumer>(ref this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_WaterConsumers = InternalCompilerInterface.GetComponentLookup<WaterConsumer>(ref this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_GarbageProducers = InternalCompilerInterface.GetComponentLookup<GarbageProducer>(ref this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_MailProducers = InternalCompilerInterface.GetComponentLookup<MailProducer>(ref this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup<CurrentBuilding>(ref this.__TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CurrentTransports = InternalCompilerInterface.GetComponentLookup<CurrentTransport>(ref this.__TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PathInformations = InternalCompilerInterface.GetComponentLookup<PathInformation>(ref this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CitizenBuffers = InternalCompilerInterface.GetBufferLookup<HouseholdCitizen>(ref this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref this.CheckedStateRef),
            m_PropertySeekers = InternalCompilerInterface.GetComponentLookup<PropertySeeker>(ref this.__TypeHandle.__Game_Agents_PropertySeeker_RW_ComponentLookup, ref this.CheckedStateRef),
            m_PollutionMap = map1,
            m_AirPollutionMap = map2,
            m_NoiseMap = map3,
            m_TelecomCoverages = data,
            m_ResidentialPropertyData = this.m_CountResidentialPropertySystem.GetResidentialPropertyData(),
            m_HealthcareParameters = this.m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
            m_ParkParameters = this.m_ParkParameterQuery.GetSingleton<ParkParameterData>(),
            m_EducationParameters = this.m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
            m_TelecomParameters = this.m_TelecomParameterQuery.GetSingleton<TelecomParameterData>(),
            m_GarbageParameters = this.m_GarbageParameterQuery.GetSingleton<GarbageParameterData>(),
            m_PoliceParameters = this.m_PoliceParameterQuery.GetSingleton<PoliceConfigurationData>(),
            m_CitizenHappinessParameterData = this.m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
            m_TaxRates = this.m_TaxSystem.GetTaxRates(),
            m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
            m_SimulationFrame = this.m_SimulationSystem.frameIndex,
            m_RentActionQueue = this.m_PropertyProcessingSystem.GetRentActionQueue(out deps).AsParallelWriter(),
            m_City = this.m_CitySystem.City,
            m_PathfindQueue = this.m_PathfindSetupSystem.GetQueue((object)this, 80 /*0x50*/, 16 /*0x10*/).AsParallelWriter(),
            m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer()
        };
        // ISSUE: reference to a compiler-generated field
        EntityQuery freePropertyQuery = this.m_FreePropertyQuery;
        JobHandle dependsOn = JobUtils.CombineDependencies(this.Dependency, dependencies1, dependencies3, dependencies2, dependencies4, deps);
        JobHandle job0 = jobData1.ScheduleParallel<RWHHouseholdFindPropertySystem.PreparePropertyJob>(freePropertyQuery, dependsOn);
        this.Dependency = jobData2.Schedule<RWHHouseholdFindPropertySystem.FindPropertyJob>(JobHandle.CombineDependencies(job0, outJobHandle2, outJobHandle1));
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_PathfindSetupSystem.AddQueueWriter(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_AirPollutionSystem.AddReader(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_NoisePollutionSystem.AddReader(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_GroundPollutionSystem.AddReader(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_TelecomCoverageSystem.AddReader(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_TriggerSystem.AddActionBufferWriter(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_CityStatisticsSystem.AddWriter(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_TaxSystem.AddReader(this.Dependency);
        nativeParallelHashMap.Dispose(this.Dependency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
        new EntityQueryBuilder((AllocatorManager.AllocatorHandle)Allocator.Temp).Dispose();
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        // ISSUE: reference to a compiler-generated method
        this.__AssignQueries(ref this.CheckedStateRef);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
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
        public ComponentLookup<Transform> m_Transforms;
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
            // ISSUE: reference to a compiler-generated field
            Entity prefab = this.m_Prefabs[property].m_Prefab;
            int free = 0;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            if (this.m_BuildingDatas.HasComponent(prefab) && (this.m_Abandoneds.HasComponent(property) || this.m_Parks.HasComponent(property) && this.m_ParkDatas[prefab].m_AllowHomeless))
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                free = Game.Buildings.BuildingUtils.GetShelterHomelessCapacity(prefab, ref this.m_BuildingDatas, ref this.m_BuildingPropertyData) - this.m_Renters[property].Length;
            }
            else
            {
                // ISSUE: reference to a compiler-generated field
                if (this.m_BuildingProperties.HasComponent(prefab))
                {
                    // ISSUE: reference to a compiler-generated field
                    BuildingPropertyData buildingProperty = this.m_BuildingProperties[prefab];
                    // ISSUE: reference to a compiler-generated field
                    DynamicBuffer<Renter> renter = this.m_Renters[property];
                    free = buildingProperty.CountProperties(AreaType.Residential);
                    for (int index = 0; index < renter.Length; ++index)
                    {
                        // ISSUE: reference to a compiler-generated field
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
            // ISSUE: reference to a compiler-generated field
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
            for (int index = 0; index < nativeArray.Length; ++index)
            {
                Entity entity = nativeArray[index];
                // ISSUE: reference to a compiler-generated method
                int free = this.CalculateFree(entity);
                if (free > 0)
                {
                    // ISSUE: reference to a compiler-generated field
                    Entity prefab = this.m_Prefabs[entity].m_Prefab;
                    // ISSUE: reference to a compiler-generated field
                    Building building = this.m_Buildings[entity];
                    // ISSUE: reference to a compiler-generated field
                    Entity healthcareServicePrefab = this.m_HealthcareParameters.m_HealthcareServicePrefab;
                    // ISSUE: reference to a compiler-generated field
                    Entity parkServicePrefab = this.m_ParkParameters.m_ParkServicePrefab;
                    // ISSUE: reference to a compiler-generated field
                    Entity educationServicePrefab = this.m_EducationParameters.m_EducationServicePrefab;
                    // ISSUE: reference to a compiler-generated field
                    Entity telecomServicePrefab = this.m_TelecomParameters.m_TelecomServicePrefab;
                    // ISSUE: reference to a compiler-generated field
                    Entity garbageServicePrefab = this.m_GarbageParameters.m_GarbageServicePrefab;
                    // ISSUE: reference to a compiler-generated field
                    Entity policeServicePrefab = this.m_PoliceParameters.m_PoliceServicePrefab;
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    DynamicBuffer<CityModifier> cityModifier = this.m_CityModifiers[this.m_City];
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: variable of a compiler-generated type
                    HouseholdFindPropertySystem.GenericApartmentQuality apartmentQuality = PropertyUtils.GetGenericApartmentQuality(entity, prefab, ref building, ref this.m_BuildingProperties, ref this.m_BuildingDatas, ref this.m_SpawnableDatas, ref this.m_Crimes, ref this.m_ServiceCoverages, ref this.m_Locked, ref this.m_ElectricityConsumers, ref this.m_WaterConsumers, ref this.m_GarbageProducers, ref this.m_MailProducers, ref this.m_Transforms, ref this.m_Abandoneds, this.m_PollutionMap, this.m_AirPollutionMap, this.m_NoiseMap, this.m_TelecomCoverages, cityModifier, healthcareServicePrefab, parkServicePrefab, educationServicePrefab, telecomServicePrefab, garbageServicePrefab, policeServicePrefab, this.m_CitizenHappinessParameterData, this.m_GarbageParameters);
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: object of a compiler-generated type is created
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
            // ISSUE: reference to a compiler-generated method
            this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    [BurstCompile]
    private struct FindPropertyJob : IJob
    {
        public NativeList<Entity> m_HomelessHouseholdEntities;
        public NativeList<Entity> m_MovedInHouseholdEntities;
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
        public ComponentLookup<Transform> m_Transforms;
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
        public ComponentLookup<PathInformation> m_PathInformations;
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
        public NativeArray<int> m_TaxRates;
        [ReadOnly]
        public CountResidentialPropertySystem.ResidentialPropertyData m_ResidentialPropertyData;
        [ReadOnly]
        public HealthcareParameterData m_HealthcareParameters;
        [ReadOnly]
        public ParkParameterData m_ParkParameters;
        [ReadOnly]
        public EducationParameterData m_EducationParameters;
        [ReadOnly]
        public TelecomParameterData m_TelecomParameters;
        [ReadOnly]
        public GarbageParameterData m_GarbageParameters;
        [ReadOnly]
        public PoliceConfigurationData m_PoliceParameters;
        [ReadOnly]
        public CitizenHappinessParameterData m_CitizenHappinessParameterData;
        [ReadOnly]
        public EconomyParameterData m_EconomyParameters;
        [ReadOnly]
        public uint m_SimulationFrame;
        public EntityCommandBuffer m_CommandBuffer;
        [ReadOnly]
        public Entity m_City;
        public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
        public NativeQueue<RentAction>.ParallelWriter m_RentActionQueue;

        private void StartHomeFinding(
          Entity household,
          Entity commuteCitizen,
          Entity targetLocation,
          Entity oldHome,
          float minimumScore,
          bool targetIsOrigin,
          DynamicBuffer<HouseholdCitizen> citizens)
        {
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.AddComponent<PathInformation>(household, new PathInformation()
            {
                m_State = PathFlags.Pending
            });
            // ISSUE: reference to a compiler-generated field
            Household household1 = this.m_Households[household];
            PathfindWeights pathfindWeights = new PathfindWeights();
            Citizen componentData;
            // ISSUE: reference to a compiler-generated field
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
            // ISSUE: reference to a compiler-generated field
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
            setupQueueTarget.m_Entity = targetLocation;
            SetupQueueTarget a = setupQueueTarget;
            setupQueueTarget = new SetupQueueTarget();
            setupQueueTarget.m_Type = SetupTargetType.FindHome;
            setupQueueTarget.m_Methods = PathMethod.Pedestrian;
            setupQueueTarget.m_Entity = household;
            setupQueueTarget.m_Entity2 = oldHome;
            setupQueueTarget.m_Value2 = minimumScore;
            SetupQueueTarget b = setupQueueTarget;
            DynamicBuffer<OwnedVehicle> bufferData;
            // ISSUE: reference to a compiler-generated field
            if (this.m_OwnedVehicles.TryGetBuffer(household, out bufferData) && bufferData.Length != 0)
            {
                parameters.m_Methods |= targetIsOrigin ? PathMethod.Road | PathMethod.MediumRoad : PathMethod.Road | PathMethod.Parking | PathMethod.MediumRoad;
                parameters.m_ParkingSize = (float2)float.MinValue;
                parameters.m_IgnoredRules |= RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidSlowTraffic;
                a.m_Methods |= PathMethod.Road | PathMethod.MediumRoad;
                a.m_RoadTypes |= RoadTypes.Car;
                b.m_Methods |= PathMethod.Road | PathMethod.MediumRoad;
                b.m_RoadTypes |= RoadTypes.Car;
            }
            if (targetIsOrigin)
            {
                parameters.m_MaxSpeed.y = 277.777771f;
                parameters.m_Methods |= PathMethod.Taxi | PathMethod.PublicTransportNight;
                parameters.m_SecondaryIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults();
            }
            else
                CommonUtils.Swap<SetupQueueTarget>(ref a, ref b);
            parameters.m_MaxResultCount = 10;
            parameters.m_PathfindFlags |= targetIsOrigin ? PathfindFlags.MultipleDestinations : PathfindFlags.MultipleOrigins;
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.AddBuffer<PathInformations>(household).Add(new PathInformations()
            {
                m_State = PathFlags.Pending
            });
            // ISSUE: reference to a compiler-generated field
            this.m_PathfindQueue.Enqueue(new SetupQueueItem(household, parameters, a, b));
        }

        private Entity GetFirstWorkplaceOrSchool(
          DynamicBuffer<HouseholdCitizen> citizens,
          ref Entity citizen)
        {
            for (int index = 0; index < citizens.Length; ++index)
            {
                citizen = citizens[index].m_Citizen;
                // ISSUE: reference to a compiler-generated field
                if (this.m_Workers.HasComponent(citizen))
                {
                    // ISSUE: reference to a compiler-generated field
                    return this.m_Workers[citizen].m_Workplace;
                }
                // ISSUE: reference to a compiler-generated field
                if (this.m_Students.HasComponent(citizen))
                {
                    // ISSUE: reference to a compiler-generated field
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
                // ISSUE: reference to a compiler-generated field
                if (this.m_CurrentBuildings.TryGetComponent(citizens[index].m_Citizen, out componentData1))
                    return componentData1.m_CurrentBuilding;
                CurrentTransport componentData2;
                // ISSUE: reference to a compiler-generated field
                if (this.m_CurrentTransports.TryGetComponent(citizens[index].m_Citizen, out componentData2))
                    return componentData2.m_CurrentTransport;
            }
            return Entity.Null;
        }

        public void Execute()
        {
            int num = 0;
            // ISSUE: reference to a compiler-generated field
            for (int index = 0; index < this.m_HomelessHouseholdEntities.Length; ++index)
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated method
                if (this.ProcessFindHome(this.m_HomelessHouseholdEntities[index]))
                    ++num;
                // ISSUE: reference to a compiler-generated field
                if (num >= RWHHouseholdFindPropertySystem.kMaxProcessEntitiesPerUpdate / 2)
                    break;
            }
            // ISSUE: reference to a compiler-generated field
            for (int index = 0; index < this.m_MovedInHouseholdEntities.Length; ++index)
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated method
                if (this.ProcessFindHome(this.m_MovedInHouseholdEntities[index]))
                    ++num;
                // ISSUE: reference to a compiler-generated field
                if (num >= RWHHouseholdFindPropertySystem.kMaxProcessEntitiesPerUpdate)
                    break;
            }

            //Mod.log.Info($"Processed {num} households for finding properties.");
        }

        private bool ProcessFindHome(Entity householdEntity)
        {
            // ISSUE: reference to a compiler-generated field
            DynamicBuffer<HouseholdCitizen> citizenBuffer = this.m_CitizenBuffers[householdEntity];
            if (citizenBuffer.Length == 0)
                return false;
            // ISSUE: reference to a compiler-generated field
            PropertySeeker propertySeeker = this.m_PropertySeekers[householdEntity];
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            int householdIncome = EconomyUtils.GetHouseholdIncome(citizenBuffer, ref this.m_Workers, ref this.m_Citizens, ref this.m_HealthProblems, ref this.m_EconomyParameters, this.m_TaxRates);
            DynamicBuffer<PathInformations> bufferData;
            // ISSUE: reference to a compiler-generated field
            if (this.m_PathInformationBuffers.TryGetBuffer(householdEntity, out bufferData))
            {
                // ISSUE: reference to a compiler-generated method
                this.ProcessPathInformations(householdEntity, bufferData, propertySeeker, citizenBuffer, householdIncome);
                return false;
            }
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            if ((long)propertySeeker.m_LastPropertySeekFrame + (long)RWHHouseholdFindPropertySystem.kFindPropertyCoolDown > (long)this.m_SimulationFrame)
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (this.m_PathInformations[householdEntity].m_State != PathFlags.Pending && math.csum(this.m_ResidentialPropertyData.m_FreeProperties) < 10)
                {
                    // ISSUE: reference to a compiler-generated field
                    CitizenUtils.HouseholdMoveAway(this.m_CommandBuffer, householdEntity);
                }
                return false;
            }
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            Entity householdHomeBuilding = Game.Buildings.BuildingUtils.GetHouseholdHomeBuilding(householdEntity, ref this.m_PropertyRenters, ref this.m_HomelessHouseholds);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            float num = householdHomeBuilding != Entity.Null ? PropertyUtils.GetPropertyScore(householdHomeBuilding, householdEntity, citizenBuffer, ref this.m_PrefabRefs, ref this.m_BuildingProperties, ref this.m_Buildings, ref this.m_BuildingDatas, ref this.m_Households, ref this.m_Citizens, ref this.m_Students, ref this.m_Workers, ref this.m_SpawnableDatas, ref this.m_Crimes, ref this.m_ServiceCoverages, ref this.m_Lockeds, ref this.m_ElectricityConsumers, ref this.m_WaterConsumers, ref this.m_GarbageProducers, ref this.m_MailProducers, ref this.m_Transforms, ref this.m_Abandoneds, ref this.m_Parks, ref this.m_Availabilities, this.m_TaxRates, this.m_PollutionMap, this.m_AirPollutionMap, this.m_NoiseMap, this.m_TelecomCoverages, this.m_CityModifiers[this.m_City], this.m_HealthcareParameters.m_HealthcareServicePrefab, this.m_ParkParameters.m_ParkServicePrefab, this.m_EducationParameters.m_EducationServicePrefab, this.m_TelecomParameters.m_TelecomServicePrefab, this.m_GarbageParameters.m_GarbageServicePrefab, this.m_PoliceParameters.m_PoliceServicePrefab, this.m_CitizenHappinessParameterData, this.m_GarbageParameters) : float.NegativeInfinity;
            Entity citizen = Entity.Null;
            // ISSUE: reference to a compiler-generated method
            Entity workplaceOrSchool = this.GetFirstWorkplaceOrSchool(citizenBuffer, ref citizen);
            bool targetIsOrigin = workplaceOrSchool == Entity.Null;
            // ISSUE: reference to a compiler-generated method
            Entity targetLocation = targetIsOrigin ? this.GetCurrentLocation(citizenBuffer) : workplaceOrSchool;
            if (targetLocation == Entity.Null)
            {
                // ISSUE: reference to a compiler-generated field
                CitizenUtils.HouseholdMoveAway(this.m_CommandBuffer, householdEntity);
                return false;
            }
            propertySeeker.m_TargetProperty = workplaceOrSchool;
            propertySeeker.m_BestProperty = householdHomeBuilding;
            propertySeeker.m_BestPropertyScore = num;
            // ISSUE: reference to a compiler-generated field
            propertySeeker.m_LastPropertySeekFrame = this.m_SimulationFrame;
            // ISSUE: reference to a compiler-generated field
            this.m_PropertySeekers[householdEntity] = propertySeeker;
            // ISSUE: reference to a compiler-generated method
            this.StartHomeFinding(householdEntity, citizen, targetLocation, householdHomeBuilding, propertySeeker.m_BestPropertyScore, targetIsOrigin, citizenBuffer);
            return true;
        }

        private void ProcessPathInformations(
          Entity householdEntity,
          DynamicBuffer<PathInformations> pathInformations,
          PropertySeeker propertySeeker,
          DynamicBuffer<HouseholdCitizen> citizens,
          int income)
        {
            int index = 0;
            PathInformations pathInformation1 = pathInformations[index];
            if ((pathInformation1.m_State & PathFlags.Pending) != (PathFlags)0)
                return;
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.RemoveComponent<PathInformations>(householdEntity);
            bool flag1 = propertySeeker.m_TargetProperty != Entity.Null;
            Entity entity = flag1 ? pathInformation1.m_Origin : pathInformation1.m_Destination;
            bool flag2 = false;
            PathInformations pathInformation2;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            for (; !this.m_CachedPropertyInfo.ContainsKey(entity) || this.m_CachedPropertyInfo[entity].free <= 0; entity = flag1 ? pathInformation2.m_Origin : pathInformation2.m_Destination)
            {
                ++index;
                if (pathInformations.Length > index)
                {
                    pathInformation2 = pathInformations[index];
                }
                else
                {
                    entity = Entity.Null;
                    flag2 = true;
                    break;
                }
            }
            if (flag2 && pathInformations.Length != 0 && pathInformations[0].m_Destination != Entity.Null)
                return;
            float num = float.NegativeInfinity;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            if (entity != Entity.Null && this.m_CachedPropertyInfo.ContainsKey(entity) && this.m_CachedPropertyInfo[entity].free > 0)
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                num = PropertyUtils.GetPropertyScore(entity, householdEntity, citizens, ref this.m_PrefabRefs, ref this.m_BuildingProperties, ref this.m_Buildings, ref this.m_BuildingDatas, ref this.m_Households, ref this.m_Citizens, ref this.m_Students, ref this.m_Workers, ref this.m_SpawnableDatas, ref this.m_Crimes, ref this.m_ServiceCoverages, ref this.m_Lockeds, ref this.m_ElectricityConsumers, ref this.m_WaterConsumers, ref this.m_GarbageProducers, ref this.m_MailProducers, ref this.m_Transforms, ref this.m_Abandoneds, ref this.m_Parks, ref this.m_Availabilities, this.m_TaxRates, this.m_PollutionMap, this.m_AirPollutionMap, this.m_NoiseMap, this.m_TelecomCoverages, this.m_CityModifiers[this.m_City], this.m_HealthcareParameters.m_HealthcareServicePrefab, this.m_ParkParameters.m_ParkServicePrefab, this.m_EducationParameters.m_EducationServicePrefab, this.m_TelecomParameters.m_TelecomServicePrefab, this.m_GarbageParameters.m_GarbageServicePrefab, this.m_PoliceParameters.m_PoliceServicePrefab, this.m_CitizenHappinessParameterData, this.m_GarbageParameters);
            }
            if ((double)num < (double)propertySeeker.m_BestPropertyScore)
                entity = propertySeeker.m_BestProperty;
            // ISSUE: reference to a compiler-generated field
            bool flag3 = (this.m_Households[householdEntity].m_Flags & HouseholdFlags.MovedIn) != 0;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            bool flag4 = entity != Entity.Null && Game.Buildings.BuildingUtils.IsHomelessShelterBuilding(entity, ref this.m_Parks, ref this.m_Abandoneds);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            bool flag5 = CitizenUtils.IsHouseholdNeedSupport(citizens, ref this.m_Citizens, ref this.m_Students);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            bool flag6 = this.m_PropertiesOnMarket.HasComponent(entity) && (flag5 || this.m_PropertiesOnMarket[entity].m_AskingRent < income);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            bool flag7 = !this.m_PropertyRenters.HasComponent(householdEntity) || !this.m_PropertyRenters[householdEntity].m_Property.Equals(entity);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            Entity householdHomeBuilding = Game.Buildings.BuildingUtils.GetHouseholdHomeBuilding(householdEntity, ref this.m_PropertyRenters, ref this.m_HomelessHouseholds);
            if (householdHomeBuilding != Entity.Null && householdHomeBuilding == entity)
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (!this.m_HomelessHouseholds.HasComponent(householdEntity) && !flag5 && income < this.m_PropertyRenters[householdEntity].m_Rent)
                {
                    // ISSUE: reference to a compiler-generated field
                    CitizenUtils.HouseholdMoveAway(this.m_CommandBuffer, householdEntity);
                }
                else
                {
                    // ISSUE: reference to a compiler-generated field
                    this.m_CommandBuffer.SetComponentEnabled<PropertySeeker>(householdEntity, false);
                }
            }
            else if (flag6 & flag7 || flag3 & flag4)
            {
                // ISSUE: reference to a compiler-generated field
                this.m_RentActionQueue.Enqueue(new RentAction()
                {
                    m_Property = entity,
                    m_Renter = householdEntity
                });
                // ISSUE: reference to a compiler-generated field
                if (this.m_CachedPropertyInfo.ContainsKey(entity))
                {
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: variable of a compiler-generated type
                    RWHHouseholdFindPropertySystem.CachedPropertyInformation propertyInformation = this.m_CachedPropertyInfo[entity];
                    // ISSUE: reference to a compiler-generated field
                    --propertyInformation.free;
                    // ISSUE: reference to a compiler-generated field
                    this.m_CachedPropertyInfo[entity] = propertyInformation;
                }
                // ISSUE: reference to a compiler-generated field
                this.m_CommandBuffer.SetComponentEnabled<PropertySeeker>(householdEntity, false);
            }
            else
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (entity == Entity.Null && (!this.m_HomelessHouseholds.HasComponent(householdEntity) || this.m_HomelessHouseholds[householdEntity].m_TempHome == Entity.Null))
                {
                    // ISSUE: reference to a compiler-generated field
                    CitizenUtils.HouseholdMoveAway(this.m_CommandBuffer, householdEntity);
                }
                else
                {
                    propertySeeker.m_BestProperty = new Entity();
                    propertySeeker.m_BestPropertyScore = float.NegativeInfinity;
                    // ISSUE: reference to a compiler-generated field
                    this.m_PropertySeekers[householdEntity] = propertySeeker;
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
        public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;
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
        public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;
        public ComponentLookup<PropertySeeker> __Game_Agents_PropertySeeker_RW_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            // ISSUE: reference to a compiler-generated field
            this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_BuildingPropertyData_RW_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.BuildingData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_CrimeProducer_RO_ComponentLookup = state.GetComponentLookup<CrimeProducer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Pathfind_PathInformations_RO_BufferLookup = state.GetBufferLookup<PathInformations>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Agents_PropertySeeker_RW_ComponentLookup = state.GetComponentLookup<PropertySeeker>();
        }
    }
}
