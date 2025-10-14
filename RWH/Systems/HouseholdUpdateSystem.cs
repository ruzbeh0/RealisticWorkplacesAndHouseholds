using Game;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [BurstCompile]
    public partial class HouseholdUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateHouseholdJobQuery;
        private EntityQuery m_ResetHouseholdJobQuery;

        private bool m_TriggerInitialHouseholdUpdate = false;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            // Normal query — excludes already processed buildings
            UpdateHouseholdJobQuery updateQuery = new();
            m_UpdateHouseholdJobQuery = GetEntityQuery(updateQuery.Query);

            // Reset query — includes all buildings
            EntityQueryDesc resetQuery = new()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<BuildingData>(),
                    ComponentType.ReadOnly<BuildingPropertyData>(),
                    ComponentType.ReadOnly<GroupAmbienceData>(),
                    ComponentType.ReadWrite<SpawnableBuildingData>(),
                    ComponentType.ReadWrite<SubMesh>(),
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            };
            m_ResetHouseholdJobQuery = GetEntityQuery(resetQuery);

            this.RequireForUpdate(m_UpdateHouseholdJobQuery);
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game && purpose == Colossal.Serialization.Entities.Purpose.LoadGame)
            {
                UpdateHouseholds(reset: true);
                m_TriggerInitialHouseholdUpdate = false;
            } else
            {
                m_TriggerInitialHouseholdUpdate = true;
            }  
        }

        [Preserve]
        protected override void OnUpdate()
        {
            //Mod.log.Info($"OnUpdate: {m_TriggerInitialHouseholdUpdate}");
            if (m_TriggerInitialHouseholdUpdate)
            {
                UpdateHouseholds(reset: true);
                m_TriggerInitialHouseholdUpdate = false;
            }
            else
            {
                UpdateHouseholds(reset: false);
            }
        }

        private void UpdateHouseholds(bool reset)
        {
            var query = reset ? m_ResetHouseholdJobQuery : m_UpdateHouseholdJobQuery;

            if (reset)
                query.ResetFilter();
            else
                query.SetChangedVersionFilter(typeof(BuildingPropertyData));

            var commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            UpdateHouseholdJob job = new UpdateHouseholdJob
            {
                ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataHandle = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                SubMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                SpawnableBuildingHandle = SystemAPI.GetComponentTypeHandle<SpawnableBuildingData>(true),
                BuildingPropertyDataHandle = SystemAPI.GetComponentTypeHandle<BuildingPropertyData>(false),
                ZoneDataLookup = SystemAPI.GetComponentLookup<ZoneData>(true),
                GroupAmbienceDataHandle = SystemAPI.GetComponentTypeHandle<GroupAmbienceData>(true),
                RealisticHouseholdDataLookup = SystemAPI.GetComponentLookup<RealisticHouseholdData>(true),
                sqm_per_apartment = Mod.m_Setting.residential_sqm_per_apartment,
                residential_avg_floor_height = Mod.m_Setting.residential_avg_floor_height,
                enable_rh_apt_per_floor = Mod.m_Setting.disable_row_homes_apt_per_floor,
                rowhome_apt_per_floor = Mod.m_Setting.rowhomes_apt_per_floor,
                rowhome_basement = Mod.m_Setting.rowhomes_basement,
                units_per_elevator = Mod.m_Setting.residential_units_per_elevator,
                single_family = Mod.m_Setting.single_household_low_density,
                luxury_highrise_less_apt = !Mod.m_Setting.disable_high_level_less_apt,
                lv4_increase = Mod.m_Setting.residential_l4_reduction / 100f,
                lv5_increase = Mod.m_Setting.residential_l5_reduction / 100f,
                hallway_pct = Mod.m_Setting.residential_hallway_space / 100f,
                global_reduction = Mod.m_Setting.results_reduction / 100f,
                sqm_per_apartment_lowdensity = Mod.m_Setting.residential_lowdensity_sqm_per_apartment,
                UffLookup = SystemAPI.GetComponentLookup<RealisticWorkplacesAndHouseholds.Components.UsableFootprintFactor>(true),
                reset = reset
            };

            this.Dependency = job.ScheduleParallel(query, this.Dependency);
            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
