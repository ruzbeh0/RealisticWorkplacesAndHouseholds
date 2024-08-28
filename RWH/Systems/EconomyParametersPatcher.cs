using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    internal class EconomyParametersPatcher
    {
        private EntityManager m_EntityManager;
        private PrefabSystem m_PrefabSystem;

        internal EconomyParametersPatcher()
        {
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
            m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        internal bool TryGetPrefab(string prefabType, string prefabName, out PrefabBase prefabBase, out Entity entity)
        {
            prefabBase = null;
            entity = default;
            PrefabID prefabID = new PrefabID(prefabType, prefabName);
            return m_PrefabSystem.TryGetPrefab(prefabID, out prefabBase) && m_PrefabSystem.TryGetEntity(prefabBase, out entity);
        }

        internal void PatchEconomyParameters()
        {
            if (TryGetPrefab(nameof(EconomyPrefab), "EconomyParameters", out PrefabBase prefabBase, out Entity entity) && m_PrefabSystem.TryGetComponentData<EconomyParameterData>(prefabBase, out EconomyParameterData comp))
            {
                if(!Mod.m_Setting.disable_high_level_less_apt)
                {
                    int lv5_reduction = Mod.m_Setting.residential_l5_reduction;
                    comp.m_ResidentialUpkeepLevelExponent = 1f + (comp.m_ResidentialUpkeepLevelExponent - 1)*(100 - lv5_reduction)/100f;
                }

                comp.m_RentPriceBuildingZoneTypeBase.x *= math.max(0, (100 - (100f / Mod.m_Setting.results_reduction) * Mod.m_Setting.rent_discount) / 100f);
                comp.m_LandValueModifier.x = math.max(0,(100 - (100f / Mod.m_Setting.results_reduction) * Mod.m_Setting.rent_discount) / 100f);

                m_PrefabSystem.AddComponentData(prefabBase, comp);
            }
        }
    }
}