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

namespace RWH.Systems
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
            FreeHomes = freeProperties.x + freeProperties.y + freeProperties.z;
            TotalHomes = HouseholdsCount + this.FreeHomes;

            bool enable = true;
            vacancy_rate = ((float)this.FreeHomes) / ((float)this.TotalHomes);

            if (this.TotalHomes == 0 || this.HouseholdsCount <= 10)
                enable = true;
            else if (vacancy_rate <= freeHomesPct)
                enable = false;

            this.World.GetOrCreateSystemManaged<HouseholdSpawnSystem>().Enabled = enable;

            var prefabs = _query.ToEntityArray(Allocator.Temp);

            foreach (var tsd in prefabs)
            {
                DemandParameterData data = EntityManager.GetComponentData<DemandParameterData>(tsd);

                data.m_FreeResidentialRequirement = new int3(Math.Max(5,(int)Math.Round(0.1* TotalHomes* freeHomesPct)), Math.Max(60, (int)Math.Round(0.4 * TotalHomes * freeHomesPct)), Math.Max(100, (int)Math.Round(0.5 * TotalHomes * freeHomesPct)));

                EntityManager.SetComponentData<DemandParameterData>(tsd, data);

                //Mod.log.Info($"freeHomesPct: {freeHomesPct}, freeProperties:{freeProperties}, HouseholdSpawnSystem Enabled: {enable}, Free Homes: {FreeHomes}, Total Homes: {TotalHomes}, vacancy_rate: {vacancy_rate}, m_FreeResidentialRequirement:{data.m_FreeResidentialRequirement}");

            }


        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 32;
        }
    }
}