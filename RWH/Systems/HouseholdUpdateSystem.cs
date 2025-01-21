using Game;
using Unity.Entities;
using UnityEngine.Scripting;
using Game.SceneFlow;
using Unity.Jobs;
using Game.Common;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Mathematics;
using Unity.Burst;
using Game.Settings;
using RealisticWorkplacesAndHouseholds;
using RealisticWorkplacesAndHouseholds.Jobs;
using Game.Buildings;
using Game.Companies;
using Unity.Collections;
using Game.UI.Menu;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [BurstCompile]
    public partial class HouseholdUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateHouseholdJobQuery;

        EndFrameBarrier m_EndFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Job Queries
            UpdateHouseholdJobQuery updateHouseholdJobQuery = new();
            m_UpdateHouseholdJobQuery = GetEntityQuery(updateHouseholdJobQuery.Query);

            this.RequireAnyForUpdate(m_UpdateHouseholdJobQuery);

        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (mode != GameMode.Game)
            {
                return;
            }
            //UpdateHouseholds();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            UpdateHouseholds();
            //Mod.log.Info("Household calculations loaded");
        }

        [Preserve]
        protected override void OnUpdate()
        {
            //UpdateHouseholds();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        private void UpdateHouseholds()
        {

            UpdateHouseholdJob updateHouseholdJob = new UpdateHouseholdJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataHandle = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                SubMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                SpawnableBuildingHandle = SystemAPI.GetComponentTypeHandle<SpawnableBuildingData>(true),
                BuildingPropertyDataHandle = SystemAPI.GetComponentTypeHandle<BuildingPropertyData>(false),
                ZoneDataLookup = SystemAPI.GetComponentLookup<ZoneData>(true),
                GroupAmbienceDataHandle = SystemAPI.GetComponentTypeHandle<GroupAmbienceData>(true),
                sqm_per_apartment = Mod.m_Setting.residential_sqm_per_apartment,
                residential_avg_floor_height = Mod.m_Setting.residential_avg_floor_height,
                enable_rh_apt_per_floor = Mod.m_Setting.disable_row_homes_apt_per_floor,
                rowhome_apt_per_floor = Mod.m_Setting.rowhomes_apt_per_floor,
                rowhome_basement = Mod.m_Setting.rowhomes_basement,
                units_per_elevator = Mod.m_Setting.residential_units_per_elevator,
                single_family = Mod.m_Setting.single_household_low_density,
                luxury_highrise_less_apt = !Mod.m_Setting.disable_high_level_less_apt,
                lv4_increase = Mod.m_Setting.residential_l4_reduction/100f,
                lv5_increase = Mod.m_Setting.residential_l5_reduction/100f,
                hallway_pct = Mod.m_Setting.residential_hallway_space/100f,
                global_reduction = Mod.m_Setting.results_reduction/100f,
                sqm_per_apartment_lowdensity = Mod.m_Setting.residential_lowdensity_sqm_per_apartment
            };
            this.Dependency = updateHouseholdJob.ScheduleParallel(m_UpdateHouseholdJobQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }
    }
}
