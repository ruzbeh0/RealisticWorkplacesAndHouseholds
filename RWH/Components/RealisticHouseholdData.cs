using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace RealisticWorkplacesAndHouseholds.Components
{
    public struct RealisticHouseholdData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version = 1;
        public RealisticHouseholdData()
        {
            
        }

        public int households = default;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(households);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out households);
        }
    }
}
