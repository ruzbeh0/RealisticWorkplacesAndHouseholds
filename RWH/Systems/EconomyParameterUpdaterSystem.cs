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

            foreach (var tsd in prefabs)
            {
                EconomyParameterData data = EntityManager.GetComponentData<EconomyParameterData>(tsd);

                if (!Mod.m_Setting.disable_high_level_less_apt)
                {
                    int lv5_reduction = Mod.m_Setting.residential_l5_reduction;
                    data.m_ResidentialUpkeepLevelExponent = 1f + (data.m_ResidentialUpkeepLevelExponent - 1) * (100 - lv5_reduction) / 100f;
                }

                data.m_RentPriceBuildingZoneTypeBase.x = 0.5f*math.max(0.01f, (100 - Mod.m_Setting.rent_discount) / 100f);
                data.m_LandValueModifier.x = 0.35f*math.max(0.01f, (100 - Mod.m_Setting.rent_discount) / 100f);
                EntityManager.SetComponentData<EconomyParameterData>(tsd, data);
            }  
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / 8;
        }
    }
}