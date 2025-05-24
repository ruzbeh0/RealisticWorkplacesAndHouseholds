using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using RealisticWorkplacesAndHouseholds;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace RWH.Systems
{
    public partial class NoisePollutionParameterUpdaterSystem : GameSystemBase
    {
        private Dictionary<Entity, PollutionParameterData> _pollutionParameterData = new Dictionary<Entity, PollutionParameterData>();

        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<PollutionParameterData>()
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
                PollutionParameterData data;

                if (!_pollutionParameterData.TryGetValue(tsd, out data))
                {
                    data = EntityManager.GetComponentData<PollutionParameterData>(tsd);
                    _pollutionParameterData.Add(tsd, data);
                }

                data.m_NoiseMultiplier *= Mod.m_Setting.noise_factor/100f;

                EntityManager.SetComponentData<PollutionParameterData>(tsd, data);
            }
        }
        protected override void OnUpdate()
        {

        }
    }
}