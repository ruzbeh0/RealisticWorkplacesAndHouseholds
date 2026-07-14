using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds.Components
{
    public struct SignatureUnlockRequirementOriginalData : IComponentData
    {
        public int MinimumSquares;
        public int MinimumCount;
    }
}
