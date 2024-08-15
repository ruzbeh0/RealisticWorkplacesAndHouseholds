using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace RealisticWorkplacesAndHouseholds.Prefabs
{
    public struct RealisticWorkplaceData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version = 1;
        public RealisticWorkplaceData()
        {
            
        }

        public int max_workers = default;
        public float space_multiplier = default;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(max_workers);
            writer.Write(space_multiplier);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out max_workers);
            reader.Read(out space_multiplier);
        }
    }
}
