//Definition of an abstract problem
//Authors: Lei He (l.he@tudelft.nl), Mathijs de Weerdt (M.M.deWeerdt@tudelft.nl), Neil Yorke-Smith (N.Yorke-Smith@tudelft.nl)
//Date: June 3, 2019
//License: CC-BY-NC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralTSALNS
{
    class problem
    {
        /// <summary>
        /// Whether the problem has a cost limit or not
        /// </summary>
        public bool IsCostLimit = false;
        /// <summary>
        /// Whether the problem has multiple time windows for the problem
        /// </summary>
        public bool IsMultiVTW = false;
        /// <summary>
        /// Whether the problem has due time penalty
        /// </summary>
        public bool IsDueTime = false;
        /// <summary>
        /// Whether the idle time heuristic should be used
        /// </summary>
        public bool IsIdleTime = false;
        /// <summary>
        /// The name of the problem
        /// </summary>
        public string ProblemName;
        /// <summary>
        /// The list of orders
        /// </summary>
        public List<Order> OrderList = new List<Order>();
        /// <summary>
        /// This is used for problem with multiple time windows. For example, in the satellite problem, each time window is an order, and each observation target is a super order which has multiple time windows
        /// </summary>
        public List<SuperOrder> SuperOrderList = new List<SuperOrder>();
        /// <summary>
        /// The list of scheduled orders
        /// </summary>
        public List<SelectedOrder> SelectedOrderList = new List<SelectedOrder>();
        /// <summary>
        /// The instance
        /// </summary>
        public Instance Instance = new Instance();
        /// <summary>
        /// List of all removal operators
        /// </summary>
        public int[] DestroyList;
        /// <summary>
        /// List of all insertion operators
        /// </summary>
        public int[] RepairList;
        /// <summary>
        /// The max iterations
        /// </summary>
        public int MaxIteration;
        public double TotalRevenue = 0;
        public double TotalUnitRevenue = 0;
        public double TotalSetup = 0;
        public double TotalConflict = 0;
        /// <summary>
        /// The shortest process time
        /// </summary>
        public double ShortProcess = 0;
        public bool IsFakeStart = false;
        public bool IsInstantTabu = false;
        public bool IsRemoveTabu = false;
        public bool IsInsertTabu = false;

        public virtual double GetSetupTime(SelectedOrder Pre, SelectedOrder Cur, double preend, double folsta)
        {
            return -1;
        }

        public virtual double GetEarliestTime(SelectedOrder Pre, SelectedOrder Cur)
        {
            return -1;
        }

        public virtual bool IsEnd(int noimprove, double Gfit)
        {
            return false;
        }

        public virtual double GetTempEarliestTime(double PreTempEnd, SelectedOrder Pre, SelectedOrder Cur)
        {
            return -1;
        }

        public virtual double GetTempTempEarliestTime(double PreTempEnd, double CurTempStart, SelectedOrder Pre, SelectedOrder Cur)
        {
            return -1;
        }

        public virtual double GetLatestTime(double SucStart, SelectedOrder Cur, SelectedOrder Suc)
        {
            return -1;
        }

        public virtual double GetDueLatestTime(double SucStart, SelectedOrder Cur, SelectedOrder Suc)
        {
            return -1;
        }

        public virtual int GetSizeOfRequestBank(int CurSize, int CurSolCount, int TotalCount)
        {
            return -1;
        }

        /// <summary>
        /// Check whether the solution is feasible
        /// </summary>
        /// <param name="sol">the solution</param>
        /// <returns></returns>
        public bool CheckSolution(List<SelectedOrder> sol)
        {
            bool IsFeasible = true;
            double EarliestTime = 0;
            for (int i = 0; i < sol.Count; i++)
            {
                sol[i].Order.FatherOrder.IsProcessed = false;
            }
            for (int i = 1; i < sol.Count; i++)
            {
                EarliestTime = GetEarliestTime(sol[i - 1], sol[i]);
                if (sol[i].RealStartTime < EarliestTime-0.001||sol[i].Order.FatherOrder.IsProcessed)
                {
                    IsFeasible = false;
                    break;
                }
                sol[i].Order.IsAccept = true;
                sol[i].Order.FatherOrder.IsProcessed=true;
            }
            
            if (IsCostLimit)
            {
                double totaltravel = 0;
                for (int jjj = 0; jjj < sol.Count - 1; jjj++)
                {
                    totaltravel += sol[jjj + 1].Setup;
                }
                if (totaltravel > Instance.CostLimit)
                {
                    IsFeasible = false;
                }
            }    
            return IsFeasible;
        }

        /// <summary>
        /// Calculate the total revenue of the solution
        /// </summary>
        /// <param name="sol"></param>
        /// <returns></returns>
        public double GetTotalProfit(List<SelectedOrder> sol)
        {
            double profit = 0;
            if (IsDueTime)
                for (int i = 0; i < sol.Count;i++ )
                {
                    double Tj = Math.Max(sol[i].RealEndTime - sol[i].Order.DueTime, 0);
                    sol[i].Profit = sol[i].Order.Revenue - sol[i].Order.Weight * Tj;
                    profit += sol[i].Profit;
                }
            else
                for (int i = 0; i < sol.Count; i++)
                    profit += sol[i].Order.Revenue;
            return profit;
        }

        /// <summary>
        /// Calculate the conflict between every two time windows
        /// </summary>
        public void GetConflictInfo()
        {
            List<Order> temporder = new List<Order>();
            temporder = OrderList.OrderBy(s => s.Release).ToList();
            double confitime = 0;

            for (int i = 0; i < temporder.Count; i++)
            {
                for (int j = i + 1; j < temporder.Count; j++)
                {
                    if (temporder[i].Deadline + temporder[i].ProcessTime > temporder[j].Release)
                    {
                        confitime = Math.Min(temporder[i].Deadline + temporder[i].ProcessTime - temporder[j].Release, temporder[j].Deadline + temporder[j].ProcessTime - temporder[j].Release);
                        temporder[i].Conflict += (confitime / (temporder[i].Deadline + temporder[i].ProcessTime - temporder[i].Release));
                        temporder[j].Conflict += (confitime / (temporder[j].Deadline + temporder[j].ProcessTime - temporder[j].Release));
                    }
                }
            }
        }
    }

    public class Order
    {
        /// <summary>
        /// The id of an order. The ID for the first and last order in the list is 0 and n+1 respectively
        /// </summary>
        public int OrderID;
        public double Release;
        public double ProcessTime;
        public double DueTime;
        /// <summary>
        /// The lastest start time of an order
        /// </summary>
        public double Deadline;
        public double Revenue;
        /// <summary>
        /// The weight for the penalty
        /// </summary>
        public double Weight;
        public double UnitRevenue;
        /// <summary>
        /// Whether the order is in the new solutino
        /// </summary>
        public bool IsInNewCurrent = false;
        /// <summary>
        /// Whether this is a dummy node or not;
        /// </summary>
        public bool IsDummy;
        public bool IsAccept = false;
        /// <summary>
        /// Once a task is inserted, the removal of this task is forbidden for a number of iterations
        /// </summary>
        public int RemoveTabu = -1;
        public int InsertTabu = -1;
        /// <summary>
        /// The minimum setuptime
        /// </summary>
        public double MinSetup;
        public double NearNeighborDis = 999999999999;
        public double Conflict = 0;
        public double TotalSetup = 0;
        public double BestDis = double.MaxValue;
        public double BestUnitRev = -1;
        public bool JustRemove = false;
        public bool IsStartPoint = false;

        public double RollA = 0;
        public double RollB = 0;
        public double PitchA = 0;
        public double PitchB = 0;
        
        public SuperOrder FatherOrder=new SuperOrder();

        public double BestValue = -1;
    }

    /// <summary>
    /// Definition of the partial sequence
    /// </summary>
    public class SmallPart
    {
        public double EndTime;
        public double Revenue;
        public double Time;
        public double UnitRevenue;
        public List<SelectedOrder> list = new List<SelectedOrder>();
    }

    /// <summary>
    /// SuperOrder
    /// </summary>
    public class SuperOrder
    {
        public int SuperOrderId;
        public List<Order> VTWList = new List<Order>();
        public double Revenue;
        public double ProcessTime;
        public bool IsProcessed;
        public bool IsInCompound;
    }

    /// <summary>
    /// Scheduled order
    /// </summary>
    public class SelectedOrder
    {
        public Order Order;
        public double TS;
        public double DueTS;
        public bool ToBeRemoved = false;//Whether this observation will be removed
        public double Setup;//The setup time of the order
        public bool JustInsert = false;
        public double Wait;
        public double RealStartTime;
        public double RealEndTime;
        public double Profit; //Calculated according to realstarttime
        public double HeuristicInfo;//remove or insert heuristic
    }

    /// <summary>
    /// Position in the solution is labeled as the id of order following the position
    /// </summary>
    public struct position
    {
        public int posi;
        public double trans;
        public double rand;
        public double heuristic;
    }
    /// <summary>
    /// Some meta information of an instance. But this is unnecessary for the algorithm
    /// </summary>
    class Instance
    {
        public struct Point
        {
            double x;
            double y;
        }
        public double SDSetup = 0;
        public double SDWindow = 0;
        public double SDRevenue = 0;
        public Point Centroid = new Point();
        public double Radius = 0;
        public double FractionDistictSetup = 0;
        public double Horizon = 0;
        public double VarianceNormalNearNeighbor = 0;
        public double CoefficientVariationofnNND = 0;
        public double ClusterRatio;
        public double OutlierRatio;
        public double EdgeOrderRatio;
        public double ClusterRadius;
        public int OrderNum;
        public double AvgWindowLength;
        public double OccupyRatio;
        public double SDProcess = 0;
        public double AvgMaxMinTDSetupRatio = 0;
        public double AvgConflictRatio = 0;
        public double SDConflictRatio = 0;
        public double ProcessWindowRatio = 0;
        public double SetupWindowRatio = 0;
        public double CostLimit = 0;
    }
}
