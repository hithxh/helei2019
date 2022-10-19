//Definition of TDOPTW problem
//Authors: Lei He (l.he@tudelft.nl), Mathijs de Weerdt (M.M.deWeerdt@tudelft.nl), Neil Yorke-Smith (N.Yorke-Smith@tudelft.nl)
//Date: June 3, 2019
//License: CC-BY-NC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace GeneralTSALNS
{
    class TDOPTWProblem:problem
    {
        /// <summary>
        /// The setup time for different slots
        /// </summary>
        public struct STL
        {
            public double[] SetupList;
        }

        /// <summary>
        /// The setup time
        /// </summary>
        STL[,] SetupTime;
        /// <summary>
        /// Miu and V are used to calculated the setup times. More information is given by Verbeeck et al. (2017)
        /// Verbeeck C, Vansteenwegen P, Aghezzaf E H. The time-dependent orienteering problem with time windows: a fast ant colony system[J]. Annals of Operations Research, 2017, 254(1-2): 481-505.
        /// </summary>
        double[, ,] Miu;
        double[, ,] V;

        /// <summary>
        /// Initialization of the problem
        /// </summary>
        public void ProblemIntialize()
        {
            ProblemName = "TDOPTW";
            IsCostLimit = true;
            IsMultiVTW = false;
            IsDueTime = false;
            IsIdleTime = true;
            IsFakeStart = true;
            IsInstantTabu = true;
            IsRemoveTabu = true;
            IsInsertTabu = true;
            DestroyList = new int[9] { 0, 1, 2, 3, 5, 6, 7, 8, 9 };
            RepairList = new int[5] { 0, 1 ,4,5,6};
            MaxIteration = 50000;
        }

        /// <summary>
        /// Determine whether the problem should be terminated
        /// </summary>
        /// <param name="noimprove">the number of noimprove iterations</param>
        /// <param name="Gfit">current best revenue</param>
        /// <returns></returns>
        public override bool IsEnd(int noimprove, double Gfit)
        {
            if (Gfit == TotalRevenue)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Calculate the shortest processing time, total revenue, total unit revenue, total setup, total conflict
        /// </summary>
        public void PreProcess()
        {
            ShortProcess = double.MaxValue;

            for (int i = 0; i < OrderList.Count; i++)
            {
                TotalRevenue += OrderList[i].Revenue;
            }    
            for (int i = 0; i < OrderList.Count; i++)
            {
                TotalUnitRevenue += OrderList[i].UnitRevenue;
                TotalSetup += OrderList[i].TotalSetup;
                TotalConflict += OrderList[i].Conflict;
                if (OrderList[i].ProcessTime < ShortProcess && OrderList[i].ProcessTime != 0)
                    ShortProcess = OrderList[i].ProcessTime;
            }
        }

        /// <summary>
        /// Calculate the number of orders to be removed
        /// </summary>
        /// <param name="CurSize">The current number of orders to be removed</param>
        /// <param name="CurSolCount">The number of orders in the current solution</param>
        /// <param name="TotalCount">Total number of iterations</param>
        /// <returns></returns>
        public override int GetSizeOfRequestBank(int CurSize, int CurSolCount, int TotalCount)
        {
            if (TotalCount % (MaxIteration / 5) == 1 && TotalCount > 1)
                return (CurSize + 1);
            else
                return CurSize;
        }

        /// <summary>
        /// Read instances from files
        /// </summary>
        /// <param name="OrderNum"></param>
        /// <param name="Tao"></param>
        /// <param name="R"></param>
        public void LoadInstance(string OrderNum, string Tao, string R)
        {
            ProblemIntialize();

            this.IsCostLimit = true;

            StreamReader OrderReader = new StreamReader(@"...\...\...\...\TDOPTW\" + OrderNum + '.' + Tao + '.' + R + ".TXT");
            StreamReader SetupReader = new StreamReader(@"...\...\...\...\TDOPTW\tt" + OrderNum + ".TXT");

            string OrderInfo = OrderReader.ReadLine();
            this.Instance.OrderNum = int.Parse(OrderNum);

            OrderInfo = OrderReader.ReadLine();
            Instance.CostLimit = double.Parse(OrderInfo);

            int ii = 0;
            

            while (!OrderReader.EndOfStream)
            {
                OrderInfo = OrderReader.ReadLine();
                string[] oi = OrderInfo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                Order temporder = new Order();
                temporder.OrderID = ii;
                temporder.Revenue = double.Parse(oi[1]);
                temporder.ProcessTime = double.Parse(oi[3]);
                temporder.Release = double.Parse(oi[4]);
                temporder.UnitRevenue = temporder.Revenue / temporder.ProcessTime;
                temporder.Deadline = double.Parse(oi[5]);
                OrderList.Add(temporder);
                ii++;
            }

            OrderList[OrderList.Count - 1].Release = OrderList[OrderList.Count - 1].Deadline;

            OrderInfo = SetupReader.ReadLine();
            string[] oi1 = OrderInfo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            int count = 0;

            SetupTime = new STL[this.Instance.OrderNum, this.Instance.OrderNum];

            for (int i = 0; i < OrderList.Count; i++)
            {
                for (int j = 0; j < OrderList.Count; j++)
                {
                    SetupTime[i, j].SetupList = new double[56];
                    for (int p = 0; p < 56; p++)
                    {
                        SetupTime[i, j].SetupList[p] = double.Parse(oi1[count]);
                        if (SetupTime[i, j].SetupList[p] < OrderList[i].MinSetup && SetupTime[i, j].SetupList[p] != 0)
                            OrderList[i].MinSetup = SetupTime[i, j].SetupList[p];
                        count++;
                    }
                }
            }

            OrderList[0].IsAccept = true;
            OrderList[0].IsDummy = true;
            OrderList[OrderList.Count - 1].IsAccept = true;
            OrderList[OrderList.Count - 1].IsDummy = true;

            Miu = new double[this.Instance.OrderNum, this.Instance.OrderNum, 55];
            V = new double[this.Instance.OrderNum, this.Instance.OrderNum, 55];

            for (int i = 0; i < this.Instance.OrderNum; i++)
                for (int j = 0; j < this.Instance.OrderNum; j++)
                    for (int k = 0; k < 55; k++)
                    {
                        double SlotStart = k * 900000 + 21600000;
                        Miu[i, j, k] = (SetupTime[i, j].SetupList[k + 1] - SetupTime[i, j].SetupList[k]) / 900000;
                        V[i, j, k] = SetupTime[i, j].SetupList[k] - Miu[i, j, k] * SlotStart;
                    }

            GetConflictInfo();
            PreProcess();
        }

        /// <summary>
        /// Calculate the setup time
        /// </summary>
        /// <param name="Pre"></param>
        /// <param name="Cur"></param>
        /// <param name="preend"></param>
        /// <param name="folsta"></param>
        /// <returns></returns>
        public override double GetSetupTime(SelectedOrder Pre, SelectedOrder Cur, double preend, double folsta)
        {
            double Trans = 0;
            int Slot = (int)((preend - 21600000) / 900000);
            double SlotStart = Slot * 900000 + 21600000;

            if (Slot > 55)
                return int.MaxValue;
            if (Slot == 55)
                return SetupTime[Pre.Order.OrderID, Cur.Order.OrderID].SetupList[Slot];
            else
            {
                Trans = Miu[Pre.Order.OrderID, Cur.Order.OrderID, Slot] * preend + V[Pre.Order.OrderID, Cur.Order.OrderID, Slot];
            }
            return Trans;
        }

        /// <summary>
        /// Get the earliest start time of the current order according to its precursor
        /// </summary>
        /// <param name="Pre"></param>
        /// <param name="Cur"></param>
        /// <returns></returns>
        public override double GetEarliestTime(SelectedOrder Pre, SelectedOrder Cur)
        {
            double EarliestTime = 0;
            double ST = GetSetupTime(Pre, Cur, Pre.RealEndTime, Cur.RealStartTime);
            EarliestTime = Math.Max(Pre.RealEndTime + ST, Cur.Order.Release);
            return EarliestTime;
        }

        /// <summary>
        /// Get the earliest start time of the current order according to a temporary end time of its precursor
        /// </summary>
        /// <param name="Pre"></param>
        /// <param name="Cur"></param>
        /// <returns></returns>
        public override double GetTempEarliestTime(double PreTempEnd, SelectedOrder Pre, SelectedOrder Cur)
        {
            double EarliestTime = 0;
            double ST = GetSetupTime(Pre, Cur, PreTempEnd, Cur.RealStartTime);
            EarliestTime = Math.Max(PreTempEnd + ST, Cur.Order.Release);
            return EarliestTime;
        }

        /// <summary>
        /// Get the latest start time of the current order according to its successor
        /// </summary>
        /// <param name="Pre"></param>
        /// <param name="Cur"></param>
        /// <returns></returns>
        public override double GetLatestTime(double SucStart, SelectedOrder Cur, SelectedOrder Suc)
        {
            double latestart = 0;

            int Slot = (int)((SucStart - 21600000) / 900000);
            double SlotStart = Slot * 900000 + 21600000;

            while (true)
            {
                if (Slot == 55 || Slot == 56)
                {
                    latestart = SucStart - SetupTime[Cur.Order.OrderID, Suc.Order.OrderID].SetupList[55];
                    Slot = 55;
                    SlotStart = 55 * 900000 + 21600000;
                }
                else
                {
                    latestart = (SucStart - V[Cur.Order.OrderID, Suc.Order.OrderID, Slot]) / (1 + Miu[Cur.Order.OrderID, Suc.Order.OrderID, Slot]);
                }
                if (latestart >= SlotStart && latestart < SlotStart + 900000)
                    break;
                else
                {
                    Slot--;
                    SlotStart = Slot * 900000 + 21600000;
                }
            }
            latestart -= Cur.Order.ProcessTime;
            if (latestart > Cur.Order.Deadline)
                latestart = Cur.Order.Deadline;
            return latestart;
        }
    }
}
