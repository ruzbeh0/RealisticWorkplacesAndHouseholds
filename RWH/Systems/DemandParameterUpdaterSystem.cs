using Colossal.Serialization.Entities;
using Game;
using Game.City;
using Game.Prefabs;
using RealisticWorkplacesAndHouseholds;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace RWH.Systems
{
    public partial class DemandParameterUpdaterSystem : GameSystemBase
    {
        private Dictionary<Entity, DemandParameterData> _demandParameterData = new Dictionary<Entity, DemandParameterData>();

        private EntityQuery _query;

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
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            var prefabs = _query.ToEntityArray(Allocator.Temp);

            foreach (var tsd in prefabs)
            {
                DemandParameterData data;

                if (!_demandParameterData.TryGetValue(tsd, out data))
                {
                    data = EntityManager.GetComponentData<DemandParameterData>(tsd);
                    _demandParameterData.Add(tsd, data);
                }

                //Population population = this.m_Populations[this.m_City];
                data.m_FreeResidentialRequirement *= 10;

                EntityManager.SetComponentData<DemandParameterData>(tsd, data);
            }
        }
        protected override void OnUpdate()
        {

        }
    }
}