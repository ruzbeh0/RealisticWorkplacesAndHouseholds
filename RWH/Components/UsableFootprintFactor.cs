using Colossal.Mathematics;
using Game;
using Game.Prefabs;
using RealisticWorkplacesAndHouseholds;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace RealisticWorkplacesAndHouseholds.Components
{
    // 0..1 multiplier stored on the PREFAB entity
    public struct UsableFootprintFactor : IComponentData
    {
        public float Value;
    }
}