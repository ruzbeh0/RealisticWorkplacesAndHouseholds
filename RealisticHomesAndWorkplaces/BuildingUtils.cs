using Colossal.Mathematics;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;

namespace RealisticWorkplacesAndHouseholds
{
    public static class BuildingUtils
    {
        //Get building dimensions. Original Method from the Realistic Occupancy Mod by Trejak
        public static Bounds3 GetBuildingDimensions(DynamicBuffer<SubMesh> subMeshes, ComponentLookup<MeshData> meshDataLookup)
        {
            var totalBounds = new Bounds3(0, 0);
            foreach (var submesh in subMeshes)
            {
                var meshData = meshDataLookup[submesh.m_SubMesh];
                if ((meshData.m_State & MeshFlags.Base) != MeshFlags.Base || (meshData.m_DecalLayer & Game.Rendering.DecalLayers.Buildings) != Game.Rendering.DecalLayers.Buildings)
                {
                    // not the main building of the asset, skip
                    continue;
                }
                totalBounds |= meshData.m_Bounds;
            }
            return totalBounds;
        }

        //Get number of people in the building (Students, workers or residents)
        public static int GetPeople(float width, float length, float height, float floor_height, float sqm_per_worker, bool skip_lobby, int floor_limit)
        {
            // commercial float SQM_PER_EMPLOYEE = 47;

            var floorSize = width * length;
            var floorCount = (int)math.floor(height / floor_height);
            if (skip_lobby)
            {
                floorCount--;
            }
            floorCount = math.min(floor_limit, math.max(floorCount, 1));

            return Math.Max((int)Math.Round((floorSize * floorCount) / sqm_per_worker, 0), 1);
        }

        public static int GetPeople(float width, float length, float height, float floor_height, float sqm_per_worker, bool skip_lobby)
        {
            // If no floor limit specified, use 163 which is the height of the world's tallest building
            int MAX_FLOORS = 163;
            return GetPeople(width, length, height, floor_height, sqm_per_worker, skip_lobby, MAX_FLOORS);
        }
    }
}
