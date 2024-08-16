using Game;
using Unity.Entities;
using UnityEngine.Scripting;
using Game.SceneFlow;
using Unity.Jobs;
using Game.Common;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Mathematics;
using Unity.Burst;
using Game.Settings;
using RealisticWorkplacesAndHouseholds;
using RealisticWorkplacesAndHouseholds.Jobs;
using Game.Buildings;
using Game.Companies;
using Unity.Collections;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [BurstCompile]
    public partial class SchoolUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateSchoolsJobQuery;

        EndFrameBarrier m_EndFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Job Queries
            UpdateSchoolsJobQuery UpdateSchoolsJobQuery = new();
            m_UpdateSchoolsJobQuery = GetEntityQuery(UpdateSchoolsJobQuery.Query);

            RequireAnyForUpdate(
                m_UpdateSchoolsJobQuery
            );
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (mode != GameMode.Game)
            {
                return;
            }
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            UpdateSchools();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        private void UpdateSchools()
        {
            UpdateSchoolsJob updateSchoolJob = new UpdateSchoolsJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataLookup = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentTypeHandle<WorkplaceData>(false),
                SchoolDataLookup = SystemAPI.GetComponentTypeHandle<SchoolData>(false),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                subMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                studentPerTeacher = Mod.m_Setting.students_per_teacher,
                sqm_per_student = Mod.m_Setting.sqm_per_student,
                support_staff = Mod.m_Setting.support_staff/100f,
                sqm_per_student_college_factor = Mod.m_Setting.sqm_college_adjuster,
                sqm_per_student_university_factor = Mod.m_Setting.sqm_univ_adjuster,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height
            };
            this.Dependency = updateSchoolJob.ScheduleParallel(m_UpdateSchoolsJobQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }
    }
}
