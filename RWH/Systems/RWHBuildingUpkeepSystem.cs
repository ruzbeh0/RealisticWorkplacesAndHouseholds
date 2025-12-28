// Decompiled with JetBrains decompiler
// Type: Game.Simulation.RWHBuildingUpkeepSystem
// Assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7F460502-8735-4AB5-92B4-3D83EE79E8D6
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed\Game.dll

using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Notifications;
using Game.Objects;
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
using Game;

#nullable disable
namespace RWH.Systems;

//[CompilerGenerated]
public partial class RWHBuildingUpkeepSystem : GameSystemBase
{
    public static readonly int kUpdatesPerDay = 16 /*0x10*/;
    public static readonly int kMaterialUpkeep = 4;
    private SimulationSystem m_SimulationSystem;
    private EndFrameBarrier m_EndFrameBarrier;
    private ResourceSystem m_ResourceSystem;
    private ClimateSystem m_ClimateSystem;
    private CitySystem m_CitySystem;
    private IconCommandSystem m_IconCommandSystem;
    private TriggerSystem m_TriggerSystem;
    private ZoneBuiltRequirementSystem m_ZoneBuiltRequirementSystemSystem;
    private Game.Zones.SearchSystem m_ZoneSearchSystem;
    private ElectricityRoadConnectionGraphSystem m_ElectricityRoadConnectionGraphSystem;
    private WaterPipeRoadConnectionGraphSystem m_WaterPipeRoadConnectionGraphSystem;
    private CityProductionStatisticSystem m_CityProductionStatisticSystem;
    private NativeQueue<RWHBuildingUpkeepSystem.UpkeepPayment> m_UpkeepExpenseQueue;
    private NativeQueue<RWHBuildingUpkeepSystem.LevelUpMaterial> m_LevelUpMaterialQueue;
    private NativeQueue<Entity> m_LevelupQueue;
    private NativeQueue<Entity> m_LeveldownQueue;
    private EntityQuery m_BuildingPrefabGroup;
    private EntityQuery m_BuildingSettingsQuery;
    private EntityQuery m_BuildingGroup;
    private EntityQuery m_ResourceNeedingBuildingGroup;
    private EntityArchetype m_GoodsDeliveryRequestArchetype;
    public bool debugFastLeveling;
    private RWHBuildingUpkeepSystem.TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        // ISSUE: reference to a compiler-generated field
        return 262144 /*0x040000*/ / (RWHBuildingUpkeepSystem.kUpdatesPerDay * 16 /*0x10*/);
    }

    public static float GetHeatingMultiplier(float temperature) => math.max(0.0f, 15f - temperature);

    [UnityEngine.Scripting.Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        // ISSUE: reference to a compiler-generated field
        this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        // ISSUE: reference to a compiler-generated field
        this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_ClimateSystem = this.World.GetOrCreateSystemManaged<ClimateSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_IconCommandSystem = this.World.GetOrCreateSystemManaged<IconCommandSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_TriggerSystem = this.World.GetOrCreateSystemManaged<TriggerSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_ZoneBuiltRequirementSystemSystem = this.World.GetOrCreateSystemManaged<ZoneBuiltRequirementSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_ZoneSearchSystem = this.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_ElectricityRoadConnectionGraphSystem = this.World.GetOrCreateSystemManaged<ElectricityRoadConnectionGraphSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_WaterPipeRoadConnectionGraphSystem = this.World.GetOrCreateSystemManaged<WaterPipeRoadConnectionGraphSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_CityProductionStatisticSystem = this.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
        // ISSUE: reference to a compiler-generated field
        this.m_UpkeepExpenseQueue = new NativeQueue<RWHBuildingUpkeepSystem.UpkeepPayment>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
        // ISSUE: reference to a compiler-generated field
        this.m_LevelUpMaterialQueue = new NativeQueue<RWHBuildingUpkeepSystem.LevelUpMaterial>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
        // ISSUE: reference to a compiler-generated field
        this.m_BuildingSettingsQuery = this.GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>(), ComponentType.ReadOnly<ZoneLevelUpResourceData>());
        // ISSUE: reference to a compiler-generated field
        this.m_LevelupQueue = new NativeQueue<Entity>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
        // ISSUE: reference to a compiler-generated field
        this.m_LeveldownQueue = new NativeQueue<Entity>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
        // ISSUE: reference to a compiler-generated field
        this.m_BuildingGroup = this.GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[3]
          {
        ComponentType.ReadOnly<BuildingCondition>(),
        ComponentType.ReadOnly<PrefabRef>(),
        ComponentType.ReadOnly<UpdateFrame>()
          },
            Any = new ComponentType[0],
            None = new ComponentType[5]
          {
        ComponentType.ReadOnly<Abandoned>(),
        ComponentType.ReadOnly<Destroyed>(),
        ComponentType.ReadOnly<Deleted>(),
        ComponentType.ReadOnly<Temp>(),
        ComponentType.ReadWrite<ResourceNeeding>()
          }
        });
        // ISSUE: reference to a compiler-generated field
        this.m_ResourceNeedingBuildingGroup = this.GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[4]
          {
        ComponentType.ReadWrite<ResourceNeeding>(),
        ComponentType.ReadOnly<BuildingCondition>(),
        ComponentType.ReadOnly<PrefabRef>(),
        ComponentType.ReadOnly<UpdateFrame>()
          },
            Any = new ComponentType[0],
            None = new ComponentType[4]
          {
        ComponentType.ReadOnly<Abandoned>(),
        ComponentType.ReadOnly<Destroyed>(),
        ComponentType.ReadOnly<Deleted>(),
        ComponentType.ReadOnly<Temp>()
          }
        });
        // ISSUE: reference to a compiler-generated field
        this.m_BuildingPrefabGroup = this.GetEntityQuery(ComponentType.ReadOnly<Game.Prefabs.BuildingData>(), ComponentType.ReadOnly<BuildingSpawnGroupData>(), ComponentType.ReadOnly<PrefabData>());
        // ISSUE: reference to a compiler-generated field
        this.m_GoodsDeliveryRequestArchetype = this.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<GoodsDeliveryRequest>(), ComponentType.ReadWrite<RequestGroup>());
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_BuildingGroup);
        // ISSUE: reference to a compiler-generated field
        this.RequireForUpdate(this.m_BuildingSettingsQuery);
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnDestroy()
    {
        base.OnDestroy();
        // ISSUE: reference to a compiler-generated field
        this.m_UpkeepExpenseQueue.Dispose();
        // ISSUE: reference to a compiler-generated field
        this.m_LevelUpMaterialQueue.Dispose();
        // ISSUE: reference to a compiler-generated field
        this.m_LevelupQueue.Dispose();
        // ISSUE: reference to a compiler-generated field
        this.m_LeveldownQueue.Dispose();
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnUpdate()
    {
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        this.m_BuildingGroup.SetSharedComponentFilter<UpdateFrame>(new UpdateFrame(SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, RWHBuildingUpkeepSystem.kUpdatesPerDay, 16 /*0x10*/)));
        // ISSUE: reference to a compiler-generated field
        BuildingConfigurationData singleton = this.m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>();
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
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        RWHBuildingUpkeepSystem.BuildingUpkeepJob jobData1 = new RWHBuildingUpkeepSystem.BuildingUpkeepJob()
        {
            m_ConditionType = InternalCompilerInterface.GetComponentTypeHandle<BuildingCondition>(ref this.__TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle, ref this.CheckedStateRef),
            m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
            m_RenterType = InternalCompilerInterface.GetBufferTypeHandle<Renter>(ref this.__TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref this.CheckedStateRef),
            m_ConsumptionDatas = InternalCompilerInterface.GetComponentLookup<ConsumptionData>(ref this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Availabilities = InternalCompilerInterface.GetBufferLookup<ResourceAvailability>(ref this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref this.CheckedStateRef),
            m_LevelUpResourceDataBufs = InternalCompilerInterface.GetBufferLookup<LevelUpResourceData>(ref this.__TypeHandle.__Game_Prefabs_LevelUpResourceData_RO_BufferLookup, ref this.CheckedStateRef),
            m_ZoneLevelUpResourceDataBufs = InternalCompilerInterface.GetBufferLookup<ZoneLevelUpResourceData>(ref this.__TypeHandle.__Game_Prefabs_ZoneLevelUpResourceData_RO_BufferLookup, ref this.CheckedStateRef),
            m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup<BuildingPropertyData>(ref this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_CityModifierBufs = InternalCompilerInterface.GetBufferLookup<CityModifier>(ref this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref this.CheckedStateRef),
            m_SignatureDatas = InternalCompilerInterface.GetComponentLookup<SignatureBuildingData>(ref this.__TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Abandoned = InternalCompilerInterface.GetComponentLookup<Abandoned>(ref this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Destroyed = InternalCompilerInterface.GetComponentLookup<Destroyed>(ref this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref this.CheckedStateRef),
            m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup<SpawnableBuildingData>(ref this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ZoneDatas = InternalCompilerInterface.GetComponentLookup<ZoneData>(ref this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref this.CheckedStateRef),
            m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup<OwnedVehicle>(ref this.__TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref this.CheckedStateRef),
            m_LayoutElements = InternalCompilerInterface.GetBufferLookup<LayoutElement>(ref this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref this.CheckedStateRef),
            m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup<Game.Vehicles.DeliveryTruck>(ref this.__TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PrefabRefs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
            m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup<IndustrialProcessData>(ref this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup<ServiceAvailable>(ref this.__TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref this.CheckedStateRef),
            m_City = this.m_CitySystem.City,
            m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
            m_ResourceDatas = InternalCompilerInterface.GetComponentLookup<ResourceData>(ref this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Resources = InternalCompilerInterface.GetBufferLookup<Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref this.CheckedStateRef),
            m_BuildingConfigurationData = singleton,
            m_BuildingConfigLevelResourceBuf = this.m_BuildingSettingsQuery.GetSingletonBuffer<ZoneLevelUpResourceData>(true),
            m_TemperatureUpkeep = RWHBuildingUpkeepSystem.GetHeatingMultiplier((float)this.m_ClimateSystem.temperature),
            m_DebugFastLeveling = this.debugFastLeveling,
            m_GoodsDeliveryRequestArchetype = this.m_GoodsDeliveryRequestArchetype,
            m_UpkeepExpenseQueue = this.m_UpkeepExpenseQueue.AsParallelWriter(),
            m_LevelDownQueue = this.m_LeveldownQueue.AsParallelWriter(),
            m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            m_IconCommandBuffer = this.m_IconCommandSystem.CreateCommandBuffer()
        };
        // ISSUE: reference to a compiler-generated field
        this.Dependency = jobData1.ScheduleParallel<RWHBuildingUpkeepSystem.BuildingUpkeepJob>(this.m_BuildingGroup, this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_IconCommandSystem.AddCommandBufferWriter(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_ResourceSystem.AddPrefabsReader(this.Dependency);
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
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        RWHBuildingUpkeepSystem.ResourceNeedingUpkeepJob jobData2 = new RWHBuildingUpkeepSystem.ResourceNeedingUpkeepJob()
        {
            m_ConditionType = InternalCompilerInterface.GetComponentTypeHandle<BuildingCondition>(ref this.__TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle, ref this.CheckedStateRef),
            m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
            m_ResourceNeedingType = InternalCompilerInterface.GetBufferTypeHandle<ResourceNeeding>(ref this.__TypeHandle.__Game_Buildings_ResourceNeeding_RW_BufferTypeHandle, ref this.CheckedStateRef),
            m_GuestVehicleBufs = InternalCompilerInterface.GetBufferLookup<GuestVehicle>(ref this.__TypeHandle.__Game_Vehicles_GuestVehicle_RO_BufferLookup, ref this.CheckedStateRef),
            m_BuildingConfigurationData = singleton,
            m_LeveUpMaterialQueue = this.m_LevelUpMaterialQueue.AsParallelWriter(),
            m_LevelupQueue = this.m_LevelupQueue.AsParallelWriter(),
            m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            m_IconCommandBuffer = this.m_IconCommandSystem.CreateCommandBuffer()
        };
        // ISSUE: reference to a compiler-generated field
        this.Dependency = jobData2.ScheduleParallel<RWHBuildingUpkeepSystem.ResourceNeedingUpkeepJob>(this.m_ResourceNeedingBuildingGroup, this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        JobHandle outJobHandle;
        JobHandle dependencies;
        JobHandle deps1;
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
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        RWHBuildingUpkeepSystem.LevelupJob jobData3 = new RWHBuildingUpkeepSystem.LevelupJob()
        {
            m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, ref this.CheckedStateRef),
            m_SpawnableBuildingType = InternalCompilerInterface.GetComponentTypeHandle<SpawnableBuildingData>(ref this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle<Game.Prefabs.BuildingData>(ref this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_BuildingPropertyType = InternalCompilerInterface.GetComponentTypeHandle<BuildingPropertyData>(ref this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_ObjectGeometryType = InternalCompilerInterface.GetComponentTypeHandle<ObjectGeometryData>(ref this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle, ref this.CheckedStateRef),
            m_BuildingSpawnGroupType = InternalCompilerInterface.GetSharedComponentTypeHandle<BuildingSpawnGroupData>(ref this.__TypeHandle.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle, ref this.CheckedStateRef),
            m_TransformData = InternalCompilerInterface.GetComponentLookup<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref this.CheckedStateRef),
            m_BlockData = InternalCompilerInterface.GetComponentLookup<Game.Zones.Block>(ref this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ValidAreaData = InternalCompilerInterface.GetComponentLookup<ValidArea>(ref this.__TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Prefabs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
            m_PrefabDatas = InternalCompilerInterface.GetComponentLookup<PrefabData>(ref this.__TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_SpawnableBuildings = InternalCompilerInterface.GetComponentLookup<SpawnableBuildingData>(ref this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Buildings = InternalCompilerInterface.GetComponentLookup<Game.Prefabs.BuildingData>(ref this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup<BuildingPropertyData>(ref this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_OfficeBuilding = InternalCompilerInterface.GetComponentLookup<OfficeBuilding>(ref this.__TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup, ref this.CheckedStateRef),
            m_ZoneData = InternalCompilerInterface.GetComponentLookup<ZoneData>(ref this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Cells = InternalCompilerInterface.GetBufferLookup<Cell>(ref this.__TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref this.CheckedStateRef),
            m_BuildingConfigurationData = singleton,
            m_SpawnableBuildingChunks = this.m_BuildingPrefabGroup.ToArchetypeChunkListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle),
            m_ZoneSearchTree = this.m_ZoneSearchSystem.GetSearchTree(true, out dependencies),
            m_RandomSeed = RandomSeed.Next(),
            m_IconCommandBuffer = this.m_IconCommandSystem.CreateCommandBuffer(),
            m_LevelupQueue = this.m_LevelupQueue,
            m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer(),
            m_TriggerBuffer = this.m_TriggerSystem.CreateActionBuffer(),
            m_ZoneBuiltLevelQueue = this.m_ZoneBuiltRequirementSystemSystem.GetZoneBuiltLevelQueue(out deps1)
        };
        this.Dependency = jobData3.Schedule<RWHBuildingUpkeepSystem.LevelupJob>(JobUtils.CombineDependencies(this.Dependency, outJobHandle, dependencies, deps1));
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_ZoneSearchSystem.AddSearchTreeReader(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_ZoneBuiltRequirementSystemSystem.AddWriter(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_TriggerSystem.AddActionBufferWriter(this.Dependency);
        JobHandle deps2;
        JobHandle deps3;
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
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        // ISSUE: reference to a compiler-generated field
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        RWHBuildingUpkeepSystem.LeveldownJob jobData4 = new RWHBuildingUpkeepSystem.LeveldownJob()
        {
            m_BuildingDatas = InternalCompilerInterface.GetComponentLookup<Game.Prefabs.BuildingData>(ref this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Prefabs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
            m_SpawnableBuildings = InternalCompilerInterface.GetComponentLookup<SpawnableBuildingData>(ref this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_Buildings = InternalCompilerInterface.GetComponentLookup<Building>(ref this.__TypeHandle.__Game_Buildings_Building_RW_ComponentLookup, ref this.CheckedStateRef),
            m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup<ElectricityConsumer>(ref this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_GarbageProducers = InternalCompilerInterface.GetComponentLookup<GarbageProducer>(ref this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_MailProducers = InternalCompilerInterface.GetComponentLookup<MailProducer>(ref this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_WaterConsumers = InternalCompilerInterface.GetComponentLookup<WaterConsumer>(ref this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref this.CheckedStateRef),
            m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup<BuildingPropertyData>(ref this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref this.CheckedStateRef),
            m_OfficeBuilding = InternalCompilerInterface.GetComponentLookup<OfficeBuilding>(ref this.__TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup, ref this.CheckedStateRef),
            m_TriggerBuffer = this.m_TriggerSystem.CreateActionBuffer(),
            m_CrimeProducers = InternalCompilerInterface.GetComponentLookup<CrimeProducer>(ref this.__TypeHandle.__Game_Buildings_CrimeProducer_RW_ComponentLookup, ref this.CheckedStateRef),
            m_Renters = InternalCompilerInterface.GetBufferLookup<Renter>(ref this.__TypeHandle.__Game_Buildings_Renter_RW_BufferLookup, ref this.CheckedStateRef),
            m_BuildingConfigurationData = singleton,
            m_LeveldownQueue = this.m_LeveldownQueue,
            m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer(),
            m_UpdatedElectricityRoadEdges = this.m_ElectricityRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps2),
            m_UpdatedWaterPipeRoadEdges = this.m_WaterPipeRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps3),
            m_IconCommandBuffer = this.m_IconCommandSystem.CreateCommandBuffer(),
            m_SimulationFrame = this.m_SimulationSystem.frameIndex
        };
        this.Dependency = jobData4.Schedule<RWHBuildingUpkeepSystem.LeveldownJob>(JobHandle.CombineDependencies(this.Dependency, deps2, deps3));
        // ISSUE: reference to a compiler-generated field
        this.m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_ElectricityRoadConnectionGraphSystem.AddQueueWriter(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_IconCommandSystem.AddCommandBufferWriter(this.Dependency);
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated method
        this.m_TriggerSystem.AddActionBufferWriter(this.Dependency);
        JobHandle deps4;
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        RWHBuildingUpkeepSystem.UpkeepPaymentJob jobData5 = new RWHBuildingUpkeepSystem.UpkeepPaymentJob()
        {
            m_Resources = InternalCompilerInterface.GetBufferLookup<Resources>(ref this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref this.CheckedStateRef),
            m_Households = InternalCompilerInterface.GetComponentLookup<Household>(ref this.__TypeHandle.__Game_Citizens_Household_RW_ComponentLookup, ref this.CheckedStateRef),
            m_UpkeepExpenseQueue = this.m_UpkeepExpenseQueue,
            m_LevelUpMaterialQueue = this.m_LevelUpMaterialQueue,
            m_UpkeepMaterialAccumulator = this.m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.LevelUp, out deps4)
        };
        this.Dependency = jobData5.Schedule<RWHBuildingUpkeepSystem.UpkeepPaymentJob>(JobHandle.CombineDependencies(this.Dependency, deps4));
    }

    public void DebugLevelUp(
      Entity building,
      ComponentLookup<BuildingCondition> conditions,
      ComponentLookup<SpawnableBuildingData> spawnables,
      ComponentLookup<PrefabRef> prefabRefs,
      ComponentLookup<ZoneData> zoneDatas,
      ComponentLookup<BuildingPropertyData> propertyDatas)
    {
        if (!conditions.HasComponent(building) || !prefabRefs.HasComponent(building))
            return;
        Entity prefab = prefabRefs[building].m_Prefab;
        if (!spawnables.HasComponent(prefab) || !propertyDatas.HasComponent(prefab))
            return;
        SpawnableBuildingData spawnable = spawnables[prefab];
        if (!zoneDatas.HasComponent(spawnable.m_ZonePrefab))
            return;
        // ISSUE: reference to a compiler-generated field
        this.m_LevelupQueue.Enqueue(building);
    }

    public void DebugLevelDown(
      Entity building,
      ComponentLookup<BuildingCondition> conditions,
      ComponentLookup<SpawnableBuildingData> spawnables,
      ComponentLookup<PrefabRef> prefabRefs,
      ComponentLookup<ZoneData> zoneDatas,
      ComponentLookup<BuildingPropertyData> propertyDatas)
    {
        if (!conditions.HasComponent(building) || !prefabRefs.HasComponent(building))
            return;
        BuildingCondition condition = conditions[building];
        Entity prefab = prefabRefs[building].m_Prefab;
        if (!spawnables.HasComponent(prefab) || !propertyDatas.HasComponent(prefab))
            return;
        SpawnableBuildingData spawnable = spawnables[prefab];
        if (!zoneDatas.HasComponent(spawnable.m_ZonePrefab))
            return;
        int areaType = (int)zoneDatas[spawnable.m_ZonePrefab].m_AreaType;
        // ISSUE: reference to a compiler-generated field
        int levelingCost = BuildingUtils.GetLevelingCost((AreaType)areaType, propertyDatas[prefab], (int)spawnable.m_Level, this.EntityManager.GetBuffer<CityModifier>(this.m_CitySystem.City, true));
        // ISSUE: reference to a compiler-generated field
        int abandonCost = BuildingUtils.GetAbandonCost((AreaType)areaType, propertyDatas[prefab], (int)spawnable.m_Level, levelingCost, this.EntityManager.GetBuffer<CityModifier>(this.m_CitySystem.City, true));
        condition.m_Condition = -3 * abandonCost / 2;
        conditions[building] = condition;
        // ISSUE: reference to a compiler-generated field
        this.m_LeveldownQueue.Enqueue(building);
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
    public RWHBuildingUpkeepSystem()
    {
    }

    private struct UpkeepPayment
    {
        public Entity m_RenterEntity;
        public int m_Price;
    }

    private struct LevelUpMaterial
    {
        public Resource m_Resource;
        public int m_Amount;
    }

    [BurstCompile]
    private struct BuildingUpkeepJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;
        public ComponentTypeHandle<BuildingCondition> m_ConditionType;
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType;
        [ReadOnly]
        public BufferTypeHandle<Renter> m_RenterType;
        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;
        [ReadOnly]
        public BufferLookup<LevelUpResourceData> m_LevelUpResourceDataBufs;
        [ReadOnly]
        public BufferLookup<ZoneLevelUpResourceData> m_ZoneLevelUpResourceDataBufs;
        [ReadOnly]
        public ComponentLookup<ZoneData> m_ZoneDatas;
        [ReadOnly]
        public BufferLookup<Resources> m_Resources;
        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;
        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;
        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;
        [ReadOnly]
        public BufferLookup<CityModifier> m_CityModifierBufs;
        [ReadOnly]
        public ComponentLookup<Abandoned> m_Abandoned;
        [ReadOnly]
        public ComponentLookup<Destroyed> m_Destroyed;
        [ReadOnly]
        public ComponentLookup<SignatureBuildingData> m_SignatureDatas;
        [ReadOnly]
        public ComponentLookup<Household> m_Households;
        [ReadOnly]
        public BufferLookup<OwnedVehicle> m_OwnedVehicles;
        [ReadOnly]
        public BufferLookup<LayoutElement> m_LayoutElements;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;
        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefs;
        [ReadOnly]
        public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;
        [ReadOnly]
        public ComponentLookup<ServiceAvailable> m_ServiceAvailables;
        [ReadOnly]
        public BuildingConfigurationData m_BuildingConfigurationData;
        [ReadOnly]
        public DynamicBuffer<ZoneLevelUpResourceData> m_BuildingConfigLevelResourceBuf;
        [ReadOnly]
        public ComponentLookup<ConsumptionData> m_ConsumptionDatas;
        [ReadOnly]
        public BufferLookup<ResourceAvailability> m_Availabilities;
        [ReadOnly]
        public Entity m_City;
        [ReadOnly]
        public EntityArchetype m_GoodsDeliveryRequestArchetype;
        public float m_TemperatureUpkeep;
        public bool m_DebugFastLeveling;
        public NativeQueue<RWHBuildingUpkeepSystem.UpkeepPayment>.ParallelWriter m_UpkeepExpenseQueue;
        public NativeQueue<Entity>.ParallelWriter m_LevelDownQueue;
        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        public IconCommandBuffer m_IconCommandBuffer;

        private void RequestResourceDelivery(
          int jobIndex,
          Entity entity,
          DynamicBuffer<ResourceNeeding> resourceNeedings,
          Resource resource,
          int amount)
        {
            resourceNeedings.Add(new ResourceNeeding()
            {
                m_Resource = resource,
                m_Amount = amount,
                m_Flags = ResourceNeedingFlags.Requested
            });
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            Entity entity1 = this.m_CommandBuffer.CreateEntity(jobIndex, this.m_GoodsDeliveryRequestArchetype);
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<GoodsDeliveryRequest>(jobIndex, entity1, new GoodsDeliveryRequest()
            {
                m_ResourceNeeder = entity,
                m_Amount = amount,
                m_Resource = resource
            });
            // ISSUE: reference to a compiler-generated field
            this.m_CommandBuffer.SetComponent<RequestGroup>(jobIndex, entity1, new RequestGroup(32U /*0x20*/));
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.m_IconCommandBuffer.Add(entity, this.m_BuildingConfigurationData.m_LevelingBuildingNotificationPrefab);
        }

        public void Execute(
          in ArchetypeChunk chunk,
          int unfilteredChunkIndex,
          bool useEnabledMask,
          in v128 chunkEnabledMask)
        {
            // ISSUE: reference to a compiler-generated field
            NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<BuildingCondition> nativeArray2 = chunk.GetNativeArray<BuildingCondition>(ref this.m_ConditionType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabType);
            // ISSUE: reference to a compiler-generated field
            BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor<Renter>(ref this.m_RenterType);
            for (int index1 = 0; index1 < chunk.Count; ++index1)
            {
                Entity entity = nativeArray1[index1];
                BuildingCondition buildingCondition = nativeArray2[index1];
                DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[index1];
                Entity prefab = nativeArray3[index1].m_Prefab;
                // ISSUE: reference to a compiler-generated field
                ConsumptionData consumptionData = this.m_ConsumptionDatas[prefab];
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                DynamicBuffer<CityModifier> cityModifierBuf = this.m_CityModifierBufs[this.m_City];
                // ISSUE: reference to a compiler-generated field
                SpawnableBuildingData spawnableBuildingData = this.m_SpawnableBuildingDatas[prefab];
                // ISSUE: reference to a compiler-generated field
                AreaType areaType = this.m_ZoneDatas[spawnableBuildingData.m_ZonePrefab].m_AreaType;
                // ISSUE: reference to a compiler-generated field
                BuildingPropertyData buildingPropertyData = this.m_BuildingPropertyDatas[prefab];
                int levelingCost = BuildingUtils.GetLevelingCost(areaType, buildingPropertyData, (int)spawnableBuildingData.m_Level, cityModifierBuf);
                int abandonCost = BuildingUtils.GetAbandonCost(areaType, buildingPropertyData, (int)spawnableBuildingData.m_Level, levelingCost, cityModifierBuf);
                // ISSUE: reference to a compiler-generated field
                int num1 = consumptionData.m_Upkeep / RWHBuildingUpkeepSystem.kUpdatesPerDay;
                // ISSUE: reference to a compiler-generated field
                int num2 = num1 - num1 / RWHBuildingUpkeepSystem.kMaterialUpkeep;
                int num3 = 0;
                for (int index2 = 0; index2 < dynamicBuffer.Length; ++index2)
                {
                    DynamicBuffer<Resources> bufferData;
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_Resources.TryGetBuffer(dynamicBuffer[index2].m_Renter, out bufferData))
                    {
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_Households.HasComponent(dynamicBuffer[index2].m_Renter))
                        {
                            num3 += EconomyUtils.GetResources(Resource.Money, bufferData);
                        }
                        else
                        {
                            Entity renter = dynamicBuffer[index2].m_Renter;
                            // ISSUE: reference to a compiler-generated field
                            bool isIndustrial = !this.m_ServiceAvailables.HasComponent(renter);
                            IndustrialProcessData componentData1 = new IndustrialProcessData();
                            PrefabRef componentData2;
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_PrefabRefs.TryGetComponent(renter, out componentData2))
                            {
                                // ISSUE: reference to a compiler-generated field
                                this.m_IndustrialProcessDatas.TryGetComponent(componentData2.m_Prefab, out componentData1);
                            }
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_OwnedVehicles.HasBuffer(dynamicBuffer[index2].m_Renter))
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                num3 += EconomyUtils.GetCompanyTotalWorth(isIndustrial, componentData1, bufferData, this.m_OwnedVehicles[dynamicBuffer[index2].m_Renter], ref this.m_LayoutElements, ref this.m_DeliveryTrucks, this.m_ResourcePrefabs, ref this.m_ResourceDatas);
                            }
                            else
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                num3 += EconomyUtils.GetCompanyTotalWorth(isIndustrial, componentData1, bufferData, this.m_ResourcePrefabs, ref this.m_ResourceDatas);
                            }
                        }
                    }
                }
                int num4 = 0;
                if (num2 > num3)
                {
                    // ISSUE: reference to a compiler-generated field
                    num4 = -this.m_BuildingConfigurationData.m_BuildingConditionDecrement * (int)math.pow(2f, (float)spawnableBuildingData.m_Level) * math.max(1, dynamicBuffer.Length);
                }
                else if (dynamicBuffer.Length > 0)
                {
                    // ISSUE: reference to a compiler-generated field
                    num4 = BuildingUtils.GetBuildingConditionChange(areaType, this.m_BuildingConfigurationData) * (int)math.pow(2f, (float)spawnableBuildingData.m_Level) * math.max(1, dynamicBuffer.Length);
                    int num5 = num2 / dynamicBuffer.Length;
                    for (int index3 = 0; index3 < dynamicBuffer.Length; ++index3)
                    {
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: object of a compiler-generated type is created
                        this.m_UpkeepExpenseQueue.Enqueue(new RWHBuildingUpkeepSystem.UpkeepPayment()
                        {
                            m_RenterEntity = dynamicBuffer[index3].m_Renter,
                            m_Price = -num5
                        });
                    }
                }
                // ISSUE: reference to a compiler-generated field
                if (this.m_DebugFastLeveling)
                    buildingCondition.m_Condition = levelingCost;
                else
                    buildingCondition.m_Condition += num4;
                if (buildingCondition.m_Condition >= levelingCost)
                {
                    // ISSUE: reference to a compiler-generated field
                    DynamicBuffer<ResourceNeeding> resourceNeedings = this.m_CommandBuffer.AddBuffer<ResourceNeeding>(unfilteredChunkIndex, entity);
                    // ISSUE: reference to a compiler-generated field
                    this.m_CommandBuffer.AddBuffer<GuestVehicle>(unfilteredChunkIndex, entity);
                    DynamicBuffer<LevelUpResourceData> bufferData1;
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_LevelUpResourceDataBufs.TryGetBuffer(prefab, out bufferData1) && bufferData1.Length > 0)
                    {
                        for (int index4 = 0; index4 < bufferData1.Length; ++index4)
                        {
                            // ISSUE: reference to a compiler-generated method
                            this.RequestResourceDelivery(unfilteredChunkIndex, entity, resourceNeedings, bufferData1[index4].m_LevelUpResource.m_Resource, bufferData1[index4].m_LevelUpResource.m_Amount);
                        }
                    }
                    else
                    {
                        DynamicBuffer<ZoneLevelUpResourceData> bufferData2;
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_ZoneLevelUpResourceDataBufs.TryGetBuffer(spawnableBuildingData.m_ZonePrefab, out bufferData2) && bufferData2.Length > 0)
                        {
                            for (int index5 = 0; index5 < bufferData2.Length; ++index5)
                            {
                                if (bufferData2[index5].m_Level == (int)spawnableBuildingData.m_Level)
                                {
                                    // ISSUE: reference to a compiler-generated method
                                    this.RequestResourceDelivery(unfilteredChunkIndex, entity, resourceNeedings, bufferData2[index5].m_LevelUpResource.m_Resource, bufferData2[index5].m_LevelUpResource.m_Amount);
                                }
                            }
                        }
                        else
                        {
                            // ISSUE: reference to a compiler-generated field
                            for (int index6 = 0; index6 < this.m_BuildingConfigLevelResourceBuf.Length; ++index6)
                            {
                                // ISSUE: reference to a compiler-generated field
                                if (this.m_BuildingConfigLevelResourceBuf[index6].m_Level == (int)spawnableBuildingData.m_Level)
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    // ISSUE: reference to a compiler-generated field
                                    // ISSUE: reference to a compiler-generated method
                                    this.RequestResourceDelivery(unfilteredChunkIndex, entity, resourceNeedings, this.m_BuildingConfigLevelResourceBuf[index6].m_LevelUpResource.m_Resource, this.m_BuildingConfigLevelResourceBuf[index6].m_LevelUpResource.m_Amount);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    if ((this.m_Abandoned.HasComponent(nativeArray1[index1]) ? 0 : (!this.m_Destroyed.HasComponent(nativeArray1[index1]) ? 1 : 0)) != 0 && nativeArray2[index1].m_Condition <= -abandonCost && !this.m_SignatureDatas.HasComponent(prefab))
                    {
                        // ISSUE: reference to a compiler-generated field
                        this.m_LevelDownQueue.Enqueue(nativeArray1[index1]);
                        buildingCondition.m_Condition += levelingCost;
                    }
                }
                nativeArray2[index1] = buildingCondition;
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
    private struct ResourceNeedingUpkeepJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;
        public ComponentTypeHandle<BuildingCondition> m_ConditionType;
        public BufferTypeHandle<ResourceNeeding> m_ResourceNeedingType;
        [ReadOnly]
        public BufferLookup<GuestVehicle> m_GuestVehicleBufs;
        [ReadOnly]
        public BuildingConfigurationData m_BuildingConfigurationData;
        public NativeQueue<Entity>.ParallelWriter m_LevelupQueue;
        public NativeQueue<RWHBuildingUpkeepSystem.LevelUpMaterial>.ParallelWriter m_LeveUpMaterialQueue;
        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        public IconCommandBuffer m_IconCommandBuffer;

        public void Execute(
          in ArchetypeChunk chunk,
          int unfilteredChunkIndex,
          bool useEnabledMask,
          in v128 chunkEnabledMask)
        {
            // ISSUE: reference to a compiler-generated field
            NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
            // ISSUE: reference to a compiler-generated field
            BufferAccessor<ResourceNeeding> bufferAccessor = chunk.GetBufferAccessor<ResourceNeeding>(ref this.m_ResourceNeedingType);
            // ISSUE: reference to a compiler-generated field
            NativeArray<BuildingCondition> nativeArray2 = chunk.GetNativeArray<BuildingCondition>(ref this.m_ConditionType);
            for (int index1 = 0; index1 < chunk.Count; ++index1)
            {
                Entity entity = nativeArray1[index1];
                // ISSUE: reference to a compiler-generated field
                if (this.m_GuestVehicleBufs.HasBuffer(entity))
                {
                    DynamicBuffer<ResourceNeeding> dynamicBuffer = bufferAccessor[index1];
                    bool flag = true;
                    for (int index2 = 0; index2 < dynamicBuffer.Length; ++index2)
                    {
                        if (dynamicBuffer[index2].m_Flags != ResourceNeedingFlags.Delivered)
                            flag = false;
                    }
                    if (flag)
                    {
                        for (int index3 = 0; index3 < dynamicBuffer.Length; ++index3)
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: object of a compiler-generated type is created
                            this.m_LeveUpMaterialQueue.Enqueue(new RWHBuildingUpkeepSystem.LevelUpMaterial()
                            {
                                m_Resource = dynamicBuffer[index3].m_Resource,
                                m_Amount = dynamicBuffer[index3].m_Amount
                            });
                        }
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.RemoveComponent<ResourceNeeding>(unfilteredChunkIndex, entity);
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        this.m_IconCommandBuffer.Remove(entity, this.m_BuildingConfigurationData.m_LevelingBuildingNotificationPrefab);
                        // ISSUE: reference to a compiler-generated field
                        this.m_LevelupQueue.Enqueue(entity);
                        BuildingCondition buildingCondition = nativeArray2[index1] with
                        {
                            m_Condition = 0
                        };
                        nativeArray2[index1] = buildingCondition;
                    }
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
    private struct UpkeepPaymentJob : IJob
    {
        [ReadOnly]
        public uint m_FrameIndex;
        public BufferLookup<Resources> m_Resources;
        public ComponentLookup<Household> m_Households;
        public NativeQueue<RWHBuildingUpkeepSystem.UpkeepPayment> m_UpkeepExpenseQueue;
        public NativeQueue<RWHBuildingUpkeepSystem.LevelUpMaterial> m_LevelUpMaterialQueue;
        public NativeArray<int> m_UpkeepMaterialAccumulator;

        public void Execute()
        {
            // ISSUE: variable of a compiler-generated type
            RWHBuildingUpkeepSystem.UpkeepPayment upkeepPayment;
            // ISSUE: reference to a compiler-generated field
            while (this.m_UpkeepExpenseQueue.TryDequeue(out upkeepPayment))
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (this.m_Resources.HasBuffer(upkeepPayment.m_RenterEntity))
                {
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    EconomyUtils.AddResources(Resource.Money, upkeepPayment.m_Price, this.m_Resources[upkeepPayment.m_RenterEntity]);
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_Households.HasComponent(upkeepPayment.m_RenterEntity))
                    {
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        Household household = this.m_Households[upkeepPayment.m_RenterEntity];
                        // ISSUE: reference to a compiler-generated field
                        household.m_MoneySpendOnBuildingLevelingLastDay += upkeepPayment.m_Price;
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        this.m_Households[upkeepPayment.m_RenterEntity] = household;
                    }
                }
            }
            // ISSUE: variable of a compiler-generated type
            RWHBuildingUpkeepSystem.LevelUpMaterial levelUpMaterial;
            // ISSUE: reference to a compiler-generated field
            while (this.m_LevelUpMaterialQueue.TryDequeue(out levelUpMaterial))
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                this.m_UpkeepMaterialAccumulator[EconomyUtils.GetResourceIndex(levelUpMaterial.m_Resource)] += levelUpMaterial.m_Amount;
            }
        }
    }

    [BurstCompile]
    private struct LeveldownJob : IJob
    {
        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;
        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;
        [ReadOnly]
        public ComponentLookup<Game.Prefabs.BuildingData> m_BuildingDatas;
        public ComponentLookup<Building> m_Buildings;
        [ReadOnly]
        public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;
        [ReadOnly]
        public ComponentLookup<WaterConsumer> m_WaterConsumers;
        [ReadOnly]
        public ComponentLookup<GarbageProducer> m_GarbageProducers;
        [ReadOnly]
        public ComponentLookup<MailProducer> m_MailProducers;
        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;
        [ReadOnly]
        public ComponentLookup<OfficeBuilding> m_OfficeBuilding;
        public NativeQueue<TriggerAction> m_TriggerBuffer;
        public ComponentLookup<CrimeProducer> m_CrimeProducers;
        public BufferLookup<Renter> m_Renters;
        [ReadOnly]
        public BuildingConfigurationData m_BuildingConfigurationData;
        public NativeQueue<Entity> m_LeveldownQueue;
        public EntityCommandBuffer m_CommandBuffer;
        public NativeQueue<Entity> m_UpdatedElectricityRoadEdges;
        public NativeQueue<Entity> m_UpdatedWaterPipeRoadEdges;
        public IconCommandBuffer m_IconCommandBuffer;
        public uint m_SimulationFrame;

        public void Execute()
        {
            Entity entity;
            // ISSUE: reference to a compiler-generated field
            while (this.m_LeveldownQueue.TryDequeue(out entity))
            {
                // ISSUE: reference to a compiler-generated field
                if (this.m_Prefabs.HasComponent(entity))
                {
                    // ISSUE: reference to a compiler-generated field
                    Entity prefab = this.m_Prefabs[entity].m_Prefab;
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_SpawnableBuildings.HasComponent(prefab))
                    {
                        // ISSUE: reference to a compiler-generated field
                        SpawnableBuildingData spawnableBuilding = this.m_SpawnableBuildings[prefab];
                        // ISSUE: reference to a compiler-generated field
                        Game.Prefabs.BuildingData buildingData = this.m_BuildingDatas[prefab];
                        // ISSUE: reference to a compiler-generated field
                        BuildingPropertyData buildingPropertyData = this.m_BuildingPropertyDatas[prefab];
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.AddComponent<Abandoned>(entity, new Abandoned()
                        {
                            m_AbandonmentTime = this.m_SimulationFrame
                        });
                        // ISSUE: reference to a compiler-generated field
                        this.m_CommandBuffer.AddComponent<Updated>(entity, new Updated());
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_ElectricityConsumers.HasComponent(entity))
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.RemoveComponent<ElectricityConsumer>(entity);
                            // ISSUE: reference to a compiler-generated field
                            Entity roadEdge = this.m_Buildings[entity].m_RoadEdge;
                            if (roadEdge != Entity.Null)
                            {
                                // ISSUE: reference to a compiler-generated field
                                this.m_UpdatedElectricityRoadEdges.Enqueue(roadEdge);
                            }
                        }
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_WaterConsumers.HasComponent(entity))
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.RemoveComponent<WaterConsumer>(entity);
                            // ISSUE: reference to a compiler-generated field
                            Entity roadEdge = this.m_Buildings[entity].m_RoadEdge;
                            if (roadEdge != Entity.Null)
                            {
                                // ISSUE: reference to a compiler-generated field
                                this.m_UpdatedWaterPipeRoadEdges.Enqueue(roadEdge);
                            }
                        }
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_GarbageProducers.HasComponent(entity))
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.RemoveComponent<GarbageProducer>(entity);
                        }
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_MailProducers.HasComponent(entity))
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.RemoveComponent<MailProducer>(entity);
                        }
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_CrimeProducers.HasComponent(entity))
                        {
                            // ISSUE: reference to a compiler-generated field
                            CrimeProducer crimeProducer = this.m_CrimeProducers[entity];
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.SetComponent<CrimeProducer>(entity, new CrimeProducer()
                            {
                                m_Crime = crimeProducer.m_Crime * 2f,
                                m_PatrolRequest = crimeProducer.m_PatrolRequest
                            });
                        }
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_Renters.HasBuffer(entity))
                        {
                            // ISSUE: reference to a compiler-generated field
                            DynamicBuffer<Renter> renter = this.m_Renters[entity];
                            for (int index = renter.Length - 1; index >= 0; --index)
                            {
                                // ISSUE: reference to a compiler-generated field
                                this.m_CommandBuffer.RemoveComponent<PropertyRenter>(renter[index].m_Renter);
                                renter.RemoveAt(index);
                            }
                        }
                        // ISSUE: reference to a compiler-generated field
                        if ((this.m_Buildings[entity].m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) != Game.Buildings.BuildingFlags.None)
                        {
                            // ISSUE: reference to a compiler-generated field
                            Building building = this.m_Buildings[entity];
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_IconCommandBuffer.Remove(entity, this.m_BuildingConfigurationData.m_HighRentNotification);
                            building.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
                            // ISSUE: reference to a compiler-generated field
                            this.m_Buildings[entity] = building;
                        }
                        // ISSUE: reference to a compiler-generated field
                        this.m_IconCommandBuffer.Remove(entity, IconPriority.Problem);
                        // ISSUE: reference to a compiler-generated field
                        this.m_IconCommandBuffer.Remove(entity, IconPriority.FatalProblem);
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        this.m_IconCommandBuffer.Add(entity, this.m_BuildingConfigurationData.m_AbandonedNotification, IconPriority.FatalProblem);
                        if (buildingPropertyData.CountProperties(AreaType.Commercial) > 0)
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelDownCommercialBuilding, Entity.Null, entity, entity));
                        }
                        if (buildingPropertyData.CountProperties(AreaType.Industrial) > 0)
                        {
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_OfficeBuilding.HasComponent(prefab))
                            {
                                // ISSUE: reference to a compiler-generated field
                                this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelDownOfficeBuilding, Entity.Null, entity, entity));
                            }
                            else
                            {
                                // ISSUE: reference to a compiler-generated field
                                this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelDownIndustrialBuilding, Entity.Null, entity, entity));
                            }
                        }
                    }
                }
            }
        }
    }

    [BurstCompile]
    private struct LevelupJob : IJob
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;
        [ReadOnly]
        public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;
        [ReadOnly]
        public ComponentTypeHandle<Game.Prefabs.BuildingData> m_BuildingType;
        [ReadOnly]
        public ComponentTypeHandle<BuildingPropertyData> m_BuildingPropertyType;
        [ReadOnly]
        public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryType;
        [ReadOnly]
        public SharedComponentTypeHandle<BuildingSpawnGroupData> m_BuildingSpawnGroupType;
        [ReadOnly]
        public ComponentLookup<Transform> m_TransformData;
        [ReadOnly]
        public ComponentLookup<Game.Zones.Block> m_BlockData;
        [ReadOnly]
        public ComponentLookup<ValidArea> m_ValidAreaData;
        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;
        [ReadOnly]
        public ComponentLookup<PrefabData> m_PrefabDatas;
        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;
        [ReadOnly]
        public ComponentLookup<Game.Prefabs.BuildingData> m_Buildings;
        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;
        [ReadOnly]
        public ComponentLookup<OfficeBuilding> m_OfficeBuilding;
        [ReadOnly]
        public ComponentLookup<ZoneData> m_ZoneData;
        [ReadOnly]
        public BufferLookup<Cell> m_Cells;
        [ReadOnly]
        public BuildingConfigurationData m_BuildingConfigurationData;
        [ReadOnly]
        public NativeList<ArchetypeChunk> m_SpawnableBuildingChunks;
        [ReadOnly]
        public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;
        [ReadOnly]
        public RandomSeed m_RandomSeed;
        public IconCommandBuffer m_IconCommandBuffer;
        public NativeQueue<Entity> m_LevelupQueue;
        public EntityCommandBuffer m_CommandBuffer;
        public NativeQueue<TriggerAction> m_TriggerBuffer;
        public NativeQueue<ZoneBuiltLevelUpdate> m_ZoneBuiltLevelQueue;

        public void Execute()
        {
            // ISSUE: reference to a compiler-generated field
            Random random = this.m_RandomSeed.GetRandom(0);
            Entity entity1;
            // ISSUE: reference to a compiler-generated field
            while (this.m_LevelupQueue.TryDequeue(out entity1))
            {
                // ISSUE: reference to a compiler-generated field
                Entity prefab = this.m_Prefabs[entity1].m_Prefab;
                // ISSUE: reference to a compiler-generated field
                if (this.m_SpawnableBuildings.HasComponent(prefab))
                {
                    // ISSUE: reference to a compiler-generated field
                    SpawnableBuildingData spawnableBuilding = this.m_SpawnableBuildings[prefab];
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_PrefabDatas.IsComponentEnabled(spawnableBuilding.m_ZonePrefab))
                    {
                        // ISSUE: reference to a compiler-generated field
                        Game.Prefabs.BuildingData building = this.m_Buildings[prefab];
                        // ISSUE: reference to a compiler-generated field
                        BuildingPropertyData buildingPropertyData = this.m_BuildingPropertyDatas[prefab];
                        // ISSUE: reference to a compiler-generated field
                        ZoneData zoneData = this.m_ZoneData[spawnableBuilding.m_ZonePrefab];
                        // ISSUE: reference to a compiler-generated method
                        float maxHeight = this.GetMaxHeight(entity1, building);
                        // ISSUE: reference to a compiler-generated method
                        Entity entity2 = this.SelectSpawnableBuilding(zoneData.m_ZoneType, (int)spawnableBuilding.m_Level + 1, building.m_LotSize, maxHeight, building.m_Flags & (Game.Prefabs.BuildingFlags.LeftAccess | Game.Prefabs.BuildingFlags.RightAccess), buildingPropertyData, ref random);
                        if (entity2 != Entity.Null)
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.AddComponent<UnderConstruction>(entity1, new UnderConstruction()
                            {
                                m_NewPrefab = entity2,
                                m_Progress = byte.MaxValue
                            });
                            if (buildingPropertyData.CountProperties(AreaType.Residential) > 0)
                            {
                                // ISSUE: reference to a compiler-generated field
                                this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpResidentialBuilding, Entity.Null, entity1, entity1));
                            }
                            if (buildingPropertyData.CountProperties(AreaType.Commercial) > 0)
                            {
                                // ISSUE: reference to a compiler-generated field
                                this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpCommercialBuilding, Entity.Null, entity1, entity1));
                            }
                            if (buildingPropertyData.CountProperties(AreaType.Industrial) > 0)
                            {
                                // ISSUE: reference to a compiler-generated field
                                if (this.m_OfficeBuilding.HasComponent(prefab))
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpOfficeBuilding, Entity.Null, entity1, entity1));
                                }
                                else
                                {
                                    // ISSUE: reference to a compiler-generated field
                                    this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpIndustrialBuilding, Entity.Null, entity1, entity1));
                                }
                            }
                            // ISSUE: reference to a compiler-generated field
                            this.m_ZoneBuiltLevelQueue.Enqueue(new ZoneBuiltLevelUpdate()
                            {
                                m_Zone = spawnableBuilding.m_ZonePrefab,
                                m_FromLevel = (int)spawnableBuilding.m_Level,
                                m_ToLevel = (int)spawnableBuilding.m_Level + 1,
                                m_Squares = building.m_LotSize.x * building.m_LotSize.y
                            });
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_IconCommandBuffer.Add(entity1, this.m_BuildingConfigurationData.m_LevelUpNotification, clusterLayer: IconClusterLayer.Transaction);
                        }
                    }
                }
            }
        }

        private Entity SelectSpawnableBuilding(
          ZoneType zoneType,
          int level,
          int2 lotSize,
          float maxHeight,
          Game.Prefabs.BuildingFlags accessFlags,
          BuildingPropertyData buildingPropertyData,
          ref Random random)
        {
            int max = 0;
            Entity entity = Entity.Null;
            // ISSUE: reference to a compiler-generated field
            for (int index1 = 0; index1 < this.m_SpawnableBuildingChunks.Length; ++index1)
            {
                // ISSUE: reference to a compiler-generated field
                ArchetypeChunk spawnableBuildingChunk = this.m_SpawnableBuildingChunks[index1];
                // ISSUE: reference to a compiler-generated field
                if (spawnableBuildingChunk.GetSharedComponent<BuildingSpawnGroupData>(this.m_BuildingSpawnGroupType).m_ZoneType.Equals(zoneType))
                {
                    // ISSUE: reference to a compiler-generated field
                    NativeArray<Entity> nativeArray1 = spawnableBuildingChunk.GetNativeArray(this.m_EntityType);
                    // ISSUE: reference to a compiler-generated field
                    NativeArray<SpawnableBuildingData> nativeArray2 = spawnableBuildingChunk.GetNativeArray<SpawnableBuildingData>(ref this.m_SpawnableBuildingType);
                    // ISSUE: reference to a compiler-generated field
                    NativeArray<Game.Prefabs.BuildingData> nativeArray3 = spawnableBuildingChunk.GetNativeArray<Game.Prefabs.BuildingData>(ref this.m_BuildingType);
                    // ISSUE: reference to a compiler-generated field
                    NativeArray<BuildingPropertyData> nativeArray4 = spawnableBuildingChunk.GetNativeArray<BuildingPropertyData>(ref this.m_BuildingPropertyType);
                    // ISSUE: reference to a compiler-generated field
                    NativeArray<ObjectGeometryData> nativeArray5 = spawnableBuildingChunk.GetNativeArray<ObjectGeometryData>(ref this.m_ObjectGeometryType);
                    for (int index2 = 0; index2 < spawnableBuildingChunk.Count; ++index2)
                    {
                        SpawnableBuildingData spawnableBuildingData = nativeArray2[index2];
                        Game.Prefabs.BuildingData buildingData = nativeArray3[index2];
                        BuildingPropertyData buildingPropertyData1 = nativeArray4[index2];
                        ObjectGeometryData objectGeometryData = nativeArray5[index2];
                        if (level == (int)spawnableBuildingData.m_Level && lotSize.Equals(buildingData.m_LotSize) && (double)objectGeometryData.m_Size.y <= (double)maxHeight && (buildingData.m_Flags & (Game.Prefabs.BuildingFlags.LeftAccess | Game.Prefabs.BuildingFlags.RightAccess)) == accessFlags && buildingPropertyData.m_AllowedManufactured == buildingPropertyData1.m_AllowedManufactured && buildingPropertyData.m_AllowedInput == buildingPropertyData1.m_AllowedInput && buildingPropertyData.m_AllowedSold == buildingPropertyData1.m_AllowedSold && buildingPropertyData.m_AllowedStored == buildingPropertyData1.m_AllowedStored)
                        {
                            int num = 100;
                            max += num;
                            if (random.NextInt(max) < num)
                                entity = nativeArray1[index2];
                        }
                    }
                }
            }
            return entity;
        }

        private float GetMaxHeight(Entity building, Game.Prefabs.BuildingData prefabBuildingData)
        {
            // ISSUE: reference to a compiler-generated field
            Transform transform = this.m_TransformData[building];
            float2 xz1 = math.rotate(transform.m_Rotation, new float3(8f, 0.0f, 0.0f)).xz;
            float2 xz2 = math.rotate(transform.m_Rotation, new float3(0.0f, 0.0f, 8f)).xz;
            float2 x1 = xz1 * (float)((double)prefabBuildingData.m_LotSize.x * 0.5 - 0.5);
            float2 x2 = xz2 * (float)((double)prefabBuildingData.m_LotSize.y * 0.5 - 0.5);
            float2 float2 = math.abs(x2) + math.abs(x1);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: object of a compiler-generated type is created
            // ISSUE: variable of a compiler-generated type
            RWHBuildingUpkeepSystem.LevelupJob.Iterator iterator = new RWHBuildingUpkeepSystem.LevelupJob.Iterator()
            {
                m_Bounds = new Bounds2(transform.m_Position.xz - float2, transform.m_Position.xz + float2),
                m_LotSize = prefabBuildingData.m_LotSize,
                m_StartPosition = transform.m_Position.xz + x2 + x1,
                m_Right = xz1,
                m_Forward = xz2,
                m_MaxHeight = int.MaxValue,
                m_BlockData = this.m_BlockData,
                m_ValidAreaData = this.m_ValidAreaData,
                m_Cells = this.m_Cells
            };
            // ISSUE: reference to a compiler-generated field
            this.m_ZoneSearchTree.Iterate<RWHBuildingUpkeepSystem.LevelupJob.Iterator>(ref iterator);
            // ISSUE: reference to a compiler-generated field
            return (float)iterator.m_MaxHeight - transform.m_Position.y;
        }

        private struct Iterator :
          INativeQuadTreeIterator<Entity, Bounds2>,
          IUnsafeQuadTreeIterator<Entity, Bounds2>
        {
            public Bounds2 m_Bounds;
            public int2 m_LotSize;
            public float2 m_StartPosition;
            public float2 m_Right;
            public float2 m_Forward;
            public int m_MaxHeight;
            public ComponentLookup<Game.Zones.Block> m_BlockData;
            public ComponentLookup<ValidArea> m_ValidAreaData;
            public BufferLookup<Cell> m_Cells;

            public bool Intersect(Bounds2 bounds) => MathUtils.Intersect(bounds, this.m_Bounds);

            public void Iterate(Bounds2 bounds, Entity blockEntity)
            {
                // ISSUE: reference to a compiler-generated field
                if (!MathUtils.Intersect(bounds, this.m_Bounds))
                    return;
                // ISSUE: reference to a compiler-generated field
                ValidArea validArea = this.m_ValidAreaData[blockEntity];
                if (validArea.m_Area.y <= validArea.m_Area.x)
                    return;
                // ISSUE: reference to a compiler-generated field
                Game.Zones.Block block = this.m_BlockData[blockEntity];
                // ISSUE: reference to a compiler-generated field
                DynamicBuffer<Cell> cell1 = this.m_Cells[blockEntity];
                // ISSUE: reference to a compiler-generated field
                float2 startPosition = this.m_StartPosition;
                int2 int2;
                // ISSUE: reference to a compiler-generated field
                for (int2.y = 0; int2.y < this.m_LotSize.y; ++int2.y)
                {
                    float2 position = startPosition;
                    // ISSUE: reference to a compiler-generated field
                    for (int2.x = 0; int2.x < this.m_LotSize.x; ++int2.x)
                    {
                        int2 cellIndex = ZoneUtils.GetCellIndex(block, position);
                        if (math.all(cellIndex >= validArea.m_Area.xz & cellIndex < validArea.m_Area.yw))
                        {
                            int index = cellIndex.y * block.m_Size.x + cellIndex.x;
                            Cell cell2 = cell1[index];
                            if ((cell2.m_State & CellFlags.Visible) != CellFlags.None)
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_MaxHeight = math.min(this.m_MaxHeight, (int)cell2.m_Height);
                            }
                        }
                        // ISSUE: reference to a compiler-generated field
                        position -= this.m_Right;
                    }
                    // ISSUE: reference to a compiler-generated field
                    startPosition -= this.m_Forward;
                }
            }
        }
    }

    private struct TypeHandle
    {
        public ComponentTypeHandle<BuildingCondition> __Game_Buildings_BuildingCondition_RW_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
        [ReadOnly]
        public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;
        [ReadOnly]
        public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;
        [ReadOnly]
        public BufferLookup<LevelUpResourceData> __Game_Prefabs_LevelUpResourceData_RO_BufferLookup;
        [ReadOnly]
        public BufferLookup<ZoneLevelUpResourceData> __Game_Prefabs_ZoneLevelUpResourceData_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;
        [ReadOnly]
        public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;
        [ReadOnly]
        public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;
        public BufferTypeHandle<ResourceNeeding> __Game_Buildings_ResourceNeeding_RW_BufferTypeHandle;
        [ReadOnly]
        public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RO_BufferLookup;
        [ReadOnly]
        public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<Game.Prefabs.BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;
        public SharedComponentTypeHandle<BuildingSpawnGroupData> __Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle;
        [ReadOnly]
        public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Zones.Block> __Game_Zones_Block_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ValidArea> __Game_Zones_ValidArea_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<Game.Prefabs.BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<OfficeBuilding> __Game_Prefabs_OfficeBuilding_RO_ComponentLookup;
        [ReadOnly]
        public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;
        public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;
        [ReadOnly]
        public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;
        public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RW_ComponentLookup;
        public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;
        public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;
        public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingCondition>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_LevelUpResourceData_RO_BufferLookup = state.GetBufferLookup<LevelUpResourceData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ZoneLevelUpResourceData_RO_BufferLookup = state.GetBufferLookup<ZoneLevelUpResourceData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<SignatureBuildingData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_ResourceNeeding_RW_BufferTypeHandle = state.GetBufferTypeHandle<ResourceNeeding>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Vehicles_GuestVehicle_RO_BufferLookup = state.GetBufferLookup<GuestVehicle>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Prefabs.BuildingData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<BuildingSpawnGroupData>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Game.Zones.Block>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Zones_ValidArea_RO_ComponentLookup = state.GetComponentLookup<ValidArea>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.BuildingData>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup = state.GetComponentLookup<OfficeBuilding>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_Building_RW_ComponentLookup = state.GetComponentLookup<Building>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(true);
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_CrimeProducer_RW_ComponentLookup = state.GetComponentLookup<CrimeProducer>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Buildings_Renter_RW_BufferLookup = state.GetBufferLookup<Renter>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Resources>();
            // ISSUE: reference to a compiler-generated field
            this.__Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();
        }
    }
}
