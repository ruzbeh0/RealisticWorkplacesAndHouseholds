using Game;
using Game.Simulation;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using RealisticWorkplacesAndHouseholds;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    public partial class ResidentialVacancySystem : GameSystemBase
    {
        private EntityQuery _query;
        public EntityQuery HousehodlsQuery;
        private int HouseholdsCount;
        private int FreeHomes;
        private int TotalHomes;
        private float freeHomesPct;
        public float vacancy_rate = 0;
        
        private CountResidentialPropertySystem m_CountResidentialPropertySystem;
        private CountResidentialPropertySystem.ResidentialPropertyData m_ResidentialPropertyData;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<DemandParameterData>()
                }
            });

            RequireForUpdate(_query);

            HousehodlsQuery = GetEntityQuery(
                ComponentType.ReadOnly<Household>(),
                ComponentType.Exclude<TouristHousehold>(),
                ComponentType.Exclude<CommuterHousehold>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>()
            );
        }

        protected override void OnUpdate()
        {
            m_CountResidentialPropertySystem = this.World.GetOrCreateSystemManaged<CountResidentialPropertySystem>();
            m_ResidentialPropertyData = this.m_CountResidentialPropertySystem.GetResidentialPropertyData();
            freeHomesPct = Mod.m_Setting.residential_vacancy_rate/100f;

            HouseholdsCount = HousehodlsQuery.CalculateEntityCount();
            int3 freeProperties = this.m_ResidentialPropertyData.m_FreeProperties;
            int3 totalProperties = this.m_ResidentialPropertyData.m_TotalProperties;
            FreeHomes = freeProperties.x + freeProperties.y + freeProperties.z;
            TotalHomes = HouseholdsCount + this.FreeHomes;

            vacancy_rate = ((float)this.FreeHomes) / ((float)this.TotalHomes);

            this.World.GetOrCreateSystemManaged<HouseholdSpawnSystem>().Enabled = false;

            using var prefabs = _query.ToEntityArray(Allocator.Temp);

            foreach (var tsd in prefabs)
            {
                DemandParameterData data = EntityManager.GetComponentData<DemandParameterData>(tsd);

                int3 targetFreeHomes = new int3(Math.Max(5,(int)Math.Round(0.1* TotalHomes* freeHomesPct)), Math.Max(60, (int)Math.Round(0.4 * TotalHomes * freeHomesPct)), Math.Max(100, (int)Math.Round(0.5 * TotalHomes * freeHomesPct)));
                data.m_FreeResidentialProportion = new float3(
                    totalProperties.x > 0 ? targetFreeHomes.x / (float)totalProperties.x : 0f,
                    totalProperties.y > 0 ? targetFreeHomes.y / (float)totalProperties.y : 0f,
                    totalProperties.z > 0 ? targetFreeHomes.z / (float)totalProperties.z : 0f);

                EntityManager.SetComponentData<DemandParameterData>(tsd, data);

                //Mod.log.Info($"freeHomesPct: {freeHomesPct}, freeProperties:{freeProperties}, Free Homes: {FreeHomes}, Total Homes: {TotalHomes}, vacancy_rate: {vacancy_rate}, m_FreeResidentialProportion:{data.m_FreeResidentialProportion}");

            }


        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 32;
        }
    }
}
