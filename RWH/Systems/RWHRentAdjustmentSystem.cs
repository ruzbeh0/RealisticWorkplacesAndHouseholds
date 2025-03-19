// Decompiled with JetBrains decompiler
// Type: Game.Simulation.RWHRentAdjustSystem
// Assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 407A0A0B-0B48-4732-BD57-8ACD1ADF3D7C
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed\Game.dll

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
using Game.Economy;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#nullable disable
namespace RealisticWorkplacesAndHouseholds.Systems
{
    //[CompilerGenerated]
    public partial class RWHRentAdjustSystem : GameSystemBase
    {
        public static readonly int kUpdatesPerDay = 16;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_DemandParameterQuery;
        private SimulationSystem m_SimulationSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private ResourceSystem m_ResourceSystem;
        private GroundPollutionSystem m_GroundPollutionSystem;
        private AirPollutionSystem m_AirPollutionSystem;
        private NoisePollutionSystem m_NoisePollutionSystem;
        private TelecomCoverageSystem m_TelecomCoverageSystem;
        private CitySystem m_CitySystem;
        private TaxSystem m_TaxSystem;
        private IconCommandSystem m_IconCommandSystem;
        private EntityQuery m_HealthcareParameterQuery;
        private EntityQuery m_ExtractorParameterQuery;
        private EntityQuery m_ParkParameterQuery;
        private EntityQuery m_EducationParameterQuery;
        private EntityQuery m_TelecomParameterQuery;
        private EntityQuery m_GarbageParameterQuery;
        private EntityQuery m_PoliceParameterQuery;
        private EntityQuery m_CitizenHappinessParameterQuery;
        private EntityQuery m_BuildingParameterQuery;
        private EntityQuery m_PollutionParameterQuery;
        private EntityQuery m_BuildingQuery;
        protected int cycles;
        private RWHRentAdjustSystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // ISSUE: reference to a compiler-generated field
            return 262144 / (RWHRentAdjustSystem.kUpdatesPerDay * 16);
        }

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
            this.m_TaxSystem = this.World.GetOrCreateSystemManaged<TaxSystem>();
            // ISSUE: reference to a compiler-generated field
            this.m_IconCommandSystem = this.World.GetOrCreateSystemManaged<IconCommandSystem>();
            // ISSUE: reference to a compiler-generated field
            this.m_EconomyParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            // ISSUE: reference to a compiler-generated field
            this.m_DemandParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            // ISSUE: reference to a compiler-generated field
            this.m_BuildingParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
            // ISSUE: reference to a compiler-generated field
            this.m_BuildingQuery = this.GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Renter>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
            // ISSUE: reference to a compiler-generated field
            this.m_ExtractorParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<ExtractorParameterData>());
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
            this.m_PollutionParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_EconomyParameterQuery);
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_DemandParameterQuery);
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_HealthcareParameterQuery);
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_ParkParameterQuery);
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_EducationParameterQuery);
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_TelecomParameterQuery);
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_GarbageParameterQuery);
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_PoliceParameterQuery);
            // ISSUE: reference to a compiler-generated field
            this.RequireForUpdate(this.m_BuildingQuery);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            uint updateFrame = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, RWHRentAdjustSystem.kUpdatesPerDay, 16);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Economy_Resources_RO_BufferLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Buildings_ExtractorProperty_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Buildings_BuildingNotifications_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Companies_CompanyNotifications_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Buildings_Building_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle.Update(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            JobHandle dependencies1;
            JobHandle dependencies2;
            JobHandle dependencies3;
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
            // ISSUE: reference to a compiler-generated method
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
            // ISSUE: reference to a compiler-generated field
            JobHandle jobHandle = new RWHRentAdjustSystem.AdjustRentJob()
            {
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_RenterType = this.__TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle,
                m_UpdateFrameType = this.GetSharedComponentTypeHandle<UpdateFrame>(),
                m_Renters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RW_ComponentLookup,
                m_OnMarkets = this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RW_ComponentLookup,
                m_Buildings = this.__TypeHandle.__Game_Buildings_Building_RW_ComponentLookup,
                m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_BuildingProperties = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup,
                m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup,
                m_WorkProviders = this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup,
                m_CompanyNotifications = this.__TypeHandle.__Game_Companies_CompanyNotifications_RO_ComponentLookup,
                m_Attached = this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup,
                m_Lots = this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup,
                m_Geometries = this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup,
                m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup,
                m_ConsumptionDatas = this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup,
                m_CitizenBufs = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup,
                m_SubAreas = this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup,
                m_StorageCompanies = this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup,
                m_Abandoned = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup,
                m_Destroyed = this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup,
                m_Transforms = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup,
                m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup,
                m_HealthProblems = this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup,
                m_SpawnableBuildingData = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup,
                m_ZoneData = this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup,
                m_BuildingNotifications = this.__TypeHandle.__Game_Buildings_BuildingNotifications_RW_ComponentLookup,
                m_ExtractorProperties = this.__TypeHandle.__Game_Buildings_ExtractorProperty_RO_ComponentLookup,
                m_ResourcesBuf = this.__TypeHandle.__Game_Economy_Resources_RO_BufferLookup,
                m_Workers = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup,
                m_CitizenDatas = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup,
                m_ProcessDatas = this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup,
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_TaxRates = this.m_TaxSystem.GetTaxRates(),
                m_PollutionMap = this.m_GroundPollutionSystem.GetMap(true, out dependencies1),
                m_AirPollutionMap = this.m_AirPollutionSystem.GetMap(true, out dependencies2),
                m_NoiseMap = this.m_NoisePollutionSystem.GetMap(true, out dependencies3),
                m_CitizenHappinessParameterData = this.m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
                m_BuildingConfigurationData = this.m_BuildingParameterQuery.GetSingleton<BuildingConfigurationData>(),
                m_PollutionParameters = this.m_PollutionParameterQuery.GetSingleton<PollutionParameterData>(),
                m_DeliveryTrucks = this.__TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup,
                m_EconomyParameterData = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_City = this.m_CitySystem.City,
                m_UpdateFrameIndex = updateFrame,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_IconCommandBuffer = this.m_IconCommandSystem.CreateCommandBuffer()
            }.ScheduleParallel<RWHRentAdjustSystem.AdjustRentJob>(this.m_BuildingQuery, JobUtils.CombineDependencies(dependencies1, dependencies2, dependencies3, this.Dependency));
            // ISSUE: reference to a compiler-generated field
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            this.m_ResourceSystem.AddPrefabsReader(jobHandle);
            // ISSUE: reference to a compiler-generated field
            this.m_GroundPollutionSystem.AddReader(jobHandle);
            // ISSUE: reference to a compiler-generated field
            this.m_AirPollutionSystem.AddReader(jobHandle);
            // ISSUE: reference to a compiler-generated field
            this.m_NoisePollutionSystem.AddReader(jobHandle);
            // ISSUE: reference to a compiler-generated field
            this.m_TelecomCoverageSystem.AddReader(jobHandle);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            this.m_TaxSystem.AddReader(jobHandle);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            this.m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
            this.Dependency = jobHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
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
        public RWHRentAdjustSystem()
        {
        }

        //[BurstCompile]
        private struct AdjustRentJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public BufferTypeHandle<Renter> m_RenterType;
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<PropertyRenter> m_Renters;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<Building> m_Buildings;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingProperties;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> m_BuildingDatas;
            [ReadOnly]
            public ComponentLookup<WorkProvider> m_WorkProviders;
            [ReadOnly]
            public ComponentLookup<CompanyNotifications> m_CompanyNotifications;
            [ReadOnly]
            public ComponentLookup<Attached> m_Attached;
            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> m_Lots;
            [ReadOnly]
            public ComponentLookup<Geometry> m_Geometries;
            [ReadOnly]
            public ComponentLookup<LandValue> m_LandValues;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<PropertyOnMarket> m_OnMarkets;
            [ReadOnly]
            public ComponentLookup<ConsumptionData> m_ConsumptionDatas;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> m_CitizenBufs;
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> m_SubAreas;
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoned;
            [ReadOnly]
            public ComponentLookup<Destroyed> m_Destroyed;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_Transforms;
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;
            [ReadOnly]
            public ComponentLookup<HealthProblem> m_HealthProblems;
            [ReadOnly]
            public ComponentLookup<Worker> m_Workers;
            [ReadOnly]
            public ComponentLookup<Citizen> m_CitizenDatas;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;
            [ReadOnly]
            public BufferLookup<Game.Economy.Resources> m_ResourcesBuf;
            [ReadOnly]
            public ComponentLookup<ExtractorProperty> m_ExtractorProperties;
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> m_ProcessDatas;
            [ReadOnly]
            public BufferLookup<OwnedVehicle> m_OwnedVehicles;
            [ReadOnly]
            public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;
            [ReadOnly]
            public BufferLookup<LayoutElement> m_LayoutElements;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<BuildingNotifications> m_BuildingNotifications;
            [ReadOnly]
            public NativeArray<int> m_TaxRates;
            [ReadOnly]
            public NativeArray<AirPollution> m_AirPollutionMap;
            [ReadOnly]
            public NativeArray<GroundPollution> m_PollutionMap;
            [ReadOnly]
            public NativeArray<NoisePollution> m_NoiseMap;
            public CitizenHappinessParameterData m_CitizenHappinessParameterData;
            public BuildingConfigurationData m_BuildingConfigurationData;
            public PollutionParameterData m_PollutionParameters;
            public IconCommandBuffer m_IconCommandBuffer;
            public uint m_UpdateFrameIndex;
            [ReadOnly]
            public Entity m_City;
            public EconomyParameterData m_EconomyParameterData;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            private bool CanDisplayHighRentWarnIcon(DynamicBuffer<Renter> renters)
            {
                bool flag = true;
                for (int index1 = 0; index1 < renters.Length; ++index1)
                {
                    Entity renter = renters[index1].m_Renter;
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_CompanyNotifications.HasComponent(renter))
                    {
                        // ISSUE: reference to a compiler-generated field
                        CompanyNotifications companyNotification = this.m_CompanyNotifications[renter];
                        if (companyNotification.m_NoCustomersEntity != Entity.Null || companyNotification.m_NoInputEntity != Entity.Null)
                        {
                            flag = false;
                            break;
                        }
                    }
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_WorkProviders.HasComponent(renter))
                    {
                        // ISSUE: reference to a compiler-generated field
                        WorkProvider workProvider = this.m_WorkProviders[renter];
                        if (workProvider.m_EducatedNotificationEntity != Entity.Null || workProvider.m_UneducatedNotificationEntity != Entity.Null)
                        {
                            flag = false;
                            break;
                        }
                    }
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_CitizenBufs.HasBuffer(renter))
                    {
                        // ISSUE: reference to a compiler-generated field
                        DynamicBuffer<HouseholdCitizen> citizenBuf = this.m_CitizenBufs[renter];
                        flag = false;
                        for (int index2 = 0; index2 < citizenBuf.Length; ++index2)
                        {
                            // ISSUE: reference to a compiler-generated field
                            if (!CitizenUtils.IsDead(citizenBuf[index2].m_Citizen, ref this.m_HealthProblems))
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                }
                return flag;
            }

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                    return;
                // ISSUE: reference to a compiler-generated field
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                // ISSUE: reference to a compiler-generated field
                BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor<Renter>(ref this.m_RenterType);
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                DynamicBuffer<CityModifier> cityModifier = this.m_CityModifiers[this.m_City];
                for (int index1 = 0; index1 < nativeArray.Length; ++index1)
                {
                    Entity entity = nativeArray[index1];
                    // ISSUE: reference to a compiler-generated field
                    Entity prefab = this.m_Prefabs[entity].m_Prefab;
                    // ISSUE: reference to a compiler-generated field
                    if (this.m_BuildingProperties.HasComponent(prefab))
                    {
                        // ISSUE: reference to a compiler-generated field
                        BuildingPropertyData buildingProperty = this.m_BuildingProperties[prefab];
                        int num1 = buildingProperty.m_ResidentialProperties <= 0 ? 0 : (buildingProperty.m_AllowedSold != Resource.NoResource ? 1 : (buildingProperty.m_AllowedManufactured > Resource.NoResource ? 1 : 0));
                        // ISSUE: reference to a compiler-generated field
                        Building building = this.m_Buildings[entity];
                        DynamicBuffer<Renter> renters = bufferAccessor[index1];
                        // ISSUE: reference to a compiler-generated field
                        Game.Prefabs.BuildingData buildingData = this.m_BuildingDatas[prefab];
                        int lotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_Attached.HasComponent(entity))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated method
                            float area = ExtractorAISystem.GetArea(this.m_SubAreas[this.m_Attached[entity].m_Parent], this.m_Lots, this.m_Geometries);
                            lotSize += Mathf.CeilToInt(area);
                        }
                        float landValueBase = 0.0f;
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_LandValues.HasComponent(building.m_RoadEdge))
                        {
                            // ISSUE: reference to a compiler-generated field
                            landValueBase = this.m_LandValues[building.m_RoadEdge].m_LandValue;
                        }
                        Game.Zones.AreaType areaType = Game.Zones.AreaType.None;
                        // ISSUE: reference to a compiler-generated field
                        int buildingLevel = PropertyUtils.GetBuildingLevel(prefab, this.m_SpawnableBuildingData);
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_SpawnableBuildingData.HasComponent(prefab))
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            areaType = this.m_ZoneData[this.m_SpawnableBuildingData[prefab].m_ZonePrefab].m_AreaType;
                        }
                        if (areaType == Game.Zones.AreaType.Residential)
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated method
                            int2 pollutionBonuses1 = CitizenHappinessSystem.GetGroundPollutionBonuses(entity, ref this.m_Transforms, this.m_PollutionMap, cityModifier, in this.m_CitizenHappinessParameterData);
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated method
                            int2 noiseBonuses = CitizenHappinessSystem.GetNoiseBonuses(entity, ref this.m_Transforms, this.m_NoiseMap, in this.m_CitizenHappinessParameterData);
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated method
                            int2 pollutionBonuses2 = CitizenHappinessSystem.GetAirPollutionBonuses(entity, ref this.m_Transforms, this.m_AirPollutionMap, cityModifier, in this.m_CitizenHappinessParameterData);
                            // ISSUE: reference to a compiler-generated field
                            bool flag1 = pollutionBonuses1.x + pollutionBonuses1.y < 2 * this.m_PollutionParameters.m_GroundPollutionNotificationLimit;
                            // ISSUE: reference to a compiler-generated field
                            bool flag2 = pollutionBonuses2.x + pollutionBonuses2.y < 2 * this.m_PollutionParameters.m_AirPollutionNotificationLimit;
                            // ISSUE: reference to a compiler-generated field
                            bool flag3 = noiseBonuses.x + noiseBonuses.y < 2 * this.m_PollutionParameters.m_NoisePollutionNotificationLimit;
                            // ISSUE: reference to a compiler-generated field
                            BuildingNotifications buildingNotification = this.m_BuildingNotifications[entity];
                            if (flag1 && !buildingNotification.HasNotification(BuildingNotification.GroundPollution))
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_IconCommandBuffer.Add(entity, this.m_PollutionParameters.m_GroundPollutionNotification, IconPriority.Problem);
                                buildingNotification.m_Notifications |= BuildingNotification.GroundPollution;
                                // ISSUE: reference to a compiler-generated field
                                this.m_BuildingNotifications[entity] = buildingNotification;
                            }
                            else if (!flag1 && buildingNotification.HasNotification(BuildingNotification.GroundPollution))
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_IconCommandBuffer.Remove(entity, this.m_PollutionParameters.m_GroundPollutionNotification);
                                buildingNotification.m_Notifications &= ~BuildingNotification.GroundPollution;
                                // ISSUE: reference to a compiler-generated field
                                this.m_BuildingNotifications[entity] = buildingNotification;
                            }
                            if (flag2 && !buildingNotification.HasNotification(BuildingNotification.AirPollution))
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_IconCommandBuffer.Add(entity, this.m_PollutionParameters.m_AirPollutionNotification, IconPriority.Problem);
                                buildingNotification.m_Notifications |= BuildingNotification.AirPollution;
                                // ISSUE: reference to a compiler-generated field
                                this.m_BuildingNotifications[entity] = buildingNotification;
                            }
                            else if (!flag2 && buildingNotification.HasNotification(BuildingNotification.AirPollution))
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_IconCommandBuffer.Remove(entity, this.m_PollutionParameters.m_AirPollutionNotification);
                                buildingNotification.m_Notifications &= ~BuildingNotification.AirPollution;
                                // ISSUE: reference to a compiler-generated field
                                this.m_BuildingNotifications[entity] = buildingNotification;
                            }
                            if (flag3 && !buildingNotification.HasNotification(BuildingNotification.NoisePollution))
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_IconCommandBuffer.Add(entity, this.m_PollutionParameters.m_NoisePollutionNotification, IconPriority.Problem);
                                buildingNotification.m_Notifications |= BuildingNotification.NoisePollution;
                                // ISSUE: reference to a compiler-generated field
                                this.m_BuildingNotifications[entity] = buildingNotification;
                            }
                            else if (!flag3 && buildingNotification.HasNotification(BuildingNotification.NoisePollution))
                            {
                                // ISSUE: reference to a compiler-generated field
                                // ISSUE: reference to a compiler-generated field
                                this.m_IconCommandBuffer.Remove(entity, this.m_PollutionParameters.m_NoisePollutionNotification);
                                buildingNotification.m_Notifications &= ~BuildingNotification.NoisePollution;
                                // ISSUE: reference to a compiler-generated field
                                this.m_BuildingNotifications[entity] = buildingNotification;
                            }
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        int rentPricePerRenter = PropertyUtils.GetRentPricePerRenter(buildingProperty, buildingLevel, lotSize, landValueBase, areaType, ref this.m_EconomyParameterData);
                        // ISSUE: reference to a compiler-generated field
                        if (this.m_OnMarkets.HasComponent(entity))
                        {
                            // ISSUE: reference to a compiler-generated field
                            PropertyOnMarket onMarket = this.m_OnMarkets[entity] with
                            {
                                m_AskingRent = rentPricePerRenter
                            };
                            // ISSUE: reference to a compiler-generated field
                            this.m_OnMarkets[entity] = onMarket;
                        }
                        int num2 = buildingProperty.CountProperties();
                        bool flag4 = false;
                        int2 int2 = new int2();
                        bool flag5 = false;
                        // ISSUE: reference to a compiler-generated field
                        bool flag6 = this.m_ExtractorProperties.HasComponent(entity);
                        for (int index2 = renters.Length - 1; index2 >= 0; --index2)
                        {
                            Entity renter1 = renters[index2].m_Renter;
                            // ISSUE: reference to a compiler-generated field
                            if (this.m_Renters.HasComponent(renter1))
                            {
                                // ISSUE: reference to a compiler-generated field
                                PropertyRenter renter2 = this.m_Renters[renter1];
                                // ISSUE: reference to a compiler-generated field
                                if (!this.m_ResourcesBuf.HasBuffer(renter1))
                                {
                                    UnityEngine.Debug.Log((object)string.Format("no resources:{0}", (object)renter1.Index));
                                }
                                else
                                {
                                    int num3;
                                    // ISSUE: reference to a compiler-generated field
                                    if (this.m_CitizenBufs.HasBuffer(renter1))
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        // ISSUE: reference to a compiler-generated field
                                        num3 = EconomyUtils.GetHouseholdIncome(this.m_CitizenBufs[renter1], ref this.m_Workers, ref this.m_CitizenDatas, ref this.m_HealthProblems, ref this.m_EconomyParameterData, this.m_TaxRates);

                                        if(num3 == 0)
                                        {
                                            DynamicBuffer<HouseholdCitizen> citizens = this.m_CitizenBufs[renter1];
                                            for (int i = 0; i < citizens.Length; i++)
                                            {
                                                Entity citizen = citizens[i].m_Citizen;
                                                Mod.log.Info($"i:{i},citizens:{citizens.Length},age:{this.m_CitizenDatas[citizen].GetAge()},worker:{m_Workers.HasComponent(citizen)},pension:{m_EconomyParameterData.m_Pension},dead:{CitizenUtils.IsDead(citizen, ref m_HealthProblems)}");
                                            }
                                            
                                        }
                                    }
                                    else
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        if (this.m_ProcessDatas.HasComponent(prefab))
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
                                            num3 = !this.m_OwnedVehicles.HasBuffer(renter1) ? EconomyUtils.GetCompanyTotalWorth(this.m_ResourcesBuf[renter1], this.m_ResourcePrefabs, this.m_ResourceDatas) : EconomyUtils.GetCompanyTotalWorth(this.m_ResourcesBuf[renter1], this.m_OwnedVehicles[renter1], this.m_LayoutElements, this.m_DeliveryTrucks, this.m_ResourcePrefabs, this.m_ResourceDatas);
                                        }
                                        else
                                            continue;
                                    }
                                    renter2.m_Rent = rentPricePerRenter;
                                    // ISSUE: reference to a compiler-generated field
                                    this.m_Renters[renter1] = renter2;
                                    // ISSUE: reference to a compiler-generated field
                                    flag5 |= this.m_StorageCompanies.HasComponent(renter1);
                                    // ISSUE: reference to a compiler-generated field
                                    if (rentPricePerRenter > num3 && !this.m_StorageCompanies.HasComponent(renter1))
                                    {
                                        // ISSUE: reference to a compiler-generated field
                                        this.m_CommandBuffer.AddComponent<PropertySeeker>(unfilteredChunkIndex, renter1, new PropertySeeker());
                                    }
                                    ++int2.y;
                                    if (rentPricePerRenter > num3)
                                    {
                                        ++int2.x;
                                        Mod.log.Info($"rentPricePerRenter:{rentPricePerRenter}, num3:{num3}");
                                    }
                                        
                                }
                            }
                            else
                            {
                                renters.RemoveAt(index2);
                                flag4 = true;
                            }
                        }
                        // ISSUE: reference to a compiler-generated method
                        if ((double)int2.x / (double)math.max(1f, (float)int2.y) <= 0.699999988079071 || !this.CanDisplayHighRentWarnIcon(renters))
                        {
                            Mod.log.Info($"calc:{(double)int2.x / (double)math.max(1f, (float)int2.y)}, int2:{int2}");
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_IconCommandBuffer.Remove(entity, this.m_BuildingConfigurationData.m_HighRentNotification);
                            building.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
                            // ISSUE: reference to a compiler-generated field
                            this.m_Buildings[entity] = building;
                        }
                        else if (renters.Length > 0 && !flag6 && (!flag5 || num2 > renters.Length) && (building.m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) == Game.Buildings.BuildingFlags.None)
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_IconCommandBuffer.Add(entity, this.m_BuildingConfigurationData.m_HighRentNotification, IconPriority.Problem);
                            building.m_Flags |= Game.Buildings.BuildingFlags.HighRentWarning;
                            // ISSUE: reference to a compiler-generated field
                            this.m_Buildings[entity] = building;
                        }
                        // ISSUE: reference to a compiler-generated field
                        if (renters.Length > num2 && this.m_Renters.HasComponent(renters[renters.Length - 1].m_Renter))
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, renters[renters.Length - 1].m_Renter);
                            renters.RemoveAt(renters.Length - 1);
                            UnityEngine.Debug.LogWarning((object)string.Format("Removed extra renter from building:{0}", (object)entity.Index));
                        }
                        if (renters.Length == 0 && (building.m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) != Game.Buildings.BuildingFlags.None)
                        {
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            this.m_IconCommandBuffer.Remove(entity, this.m_BuildingConfigurationData.m_HighRentNotification);
                            building.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
                            // ISSUE: reference to a compiler-generated field
                            this.m_Buildings[entity] = building;
                        }
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        if (((!this.m_Prefabs.HasComponent(entity) || this.m_Abandoned.HasComponent(entity) ? 0 : (!this.m_Destroyed.HasComponent(entity) ? 1 : 0)) & (flag4 ? 1 : 0)) != 0 && num2 > renters.Length)
                        {
                            // ISSUE: reference to a compiler-generated field
                            this.m_CommandBuffer.AddComponent<PropertyOnMarket>(unfilteredChunkIndex, entity, new PropertyOnMarket()
                            {
                                m_AskingRent = rentPricePerRenter
                            });
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

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            public BufferTypeHandle<Renter> __Game_Buildings_Renter_RW_BufferTypeHandle;
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RW_ComponentLookup;
            public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RW_ComponentLookup;
            public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<CompanyNotifications> __Game_Companies_CompanyNotifications_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;
            public ComponentLookup<BuildingNotifications> __Game_Buildings_BuildingNotifications_RW_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ExtractorProperty> __Game_Buildings_ExtractorProperty_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                // ISSUE: reference to a compiler-generated field
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                // ISSUE: reference to a compiler-generated field
                this.__Game_Buildings_Renter_RW_BufferTypeHandle = state.GetBufferTypeHandle<Renter>();
                // ISSUE: reference to a compiler-generated field
                this.__Game_Buildings_PropertyRenter_RW_ComponentLookup = state.GetComponentLookup<PropertyRenter>();
                // ISSUE: reference to a compiler-generated field
                this.__Game_Buildings_PropertyOnMarket_RW_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>();
                // ISSUE: reference to a compiler-generated field
                this.__Game_Buildings_Building_RW_ComponentLookup = state.GetComponentLookup<Building>();
                // ISSUE: reference to a compiler-generated field
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.BuildingData>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Companies_CompanyNotifications_RO_ComponentLookup = state.GetComponentLookup<CompanyNotifications>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Buildings_BuildingNotifications_RW_ComponentLookup = state.GetComponentLookup<BuildingNotifications>();
                // ISSUE: reference to a compiler-generated field
                this.__Game_Buildings_ExtractorProperty_RO_ComponentLookup = state.GetComponentLookup<ExtractorProperty>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
                // ISSUE: reference to a compiler-generated field
                this.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(true);
            }
        }
    }
}
