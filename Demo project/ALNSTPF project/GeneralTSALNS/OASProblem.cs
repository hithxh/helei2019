//Definition of the OAS problem
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
    class OASProblem:problem
    {
        //The sequence-dependent setup times are stored in a n*n table
        public double[,] SetupTime;

        /// <summary>
        /// Initialization of the problem
        /// </summary>
        public void ProblemIntialize(string OrderNum)
        {
            ProblemName = "OAS";
            IsCostLimit = false;
            IsMultiVTW = false;
            IsDueTime = true;
            IsIdleTime = true;
            IsFakeStart = false;
            IsInstantTabu = false;
            IsRemoveTabu = true;
            IsInsertTabu = true;
            DestroyList = new int[6] { 0,1,2,3,8,9 };
            RepairList = new int[3] { 0, 1 ,5};
            MaxIteration = 1000 * int.Parse(OrderNum);
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
        /// Read instances from files
        /// </summary>
        /// <param name="OrderNum"></param>
        /// <param name="Tao"></param>
        /// <param name="R"></param>
        /// <param name="InstanceNumber"></param>
        public void LoadInstance(string OrderNum, string Tao, string R, string InstanceNumber)
        {
            ProblemIntialize(OrderNum);

            StreamReader OrderReader = new StreamReader(@"...\...\...\...\OAS\" + OrderNum + "orders\\Tao" + Tao + "\\R" + R + "\\Dataslack_" + OrderNum + "orders_Tao" + Tao + "R" + R + "_" + InstanceNumber + ".txt");
            this.Instance.OrderNum = int.Parse(OrderNum);

            SetupTime = new double[this.Instance.OrderNum + 2, this.Instance.OrderNum + 2];

            string OrderInfo = OrderReader.ReadLine();
            string[] oi = OrderInfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < this.Instance.OrderNum + 2; i++)
            {
                Order temporder = new Order();
                temporder.OrderID = i;
                temporder.Release = double.Parse(oi[i]);
                OrderList.Add(temporder);
            }

            OrderInfo = OrderReader.ReadLine();
            oi = OrderInfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < this.Instance.OrderNum + 2; i++)
                OrderList[i].ProcessTime = double.Parse(oi[i]);

            OrderInfo = OrderReader.ReadLine();
            oi = OrderInfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < this.Instance.OrderNum + 2; i++)
                OrderList[i].DueTime = double.Parse(oi[i]);

            OrderInfo = OrderReader.ReadLine();
            oi = OrderInfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < this.Instance.OrderNum + 2; i++)
            {
                OrderList[i].Deadline = double.Parse(oi[i]);
                OrderList[i].Deadline -= OrderList[i].ProcessTime;//Change it to the latest start time
            }

            OrderInfo = OrderReader.ReadLine();
            oi = OrderInfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < this.Instance.OrderNum + 2; i++)
            {
                OrderList[i].Revenue = double.Parse(oi[i]);
                OrderList[i].UnitRevenue = OrderList[i].Revenue / (OrderList[i].ProcessTime + 0.001);
            }
            OrderInfo = OrderReader.ReadLine();
            oi = OrderInfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < this.Instance.OrderNum + 2; i++)
                OrderList[i].Weight = double.Parse(oi[i]);

            for (int i = 0; i < this.Instance.OrderNum + 2; i++)
            {
                OrderInfo = OrderReader.ReadLine();
                oi = OrderInfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < this.Instance.OrderNum + 2; j++)
                {
                    SetupTime[i, j] = double.Parse(oi[j]);
                    if (SetupTime[i, j] < 0)
                        SetupTime[i, j] = 0;
                }
            }
            OrderReader.Close();

            OrderList[OrderList.Count - 1].Release = OrderList[OrderList.Count - 1].Deadline;
            OrderList[0].IsAccept = true;
            OrderList[0].IsDummy = true;
            OrderList[OrderList.Count - 1].IsAccept = true;
            OrderList[OrderList.Count - 1].IsDummy = true;

            double totalsetup = 0;
            for (int i = 0; i < OrderList.Count; i++)
            {
                totalsetup = 0;
                OrderList[i].MinSetup = 100;
                for (int j = 0; j < OrderList.Count; j++)
                {
                    totalsetup += SetupTime[j, i];
                    if (SetupTime[j, i] < OrderList[i].MinSetup && i != j && j != OrderList.Count - 1)
                        OrderList[i].MinSetup = SetupTime[j, i];
                }
                OrderList[i].TotalSetup = totalsetup;
            }
            totalsetup=0;
            for (int i = 0; i < OrderList.Count; i++)
                for (int j = 0; j < OrderList.Count; j++)
                    totalsetup += SetupTime[i, j];
            totalsetup /= (OrderList.Count * OrderList.Count);

            GetConflictInfo();
            PreProcess();
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
            int size = 0;
            size = (int)Math.Ceiling(CurSolCount * 0.1 );
            return size;
        }

        /// <summary>
        /// Calculate the setup time
        /// </summary>
        /// <param name="Pre">pre order</param>
        /// <param name="Cur">current order</param>
        /// <param name="preend">the end of pre order</param>
        /// <param name="folsta">the start of the current order</param>
        /// <returns></returns>
        public override double GetSetupTime(SelectedOrder Pre, SelectedOrder Cur, double preend, double folsta)
        {
            return SetupTime[Pre.Order.OrderID, Cur.Order.OrderID];
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
        /// Get the earliest start time of the current order according to its precursor
        /// </summary>
        /// <param name="Pre"></param>
        /// <param name="Cur"></param>
        /// <returns></returns>
        public override double GetEarliestTime(SelectedOrder Pre, SelectedOrder Cur)
        {
            double EarliestTime = 0;
            double ST = GetSetupTime(Pre, Cur, Pre.RealEndTime, Cur.RealStartTime);
            EarliestTime = Math.Max(Pre.RealEndTime + ST, Cur.Order.Release + ST);
            return EarliestTime;
        }

        /// <summary>
        /// Get the earliest start time of the current order according to a temporary end time of its precursor
        /// </summary>
        /// <param name="PreTempEnd"></param>
        /// <param name="Pre"></param>
        /// <param name="Cur"></param>
        /// <returns></returns>
        public override double GetTempEarliestTime(double PreTempEnd, SelectedOrder Pre, SelectedOrder Cur)
        {
            double EarliestTime = 0;
            double ST = GetSetupTime(Pre, Cur, PreTempEnd, Cur.RealStartTime);
            EarliestTime = Math.Max(PreTempEnd + ST, Cur.Order.Release + ST);
            return EarliestTime;
        }

        /// <summary>
        /// Get the earliest start time of the current order according to a temporary end time of its precursor
        /// </summary>
        /// <param name="PreTempEnd"></param>
        /// <param name="Pre"></param>
        /// <param name="Cur"></param>
        /// <returns></returns>
        public override double GetTempTempEarliestTime(double PreTempEnd,double CurTempStart, SelectedOrder Pre, SelectedOrder Cur)
        {
            double EarliestTime = 0;
            double ST = GetSetupTime(Pre, Cur, PreTempEnd, CurTempStart);
            EarliestTime = Math.Max(PreTempEnd + ST, Cur.Order.Release + ST);
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
            double LatestTime = 0;
            LatestTime = Math.Min(Cur.Order.Deadline, SucStart - SetupTime[Cur.Order.OrderID,Suc.Order.OrderID]-Cur.Order.ProcessTime);
            return LatestTime;
        }

        /// <summary>
        /// Get the latest start time of the current order that the order will receive no penalty
        /// </summary>
        /// <param name="Pre"></param>
        /// <param name="Cur"></param>
        /// <returns></returns>
        public override double GetDueLatestTime(double SucStart, SelectedOrder Cur, SelectedOrder Suc)
        {
            double LatestTime = 0;
            LatestTime = Math.Min(Cur.Order.DueTime-Cur.Order.ProcessTime, SucStart - SetupTime[Cur.Order.OrderID, Suc.Order.OrderID] - Cur.Order.ProcessTime);
            return LatestTime;
        }
    }
}
