using Game;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using static Unity.Burst.Intrinsics.X86.Avx;
using Unity.Mathematics;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    public partial class EconomyParameterUpdaterSystem : GameSystemBase
    {
        private EntityQuery _query;
        private Dictionary<Entity, EconomyParameterData> _baseline = new();

        protected override void OnCreate()
        {
            base.OnCreate();
            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<EconomyParameterData>()
                }
            });

            RequireForUpdate(_query);
        }

        protected override void OnUpdate()
        {
            var prefabs = _query.ToEntityArray(Allocator.Temp);
            foreach (var e in prefabs)
            {
                var data = EntityManager.GetComponentData<EconomyParameterData>(e);
                if (!_baseline.TryGetValue(e, out var baseData))
                {
                    _baseline[e] = data;              // cache pristine baseline once
                    baseData = data;
                }

                // now write from the baseline every time (no compounding)
                if (!Mod.m_Setting.disable_high_level_less_apt)
                {
                    int lv5 = Mod.m_Setting.residential_l5_reduction;
                    data.m_ResidentialUpkeepLevelExponent =
                        1f + (baseData.m_ResidentialUpkeepLevelExponent - 1f) * (100 - lv5) / 100f;
                }

                float rentK = math.max(0.01f, (100 - Mod.m_Setting.rent_discount) / 100f);
                data.m_RentPriceBuildingZoneTypeBase.x = 0.5f * rentK;
                data.m_LandValueModifier.x = 0.35f * rentK;

                EntityManager.SetComponentData(e, data);
            }
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 8;
        }
    }
}