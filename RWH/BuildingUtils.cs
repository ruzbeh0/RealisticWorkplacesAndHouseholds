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
        const float ELEVATOR_SPACE = 4.0f; // 4sqm for an elevator
        const float STAIRS_SPACE = 8.0f; // 8sqm for an elevator
        const int MIN_APT_SIZE = 60; // Square Meters
        const int MAX_FLOORS = 163;

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
        public static int GetPeople(bool residential, float width, float length, float height, float floor_height, float sqm_per_unit, int floor_offset, int sqm_per_elevator, int floor_limit)
        {
            var floorSize = width * length;
            var floorCount = (int)math.floor(height / floor_height);
            floorCount -= floor_offset;
            floorCount = math.min(floor_limit, math.max(floorCount, 1));

            float total_area = floorSize * floorCount;

            float total_elevator_area = 0f;
            if (sqm_per_elevator > 0)
            {
                int number_of_elevators = (int)Math.Floor(total_area / sqm_per_elevator);
                //Remove elevator area
                total_elevator_area = number_of_elevators * ELEVATOR_SPACE * floorCount;
                //Mod.log.Info($"Elevators: {number_of_elevators}, area: {total_area}");

            }

            //If few elevators one stair, if more than 4 elevators two stairs 
            int number_of_stairs = 1;
            if (floorCount >= 7 || width > 100 || length > 100)
            {
                //More stairs for tall or long bulidings
                number_of_stairs++;
            }
            float total_stair_area = number_of_stairs * STAIRS_SPACE * floorCount;


            if (residential)
            {
                if (sqm_per_unit > MIN_APT_SIZE)
                {
                    //If there is little space left in a floor area from a apartment, we remove that area.
                    //If this condition is met, what it represents is that this building has bigger apartments than the average
                    if (floorSize % sqm_per_unit < MIN_APT_SIZE)
                    {
                        floorSize -= floorSize % sqm_per_unit;
                    }
                } else
                {
                    //If it is a small apartment, lets have one per floor - with extra space for elevator and stairs

                    sqm_per_unit = floorSize - (total_stair_area + total_elevator_area) / floorCount;
                }
            }

            // Mod.log.Info($"Final Area: {total_area}, people: {Math.Max((int)Math.Round((total_area) / sqm_per_unit, 0), 1)}");

            total_area -= (total_stair_area + total_elevator_area);
            return Math.Max((int)Math.Round((total_area) / sqm_per_unit, 0), 1);
        }
        public static int GetPeople(bool residential, float width, float length, float height, float floor_height, float sqm_per_unit, int floor_offset, int sqm_per_elevator)
        {
            // If no floor limit specified, use 163 which is the height of the world's tallest building
            int MAX_FLOORS = 163;
            return GetPeople(residential, width, length, height, floor_height, sqm_per_unit, floor_offset, sqm_per_elevator, MAX_FLOORS);
        }

        public static int GetPeople(float width, float length, float height, float floor_height, float sqm_per_unit, int floor_offset, int sqm_per_elevator, int floor_limit)
        {
            // If residential not specified, false
            
            return GetPeople(false, width, length, height, floor_height, sqm_per_unit, floor_offset, sqm_per_elevator, floor_limit);
        }

        public static int GetPeople(float width, float length, float height, float floor_height, float sqm_per_unit, int floor_offset, int sqm_per_elevator)
        {
            // If no floor limit specified, use 163 which is the height of the world's tallest building
            // If residential not specified, false
            return GetPeople(false,width, length, height, floor_height, sqm_per_unit, floor_offset, sqm_per_elevator, MAX_FLOORS);
        }

        public static int GetPeople(float width, float length, float height, float floor_height, float sqm_per_unit, int sqm_per_elevator)
        {
            // If no floor offset is specified use zero
            // If residential not specified, false
            return GetPeople(false, width, length, height, floor_height, sqm_per_unit, 0, sqm_per_elevator, MAX_FLOORS);
        }

        //Smooths area factor. This is useful for building types that have a good employee per sqm at small buildings but them it is too much for big buildings
        public static float smooth_area_factor(float base_area, float x, float y)
        {
            float area = x * y;
            if (area < base_area)
            {
                return 1;
            }
            else
            {
                return (float)Math.Sqrt(area / base_area);
            }
        }
    }
}
