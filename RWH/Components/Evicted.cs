using Colossal;
using Colossal.Serialization.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds.Components
{
    /// <summary>
    /// Added to households that were removed due to a lower number of properties
    /// </summary>
    public partial struct Evicted : IComponentData, ISerializable, IDefaultSerializable
    {
        /// <summary>
        /// The property that they were evicted from
        /// </summary>
        public Entity from;
        int version;

        public Evicted(Entity from)
        {
            this.from = from;
            this.version = 0;
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out this.from);
            reader.Read(out this.version);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(this.from);
            writer.Write(this.version);
        }

        public void SetDefaults(Context context)
        {
            this.from = Entity.Null;
            this.version = 0;
        }
    }
}
