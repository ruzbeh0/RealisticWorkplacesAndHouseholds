using Colossal.Entities;
using Game;
using Game.City;
using Unity.Mathematics;
using Game.Simulation;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Entities.Internal;

public partial class RWHBudgetApplySystem : GameSystemBase
{
    public static readonly int kUpdatesPerDay = 1024 /*0x0400*/;
    private CitySystem m_CitySystem;
    private CityServiceBudgetSystem m_CityServiceBudgetSystem;
    private CityStatisticsSystem m_CityStatisticsSystem;
    private RWHBudgetApplySystem.TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 262144 /*0x040000*/ / BudgetApplySystem.kUpdatesPerDay;
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
        this.m_CityServiceBudgetSystem = this.World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
        this.m_CityStatisticsSystem = this.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
    }

    [UnityEngine.Scripting.Preserve]
    protected override void OnUpdate()
    {
        JobHandle deps1;
        JobHandle deps2;
        JobHandle deps3;

        RWHBudgetApplySystem.BudgetApplyJob jobData = new RWHBudgetApplySystem.BudgetApplyJob()
        {
            m_PlayerMoneys = InternalCompilerInterface.GetComponentLookup<PlayerMoney>(ref this.__TypeHandle.__Game_City_PlayerMoney_RW_ComponentLookup, ref this.CheckedStateRef),
            m_City = this.m_CitySystem.City,
            m_Expenses = this.m_CityServiceBudgetSystem.GetExpenseArray(out deps1),
            m_Income = this.m_CityServiceBudgetSystem.GetIncomeArray(out deps2),
            m_StatisticsEventQueue = this.m_CityStatisticsSystem.GetStatisticsEventQueue(out deps3).AsParallelWriter()
        };
        this.Dependency = jobData.Schedule<RWHBudgetApplySystem.BudgetApplyJob>(JobUtils.CombineDependencies(deps1, deps2, deps3, this.Dependency));
        this.m_CityServiceBudgetSystem.AddArrayReader(this.Dependency);
        this.m_CityStatisticsSystem.AddWriter(this.Dependency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
        new EntityQueryBuilder((AllocatorManager.AllocatorHandle)Allocator.Temp).Dispose();
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        this.__AssignQueries(ref this.CheckedStateRef);
        this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
    }

    [UnityEngine.Scripting.Preserve]
    public RWHBudgetApplySystem()
    {
    }

    //[BurstCompile]
    private struct BudgetApplyJob : IJob
    {
        public NativeArray<int> m_Income;
        public NativeArray<int> m_Expenses;
        public ComponentLookup<PlayerMoney> m_PlayerMoneys;
        public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;
        public Entity m_City;

        public void Execute()
        {
            int num = 0;
            StatisticsEvent statisticsEvent1;
            for (int source = 0; source < 15; ++source)
            {
                ExpenseSource expenseSource = (ExpenseSource)source;
                int expense = CityServiceBudgetSystem.GetExpense((ExpenseSource)source, this.m_Expenses);
                num -= expense;
                ref NativeQueue<StatisticsEvent>.ParallelWriter local = ref this.m_StatisticsEventQueue;
                statisticsEvent1 = new StatisticsEvent();
                statisticsEvent1.m_Statistic = StatisticType.Expense;
                statisticsEvent1.m_Change = math.abs((float)expense / (float)RWHBudgetApplySystem.kUpdatesPerDay);
                statisticsEvent1.m_Parameter = (int)expenseSource;
                StatisticsEvent statisticsEvent2 = statisticsEvent1;
                local.Enqueue(statisticsEvent2);
            }
            for (int source = 0; source < 14; ++source)
            {
                IncomeSource incomeSource = (IncomeSource)source;
                int income = CityServiceBudgetSystem.GetIncome((IncomeSource)source, this.m_Income);
                num += income;
                ref NativeQueue<StatisticsEvent>.ParallelWriter local = ref this.m_StatisticsEventQueue;
                statisticsEvent1 = new StatisticsEvent();
                statisticsEvent1.m_Statistic = StatisticType.Income;
                statisticsEvent1.m_Change = math.abs((float)income / (float)BudgetApplySystem.kUpdatesPerDay);
                statisticsEvent1.m_Parameter = (int)incomeSource;
                StatisticsEvent statisticsEvent3 = statisticsEvent1;
                local.Enqueue(statisticsEvent3);
            }
            PlayerMoney playerMoney = this.m_PlayerMoneys[this.m_City];
            playerMoney.Add(num / BudgetApplySystem.kUpdatesPerDay);
            this.m_PlayerMoneys[this.m_City] = playerMoney;
        }
    }

    private struct TypeHandle
    {
        public ComponentLookup<PlayerMoney> __Game_City_PlayerMoney_RW_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            this.__Game_City_PlayerMoney_RW_ComponentLookup = state.GetComponentLookup<PlayerMoney>();
        }
    }
}

