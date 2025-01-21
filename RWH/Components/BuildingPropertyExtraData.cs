using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace RealisticWorkplacesAndHouseholds.Components
{
    public struct BuildingPropertyExtraData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version = 1;
        public BuildingPropertyExtraData(float fac)
        {
            this.factor = fac;
        }

        public float factor = default;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(factor);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out factor);
        }
    }
}
