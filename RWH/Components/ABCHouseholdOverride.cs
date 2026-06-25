using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace RealisticWorkplacesAndHouseholds.Components
{
    /// <summary>
    /// Marker placed on a BUILDING PREFAB when Advanced Building Control
    /// has an active BuildingPropertyData_ResidentialProperties override for that prefab.
    /// </summary>
    public struct ABCHouseholdOverride : IComponentData
    {
    }
}
