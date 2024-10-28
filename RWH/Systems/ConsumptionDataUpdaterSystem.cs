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
    public partial class ConsumptionDataUpdaterSystem : GameSystemBase
    {
        private Dictionary<Entity, ConsumptionData> _consumptionData = new Dictionary<Entity, ConsumptionData>();

        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<ConsumptionData>()
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
                ConsumptionData data;

                if (!_consumptionData.TryGetValue(tsd, out data))
                {
                    data = EntityManager.GetComponentData<ConsumptionData>(tsd);
                    _consumptionData.Add(tsd, data);
                }

                data.m_ElectricityConsumption *= (100 - Mod.m_Setting.electricity_consumption_reduction) / 100f;
                data.m_WaterConsumption *= (100 - Mod.m_Setting.water_consumption_reduction) / 100f;

                EntityManager.SetComponentData<ConsumptionData>(tsd, data);
            }
        }
        protected override void OnUpdate()
        {

        }
    }
}