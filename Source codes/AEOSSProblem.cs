//Definition of AEOSS problem
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
    class AEOSSProblem:problem
    {
        /// <summary>
        /// Initialization of the problem
        /// </summary>
        public void ProblemIntialize()
        {
            ProblemName = "AEOSS";
            IsCostLimit = false;
            IsMultiVTW = true;
            IsDueTime = false;
            IsIdleTime = false;
            IsFakeStart = false;
            IsInstantTabu = false;
            IsRemoveTabu = true;
            IsInsertTabu = true;
            DestroyList = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
            RepairList = new int[6] { 0, 1, 3, 4, 5, 6};
            MaxIteration = 10000;
        }

        /// <summary>
        /// Determine whether the problem should be terminated
        /// </summary>
        /// <param name="noimprove">the number of noimprove iterations</param>
        /// <param name="Gfit">current best revenue</param>
        /// <returns></returns>
        public override bool IsEnd(int noimprove, double Gfit)
        {
            if (Gfit == TotalRevenue||noimprove>1000)
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
            for (int i = 0; i < SuperOrderList.Count; i++)
            {
                if (SuperOrderList[i].VTWList.Count > 0)
                    TotalRevenue += SuperOrderList[i].Revenue;
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
            int size = 0;
            size = (int)Math.Ceiling(CurSolCount * 0.1);
            return size;
        }

        /// <summary>
        /// Read instances from files
        /// </summary>
        /// <param name="Distribution">distribution method: Area/World</param>
        /// <param name="SuperOrderNum">the number of tasks</param>
        public void LoadInstance(string Distribution, string SuperOrderNum)
        {
            ProblemIntialize();
            
            //Read tasks
            StreamReader TaskReader = new StreamReader(@"...\...\...\...\Satellite_Timedependent\instances\" + Distribution + "_Task_" + SuperOrderNum + ".txt");
            while (!TaskReader.EndOfStream)
            {
                SuperOrder newTask = new SuperOrder();
                string TaskInfo = TaskReader.ReadLine();
                if (TaskInfo != "2  45.0 45.0 0.00")
                {
                    string[] task = TaskInfo.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    newTask.SuperOrderId = int.Parse(task[0]);
                    newTask.Revenue = int.Parse(task[3]);
                    newTask.ProcessTime = int.Parse(task[4]);
                    newTask.IsProcessed = false;
                    SuperOrderList.Add(newTask);
                }
            }
            TaskReader.Close();

            //Read time windows
            int CountId = 0;
            StreamReader sr = new StreamReader(@"...\...\...\...\Satellite_Timedependent\instances\" + Distribution + "_TimeWindow_" + SuperOrderNum + ".txt");

            for (int mm = 0; mm < 1; mm++)
            {
                for (int i = 0; i < SuperOrderList.Count; i++)
                {
                    int windowCount = 0;

                    while (!sr.EndOfStream)
                    {
                        string WinInfo = sr.ReadLine();
                        string[] TimePiece = WinInfo.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (WinInfo == " ")
                            continue;

                        if (i == int.Parse(TimePiece[0]))
                        {
                            WinInfo = sr.ReadLine();
                            TimePiece = WinInfo.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        }

                        if (i == int.Parse(TimePiece[0]) - 1)
                        {
                            break;
                        }

                        Order tempTask = new Order();

                        tempTask.Revenue = SuperOrderList[i].Revenue;
                        tempTask.UnitRevenue = tempTask.Revenue * 1.0 / SuperOrderList[i].ProcessTime;
                        tempTask.ProcessTime = SuperOrderList[i].ProcessTime;
                        tempTask.FatherOrder = SuperOrderList[i];

                        windowCount++;
                        CountId++;

                        string StartPiece = null, EndPiece = null;
                        for (int k = 0; k < TimePiece.Length - 1; k++)
                        {
                            if (k <= 5)
                            {
                                StartPiece += TimePiece[k] + ' ';
                            }
                            else
                            {
                                EndPiece = EndPiece + TimePiece[k] + ' ';
                            }
                        }
                        tempTask.OrderID = CountId;
                        
                        tempTask.Release = double.Parse(TimePiece[3]) * 3600 + double.Parse(TimePiece[4]) * 60 + double.Parse(TimePiece[5]);
                        tempTask.Deadline = double.Parse(TimePiece[9]) * 3600 + double.Parse(TimePiece[10]) * 60 + double.Parse(TimePiece[11]);
                        
                        if(tempTask.Deadline-tempTask.Release<tempTask.ProcessTime)
                        {
                            tempTask.ProcessTime = tempTask.Deadline-tempTask.Release;
                            tempTask.FatherOrder.ProcessTime = tempTask.ProcessTime;
                        }
                        
                        tempTask.Deadline -= tempTask.ProcessTime;

                        tempTask.RollA = double.Parse(TimePiece[13]);
                        tempTask.RollB = double.Parse(TimePiece[14]);
                        tempTask.PitchA = double.Parse(TimePiece[15]);
                        tempTask.PitchB = double.Parse(TimePiece[16]);

                        OrderList.Add(tempTask);

                    }
                }
            }
            sr.Close();

            for (int i = 0; i < OrderList.Count; i++)
            {
                OrderList[i].FatherOrder.VTWList.Add(OrderList[i]);
            }

            for (int i = 0; i < SuperOrderList.Count;i++ )
            {
                if(SuperOrderList[i].VTWList.Count==0)
                {
                    SuperOrderList.Remove(SuperOrderList[i]);
                    i--;
                }
            }

            //Add dummy start and end nodes
            Order TempOrder = new Order();
            TempOrder.Release = 0;
            TempOrder.Deadline = 0;
            TempOrder.IsAccept = true;
            TempOrder.IsDummy = true;
            OrderList.Insert(0, TempOrder);
            Order TempOrderlast = new Order();
            TempOrderlast.Release = 86400;
            TempOrderlast.Deadline = 86400;
            TempOrderlast.IsAccept = true;
            TempOrderlast.IsDummy = true;
            OrderList.Add(TempOrderlast);

            Instance.OrderNum = OrderList.Count();

            GetConflictInfo();
            PreProcess();
        }

        /// <summary>
        /// Calculate the setup time (transition time)
        /// </summary>
        /// <param name="Pre">pre order</param>
        /// <param name="Cur">current order</param>
        /// <param name="preend">the end of pre order</param>
        /// <param name="folsta">the start of the current order</param>
        /// <returns></returns>
        public override double GetSetupTime(SelectedOrder Pre, SelectedOrder Cur, double preend, double folsta)
        {
            double Time = 0;
            preend -= Pre.Order.ProcessTime;
            double PreRoll = Pre.Order.RollA * preend + Pre.Order.RollB;
            double PrePitch = Pre.Order.PitchA * preend + Pre.Order.PitchB;
            double CurRoll = Cur.Order.RollA * folsta + Cur.Order.RollB;
            double CurPitch = Cur.Order.PitchA * folsta + Cur.Order.PitchB;
            double Total_differ = Math.Abs(PreRoll - CurRoll) + Math.Abs(PrePitch - CurPitch);

            if (Total_differ >= 90)
            {
                Time = Total_differ / 3 + 22;
                return Time;
            }
            if ((Total_differ >= 60) && (Total_differ < 90))
            {
                Time = Total_differ / 2.5 + 16;
                return Time;
            }
            if ((Total_differ >= 30) && (Total_differ < 60))
            {
                Time = Total_differ / 2 + 10;
                return Time;
            }
            if ((Total_differ > 10) && (Total_differ < 30))
            {
                Time = Total_differ / 1.5 + 5;
                return Time;
            }
            if (Total_differ <= 10)
            {
                Time = 35/3.0;
                return Time;
            }
            return Time;
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
            double PreRoll = Pre.Order.RollA * Pre.RealStartTime + Pre.Order.RollB;
            double PrePitch = Pre.Order.PitchA * Pre.RealStartTime + Pre.Order.PitchB;
            
            double TempTrans = 0;

            if(Pre.RealEndTime<Cur.Order.Release)
            {
                TempTrans = GetSetupTime(Pre, Cur, Pre.RealEndTime, Cur.Order.Release);
                if (Pre.RealEndTime + TempTrans <= Cur.Order.Release)
                    return Cur.Order.Release;
            }

            TempTrans = GetSetupTime(Pre, Cur, Pre.RealEndTime, Cur.Order.Deadline);
            if (Pre.RealEndTime + TempTrans > Cur.Order.Deadline)
            {
                if (Pre.RealEndTime + TempTrans - Cur.Order.Deadline < 0.001)
                    return Cur.Order.Deadline;
                return 86400;
            }
            else
            {
                EarliestTime = SolvePieceEquation(0, 0, 0, 10, 35 / 3.0, 35 / 3.0, Pre.RealEndTime, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
                EarliestTime = SolvePieceEquation(1.5, 5, 10, 30, 35 / 3.0, 25, Pre.RealEndTime, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
                EarliestTime = SolvePieceEquation(2, 10, 30, 60, 25, 40, Pre.RealEndTime, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
                EarliestTime = SolvePieceEquation(2.5, 16, 60, 90, 40, 52, Pre.RealEndTime, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
                EarliestTime = SolvePieceEquation(3, 22, 90, 10000, 52, 10000, Pre.RealEndTime, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
            }

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
            double PreRoll = Pre.Order.RollA * (PreTempEnd-Pre.Order.ProcessTime) + Pre.Order.RollB;
            double PrePitch = Pre.Order.PitchA * (PreTempEnd - Pre.Order.ProcessTime) + Pre.Order.PitchB;

            double TempTrans = 0;

            if (PreTempEnd < Cur.Order.Release)
            {
                TempTrans = GetSetupTime(Pre, Cur, PreTempEnd, Cur.Order.Release);
                if (PreTempEnd + TempTrans <= Cur.Order.Release)
                    return Cur.Order.Release;
            }

            TempTrans = GetSetupTime(Pre, Cur, PreTempEnd, Cur.Order.Deadline);
            if (PreTempEnd + TempTrans > Cur.Order.Deadline)
            {
                if (PreTempEnd + TempTrans - Cur.Order.Deadline < 0.001)
                    return Cur.Order.Deadline;
                return 86400;
            }
            else
            {
                EarliestTime = SolvePieceEquation(0, 0, 0, 10, 35 / 3.0, 35 / 3.0, PreTempEnd, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
                EarliestTime = SolvePieceEquation(1.5, 5, 10, 30, 35 / 3.0, 25, PreTempEnd, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
                EarliestTime = SolvePieceEquation(2, 10, 30, 60, 25, 40, PreTempEnd, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
                EarliestTime = SolvePieceEquation(2.5, 16, 60, 90, 40, 52, PreTempEnd, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
                EarliestTime = SolvePieceEquation(3, 22, 90, 10000, 52, 10000, PreTempEnd, 1, Cur, PreRoll, PrePitch);
                if (EarliestTime != -1)
                    return EarliestTime;
            }

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
            double SucRoll = Suc.Order.RollA * SucStart + Suc.Order.RollB;
            double SucPitch = Suc.Order.PitchA * SucStart + Suc.Order.PitchB;

            double TempTrans = 0; 

            if (Cur.Order.Deadline < SucStart)
            {
                TempTrans = GetSetupTime(Cur, Suc, Cur.Order.Deadline+Cur.Order.ProcessTime, SucStart);
                if (Cur.Order.Deadline + Cur.Order.ProcessTime + TempTrans <= SucStart)
                    return Cur.Order.Deadline;
            }
            TempTrans = GetSetupTime(Cur, Suc, Cur.Order.Release + Cur.Order.ProcessTime, SucStart);
            double t = Cur.Order.Release + Cur.Order.ProcessTime + TempTrans;
            if (Cur.Order.Release + Cur.Order.ProcessTime + TempTrans > SucStart)
            {
                if (Cur.Order.Release + Cur.Order.ProcessTime + TempTrans - SucStart < 0.01)
                    return Cur.Order.Release;
                return -1;
            }
            else
            {
                LatestTime = SolvePieceEquation(0, 0, 0, 10, 35 / 3.0, 35 / 3.0, SucStart - Cur.Order.ProcessTime, -1, Cur, SucRoll, SucPitch);
                if (LatestTime != -1)
                    return LatestTime;
                LatestTime = SolvePieceEquation(1.5, 5, 10, 30, 35 / 3.0, 25, SucStart - Cur.Order.ProcessTime, -1, Cur, SucRoll, SucPitch);
                if (LatestTime != -1)
                    return LatestTime;
                LatestTime = SolvePieceEquation(2, 10, 30, 60, 25, 40, SucStart - Cur.Order.ProcessTime, -1, Cur, SucRoll, SucPitch);
                if (LatestTime != -1)
                    return LatestTime;
                LatestTime = SolvePieceEquation(2.5, 16, 60, 90, 40, 52, SucStart - Cur.Order.ProcessTime, -1, Cur, SucRoll, SucPitch);
                if (LatestTime != -1)
                    return LatestTime;
                LatestTime = SolvePieceEquation(3, 22, 90, 10000, 52, 10000, SucStart - Cur.Order.ProcessTime, -1, Cur, SucRoll, SucPitch);
                if (LatestTime != -1)
                    return LatestTime;
            }

            return LatestTime;
        }

        /// <summary>
        /// Get the time according to angles
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="e"></param>
        /// <param name="f"></param>
        /// <param name="fixtime"></param>
        /// <param name="dir"></param>
        /// <param name="Cur"></param>
        /// <param name="PreRoll"></param>
        /// <param name="PrePitch"></param>
        /// <returns></returns>
        public double SolvePieceEquation(double a, double b, double c, double d, double e, double f, double fixtime, int dir, SelectedOrder Cur, double PreRoll, double PrePitch)
        {
            double EarliestTime = -1;
            double RollDiffer = 0;
            double PitchDiffer = 0;
            double TempSetup = 0;

            if(e==f)
            {
                EarliestTime = fixtime + e * dir;
                RollDiffer = EarliestTime * Cur.Order.RollA + Cur.Order.RollB - PreRoll;
                PitchDiffer = EarliestTime * Cur.Order.PitchA + Cur.Order.PitchB - PrePitch;
                TempSetup = dir * (EarliestTime - fixtime);
                if (Math.Abs(RollDiffer) + Math.Abs(PitchDiffer) > c && Math.Abs(RollDiffer) + Math.Abs(PitchDiffer) <= d)
                    return EarliestTime;
            }

            EarliestTime = (a * (-dir * fixtime - b) - Cur.Order.RollB - Cur.Order.PitchB + PreRoll + PrePitch) / (Cur.Order.RollA + Cur.Order.PitchA - dir * a);
            RollDiffer = EarliestTime * Cur.Order.RollA + Cur.Order.RollB - PreRoll;
            PitchDiffer = EarliestTime * Cur.Order.PitchA + Cur.Order.PitchB - PrePitch;
            TempSetup = dir * (EarliestTime - fixtime);
            if (RollDiffer >= 0 && PitchDiffer >= 0 && Math.Abs(RollDiffer) + Math.Abs(PitchDiffer) > c && Math.Abs(RollDiffer) + Math.Abs(PitchDiffer)<=d&&TempSetup>e&&TempSetup<=f)
                return EarliestTime;

            EarliestTime = (a * (-dir * fixtime - b) - Cur.Order.RollB + Cur.Order.PitchB + PreRoll - PrePitch) / (Cur.Order.RollA - Cur.Order.PitchA - dir * a);
            RollDiffer = EarliestTime * Cur.Order.RollA + Cur.Order.RollB - PreRoll;
            PitchDiffer = EarliestTime * Cur.Order.PitchA + Cur.Order.PitchB - PrePitch;
            TempSetup = dir * (EarliestTime - fixtime);
            if (RollDiffer >= 0 && PitchDiffer <= 0 && Math.Abs(RollDiffer) + Math.Abs(PitchDiffer) > c && Math.Abs(RollDiffer) + Math.Abs(PitchDiffer) <= d && TempSetup > e && TempSetup <= f)
                return EarliestTime;

            EarliestTime = (a * (-dir * fixtime - b) + Cur.Order.RollB - Cur.Order.PitchB - PreRoll + PrePitch) / (-Cur.Order.RollA + Cur.Order.PitchA - dir * a);
            RollDiffer = EarliestTime * Cur.Order.RollA + Cur.Order.RollB - PreRoll;
            PitchDiffer = EarliestTime * Cur.Order.PitchA + Cur.Order.PitchB - PrePitch;
            TempSetup = dir * (EarliestTime - fixtime);
            if (RollDiffer <= 0 && PitchDiffer >= 0 && Math.Abs(RollDiffer) + Math.Abs(PitchDiffer) > c && Math.Abs(RollDiffer) + Math.Abs(PitchDiffer) <= d && TempSetup > e && TempSetup <= f)
                return EarliestTime;

            EarliestTime = (a * (-dir * fixtime - b) + Cur.Order.RollB + Cur.Order.PitchB - PreRoll - PrePitch) / (-Cur.Order.RollA - Cur.Order.PitchA - dir * a);
            RollDiffer = EarliestTime * Cur.Order.RollA + Cur.Order.RollB - PreRoll;
            PitchDiffer = EarliestTime * Cur.Order.PitchA + Cur.Order.PitchB - PrePitch;
            TempSetup = dir * (EarliestTime - fixtime);
            if (RollDiffer <= 0 && PitchDiffer <= 0 && Math.Abs(RollDiffer) + Math.Abs(PitchDiffer) > c && Math.Abs(RollDiffer) + Math.Abs(PitchDiffer) <= d && TempSetup > e && TempSetup <= f)
                return EarliestTime;

            return -1;
        }
    }
}
