//Original code by Trejak from the Building Occupancy Mod
using Colossal.Serialization.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealisticWorkplacesAndHouseholds.Jobs;
using Unity.Burst.Intrinsics;
using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    public partial class CheckBuildingsSystem : GameSystemBase
    {
        public bool initialized;
        EndFrameBarrier m_EndFrameBarrier;

        EntityQuery m_EconomyParamQuery;
        EntityQuery m_BuildingsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_BuildingsQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<ResidentialProperty>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<Renter>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<PropertyOnMarket>()
            );

            m_EconomyParamQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game && purpose == Purpose.LoadGame)
            {
                CheckBuildings();
            }
        }

        protected override void OnUpdate()
        {
            CheckBuildings();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 2;
        }

        private void CheckBuildings()
        {

            Mod.log.Info("Scheduling check for buildings that should be on the market");
            AddPropertiesToMarketJob job = new AddPropertiesToMarketJob()
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                commercialPropertyLookup = SystemAPI.GetComponentLookup<CommercialProperty>(true),
                entityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                prefabRefTypeHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                buildingTypeHandle = SystemAPI.GetComponentTypeHandle<Building>(true),
                propertyDataLookup = SystemAPI.GetComponentLookup<BuildingPropertyData>(true),
                renterTypeHandle = SystemAPI.GetBufferTypeHandle<Renter>(false),
                propertyToBeOnMarketLookup = SystemAPI.GetComponentLookup<PropertyToBeOnMarket>(false),
                propertyOnMarketLookup = SystemAPI.GetComponentLookup<PropertyOnMarket>(true),
                consumptionDataLookup = SystemAPI.GetComponentLookup<ConsumptionData>(true),
                landValueLookup = SystemAPI.GetComponentLookup<LandValue>(true),
                buildingDataLookup = SystemAPI.GetComponentLookup<BuildingData>(true),
                zoneDataLookup = SystemAPI.GetComponentLookup<ZoneData>(true),
                spawnableBuildingDataLookup = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true),
                economyParameterData = m_EconomyParamQuery.GetSingleton<EconomyParameterData>()

            };
            this.Dependency = job.ScheduleParallel(m_BuildingsQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }
    }
}
