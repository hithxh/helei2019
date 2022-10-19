//The ALNS/TPF algorithm
//Authors: Lei He (l.he@tudelft.nl), Mathijs de Weerdt (M.M.deWeerdt@tudelft.nl), Neil Yorke-Smith (N.Yorke-Smith@tudelft.nl)
//Date: June 3, 2019
//License: CC-BY-NC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace GeneralTSALNS
{
    class ALNSTPF
    {
        /// <summary>
        /// The problem
        /// </summary>
        public problem Problem = new problem();
        /// <summary>
        /// A list of orders that are just removed/inserted 
        /// </summary>
        List<Order> JustRemove = new List<Order>(10);
        List<Order> JustInsert = new List<Order>(10);
        /// <summary>
        /// The list of removal and insertion operators
        /// </summary>
        public List<Heuristic> DestroyList = new List<Heuristic>();
        public List<Heuristic> InsertList = new List<Heuristic>();
        /// <summary>
        /// A random number
        /// </summary>
        public Random r = new Random(System.DateTime.Now.Millisecond);
        /// <summary>
        /// The length of tabu list [0,\sqrt(n/2)]
        /// </summary>
        int tabutime;

        /// <summary>
        /// The main ALNS/TPF algorithm. It returns the anytime revenue and running time. It also does the solution check: a negative value will be returned if the solution is infeasible
        /// </summary>
        /// <param name="time"></param>
        /// <param name="rev"></param>
        public void StartExecute(out List<double> time, out List<double> rev)
        {
            //Initialization of of the algorithm
            int InnerIteration = 50;
            int SigmaGlobal = 30;
            int SigmaLocal = 20;
            int SigmaBad = 10;
            double Temperature = 100;
            double ParaOfAnnealing = 0.99975;
            int SizeOfRequestBank = 1;
            double Lam = 0.5;
            int TotalCount = 0;
            int noimprove = 0;
            bool IsEnd = false;
            Heuristic CurDelete = new Heuristic();
            CurDelete.type = "Destroy";
            Heuristic CurInsert = new Heuristic();
            CurInsert.type = "Repair";
            int SelectDelNum, SelectInsNum;
            time = new List<double>(Problem.MaxIteration);
            rev = new List<double>(Problem.MaxIteration);
            tabutime = (int)Math.Sqrt(Problem.OrderList.Count / 2);
            InitialOperatorPool();
            List<SelectedOrder> CurrentSol = new List<SelectedOrder>(Problem.OrderList.Count);
            List<SelectedOrder> IniSol = new List<SelectedOrder>(Problem.OrderList.Count);
            List<SelectedOrder> GlobalOptimum = new List<SelectedOrder>(Problem.OrderList.Count);
            List<SelectedOrder> NewCurrent = new List<SelectedOrder>(Problem.OrderList.Count);
            List<SelectedOrder> BankList = new List<SelectedOrder>(Problem.OrderList.Count);
            string curlist = " ";
            //End of the initialization of of the algorithm


            Stopwatch st = new Stopwatch();
            st.Start();

            IniSol = InitialSolution();
            CopyListToList(IniSol, CurrentSol);
            CopyListToList(CurrentSol, GlobalOptimum);

            double C_fit = Problem.GetTotalProfit(@CurrentSol);
            double G_fit = C_fit;
            double NC_fit = 0;

            Temperature = -(0.05 / Math.Log(0.5) * C_fit);
            if (Temperature > 100)
                Temperature = 100;
            double initemperature = Temperature;

            for (int i = 0; i < Problem.MaxIteration / InnerIteration; i++)
            {
                for (int j = 0; j < InnerIteration; j++)
                {
                    TotalCount++;
                    noimprove++;
                    curlist = " ";
                    rev.Add(G_fit);
                    time.Add(st.Elapsed.TotalSeconds);
                    foreach (SelectedOrder a in CurrentSol)
                        curlist += ',' + a.Order.OrderID.ToString("D3");

                    if (Problem.IsFakeStart)
                        if (noimprove > 10000)
                        {
                            Temperature = initemperature;
                        }

                    SizeOfRequestBank = Problem.GetSizeOfRequestBank(SizeOfRequestBank, CurrentSol.Count, TotalCount);

                    ////***********Tabu search************//
                    for (int ii = 0; ii < Problem.OrderList.Count; ii++)
                    {
                        Problem.OrderList[ii].RemoveTabu--;
                        Problem.OrderList[ii].InsertTabu--;
                    }
                    JustRemove.Clear();
                    JustInsert.Clear();
                    ////***********Tabu search************//    


                    ////***********Remove orders************//
                    SelectDelNum = SelectOfHeuristic(DestroyList);
                    CurDelete = DestroyList[SelectDelNum];
                    CurDelete.CalledTimes++;

                    CurDelete.DestroyHeu(CurDelete.Heur_Id, CurrentSol, SizeOfRequestBank, r);

                    for (int ii = 0; ii < Problem.SelectedOrderList.Count; ii++)
                    {
                        Problem.SelectedOrderList[ii].Order.IsInNewCurrent = false;
                        Problem.SelectedOrderList[ii].Order.FatherOrder.IsInCompound = false;
                        Problem.SelectedOrderList[ii].Order.FatherOrder.IsProcessed = false;
                        Problem.SelectedOrderList[ii].Order.JustRemove = false;
                        Problem.SelectedOrderList[ii].Order.IsStartPoint = false;
                    }

                    List<SelectedOrder> StartPoint = CheckRemove(CurrentSol, NewCurrent);
                    for (int ii = 0; ii < NewCurrent.Count; ii++)
                    {
                        SelectedOrder tempCurrent = new SelectedOrder();
                        CloneOrder(NewCurrent[ii], tempCurrent);
                        NewCurrent[ii] = tempCurrent;
                        NewCurrent[ii].Order.IsInNewCurrent = true;
                        NewCurrent[ii].Order.FatherOrder.IsProcessed = true;
                        NewCurrent[ii].JustInsert = false;
                    }

                    if (NewCurrent.Count > 0)
                        UpdateSolution(ref NewCurrent);
                    ////***********Remove orders************//

                    ////***********Add unscheduled orders to banklist, and calculate the revenue in the list, the idle time of the solution************//
                    double CurrentRepairRevenue = 0;
                    for (int ii = 0; ii < NewCurrent.Count; ii++)
                        CurrentRepairRevenue += NewCurrent[ii].Order.Revenue;
                    double BankRevenue = 0;
                    BankList.Clear();
                    for (int ii = 0; ii < Problem.SelectedOrderList.Count; ii++)
                        if (!Problem.SelectedOrderList[ii].Order.IsInNewCurrent && !Problem.SelectedOrderList[ii].Order.FatherOrder.IsProcessed)
                        {
                            Problem.SelectedOrderList[ii].JustInsert = false;
                            BankList.Add(Problem.SelectedOrderList[ii]);
                            BankRevenue += Problem.SelectedOrderList[ii].Order.Revenue;
                        }
                    double IdleTime = Problem.OrderList[Problem.OrderList.Count - 1].Deadline - Problem.OrderList[0].Release;
                    for (int idle = 0; idle < NewCurrent.Count - 1; idle++)
                    {
                        IdleTime -= (NewCurrent[idle + 1].Setup + NewCurrent[idle + 1].Order.ProcessTime);
                    }
                    ////***********Add unscheduled orders to banklist, and calculate the revenue in the list, the idle time of the solution************//

                    ////***********Insert orders************//
                    SelectInsNum = SelectOfHeuristic(InsertList);
                    CurInsert = InsertList[SelectInsNum];
                    CurInsert.CalledTimes++;

                    switch (CurInsert.Heur_Id)
                    {
                        case 0:
                            for (int inser = 0; inser < BankList.Count; inser++)
                                BankList[inser].HeuristicInfo = BankList[inser].Order.Revenue * (1 + r.NextDouble());
                            BankList = BankList.OrderByDescending(s => s.HeuristicInfo).ToList<SelectedOrder>();
                            break;
                        case 1:
                            for (int inser = 0; inser < BankList.Count; inser++)
                                BankList[inser].HeuristicInfo = BankList[inser].Order.UnitRevenue * (1 + r.NextDouble());
                            BankList = BankList.OrderByDescending(s => s.HeuristicInfo).ToList<SelectedOrder>();
                            break;
                        case 2:
                            for (int inser = 0; inser < BankList.Count; inser++)
                                BankList[inser].HeuristicInfo = BankList[inser].Order.TotalSetup * (1 + r.NextDouble());
                            BankList = BankList.OrderBy(s => s.HeuristicInfo).ToList<SelectedOrder>();
                            break;
                        case 3:
                            for (int inser = 0; inser < BankList.Count; inser++)
                                BankList[inser].HeuristicInfo = BankList[inser].Order.FatherOrder.VTWList.Count * (1 + r.NextDouble());
                            BankList = BankList.OrderBy(s => s.HeuristicInfo).ToList<SelectedOrder>();
                            break;
                        case 4:
                            for (int inser = 0; inser < BankList.Count; inser++)
                                BankList[inser].HeuristicInfo = BankList[inser].Order.Conflict * (1 + r.NextDouble());
                            BankList = BankList.OrderBy(s => s.HeuristicInfo).ToList<SelectedOrder>();
                            break;
                        case 5:
                            for (int inser = 0; inser < BankList.Count; inser++)
                                BankList[inser].HeuristicInfo = BankList[inser].Order.BestUnitRev * (1 + r.NextDouble());
                            BankList = BankList.OrderByDescending(s => s.HeuristicInfo).ToList<SelectedOrder>();
                            break;
                        case 6:
                            for (int inser = 0; inser < BankList.Count; inser++)
                            {
                                double near = double.MaxValue;
                                for (int cur = 0; cur < NewCurrent.Count - 1; cur++)
                                {
                                    double setup = Problem.GetSetupTime(NewCurrent[cur], BankList[inser], NewCurrent[cur].RealEndTime, BankList[inser].RealStartTime);
                                    if (setup < near)
                                        near = setup;
                                }
                                BankList[inser].HeuristicInfo = near * (1 + r.NextDouble());
                            }
                            BankList = BankList.OrderBy(s => s.HeuristicInfo).ToList<SelectedOrder>();
                            break;
                    }

                    List<SelectedOrder> TempBankList = new List<SelectedOrder>();
                    for (int ins = 0; ins < BankList.Count; ins++)
                        if (BankList[ins].Order.InsertTabu >= 0)
                        {
                            TempBankList.Add(BankList[ins]);
                            BankList.Remove(BankList[ins]);
                            ins--;
                        }
                    for (int ins = 0; ins < TempBankList.Count; ins++)
                        BankList.Add(TempBankList[ins]);

                    bool jump = false;
                    SelectedOrder tempObs = new SelectedOrder();

                    for (int k = 0; k < BankList.Count; k++)
                    {
                        tempObs = BankList[k];
                        BankList.Remove(tempObs);
                        BankRevenue -= tempObs.Order.Revenue;
                        if (CurrentRepairRevenue + tempObs.Order.Revenue + BankRevenue < C_fit)
                        {
                            jump = true;
                            break;
                        }
                        if (IdleTime < tempObs.Order.ProcessTime || tempObs.Order.FatherOrder.IsProcessed)
                        {
                            if (IdleTime < Problem.ShortProcess)
                                break;
                            k = -1;
                            continue;
                        }
                        int IniLength = NewCurrent.Count;
                        SelectedOrder SelectedObs = new SelectedOrder();
                        CloneOrder(tempObs, SelectedObs);
                        double NewRev = 0;
                        double DeltaTrans = InsertObsIntoSolWithTabu(ref NewCurrent, SelectedObs, curlist, CurrentRepairRevenue, out NewRev);
                        if (NewCurrent.Count > IniLength)
                        {
                            SelectedObs.JustInsert = true;
                            SelectedObs.Order.IsInNewCurrent = true;
                            SelectedObs.Order.FatherOrder.IsProcessed = true;
                            IdleTime -= (SelectedObs.Order.ProcessTime + DeltaTrans);
                            CurrentRepairRevenue = NewRev;
                            JustInsert.Add(SelectedObs.Order);
                        }
                        k = -1;
                    }
                    ////***********Insert orders************//

                    ////***********Update removal tabu attribute if it jumps early************//
                    if (jump)
                    {
                        if (Problem.IsRemoveTabu)
                            for (int ii = 0; ii < JustRemove.Count; ii++)
                                JustRemove[ii].RemoveTabu = r.Next(tabutime);
                        continue;
                    }
                    ////***********Update removal tabu attribute if it jumps early************//

                    NC_fit = Problem.GetTotalProfit(NewCurrent);

                    for (int cur = 1; cur < NewCurrent.Count - 1; cur++)
                    {
                        if (NewCurrent[cur].Setup + NewCurrent[cur + 1].Setup < NewCurrent[cur].Order.BestDis)
                            NewCurrent[cur].Order.BestDis = NewCurrent[cur].Setup + NewCurrent[cur + 1].Setup;
                        double CurrentUnitRev = NewCurrent[cur].Order.Revenue / (NewCurrent[cur + 1].RealStartTime - NewCurrent[cur - 1].RealEndTime);
                        if (CurrentUnitRev > NewCurrent[cur].Order.BestUnitRev)
                            NewCurrent[cur].Order.BestUnitRev = CurrentUnitRev;
                    }

                    ////***********Partial sequence dominance************//
                    double Com_fit;
                    NewCurrent = GenerateCompoundSolution(CurrentSol, NewCurrent, NC_fit, out Com_fit);
                    NC_fit = Com_fit;
                    ////***********Partial sequence dominance************//

                    ////***********Update solution************//
                    bool IsAccept = false;
                    if ((NC_fit > G_fit) || (NC_fit == G_fit && NewCurrent[NewCurrent.Count - 2].RealEndTime < GlobalOptimum[GlobalOptimum.Count - 2].RealEndTime))
                    {
                        CopyListToList(NewCurrent, CurrentSol);
                        IsAccept = true;
                        C_fit = NC_fit;
                        noimprove = 0;
                        CopyListToList(NewCurrent, GlobalOptimum);
                        G_fit = NC_fit;
                        CurDelete.score += SigmaGlobal;
                        CurInsert.score += SigmaGlobal;
                    }
                    else
                    {
                        if (NC_fit >= C_fit)
                        {
                            CopyListToList(NewCurrent, CurrentSol);
                            IsAccept = true;
                            C_fit = NC_fit;
                            CurDelete.score += SigmaLocal;
                            CurInsert.score += SigmaLocal;
                        }
                        else
                        {
                            double pp = Math.Exp(100 / Temperature * ((NC_fit - C_fit) / C_fit));
                            double temp = r.NextDouble();
                            if (pp >= temp)
                            {
                                CopyListToList(NewCurrent, CurrentSol);
                                IsAccept = true;
                                C_fit = NC_fit;
                                CurDelete.score += SigmaBad;
                                CurInsert.score += SigmaBad;
                            }
                        }
                    }

                    if (IsAccept)
                    {
                        if (Problem.IsRemoveTabu)
                            for (int ii = 0; ii < JustInsert.Count; ii++)
                                JustInsert[ii].RemoveTabu = r.Next(tabutime);
                        if (Problem.IsInsertTabu)
                            for (int ii = 0; ii < JustRemove.Count; ii++)
                                JustRemove[ii].InsertTabu = r.Next(tabutime);
                    }
                    else
                    {
                        if (Problem.IsRemoveTabu)
                            for (int ii = 0; ii < JustRemove.Count; ii++)
                                JustRemove[ii].RemoveTabu = r.Next(tabutime);
                    }

                    Temperature = Temperature * ParaOfAnnealing;
                    ////***********Update solution************//

                    if (Problem.IsEnd(noimprove, G_fit))
                    {
                        IsEnd = true;
                        break;
                    }
                }

                if (IsEnd)
                    break;

                ////***********Update weights of operators************//
                UpdateHeuristicList(DestroyList, Lam);
                UpdateHeuristicList(InsertList, Lam);
                ////***********Update weights of operators************//
            }

            st.Stop();

            G_fit = Problem.GetTotalProfit(GlobalOptimum);
            rev.Add(G_fit);

            time.Add(st.Elapsed.TotalSeconds);

            bool Feasible = Problem.CheckSolution(GlobalOptimum);

            if (!Feasible)
                for (int a = 0; a < rev.Count; a++)
                    rev[a] = -1000000;
        }

        public void InitialOperatorPool()
        {
            for (int i = 0; i < Problem.DestroyList.Length; i++)
            {
                Heuristic a = new Heuristic();
                a.Heur_Id = Problem.DestroyList[i];
                decimal temp = (decimal)1 / Problem.DestroyList.Length; ;
                a.weight = (double)temp;
                a.type = "Destroy";
                a.score = 1;
                a.CalledTimes = 0;
                DestroyList.Add(a);
            }
            for (int i = 0; i < Problem.RepairList.Length; i++)
            {
                Heuristic a = new Heuristic();
                a.Heur_Id = Problem.RepairList[i];
                decimal temp = (decimal)1 / Problem.RepairList.Length; ;
                a.weight = (double)temp;
                a.type = "Repair";
                a.score = 1;
                a.CalledTimes = 0;
                InsertList.Add(a);
            }
        }

        /// <summary>
        /// Generalize the initial solution
        /// </summary>
        /// <returns></returns>
        public List<SelectedOrder> InitialSolution()
        {
            List<SelectedOrder> IniSolList = new List<SelectedOrder>(Problem.OrderList.Count);

            Problem.OrderList = Problem.OrderList.OrderBy(s => s.Release).ToList<Order>();

            Problem.SelectedOrderList = CreateSelectedOrderList(Problem.OrderList);

            IniSolList.Add(Problem.SelectedOrderList[0]);

            SelectedOrder curorder, preorder;

            for (int i = 1; i < Problem.OrderList.Count - 1; i++)
            {
                if (Problem.IsMultiVTW)
                    if (Problem.SelectedOrderList[i].Order.FatherOrder.IsProcessed)
                        continue;
                curorder = Problem.SelectedOrderList[i];
                preorder = IniSolList[IniSolList.Count - 1];

                double NeededTime = Problem.GetEarliestTime(preorder, curorder);

                if (NeededTime <= curorder.Order.Deadline)
                {
                    curorder.RealStartTime = NeededTime;
                    curorder.RealEndTime = NeededTime + curorder.Order.ProcessTime;
                    curorder.Setup = Problem.GetSetupTime(preorder, curorder, preorder.RealEndTime, curorder.RealStartTime);
                    curorder.Order.IsAccept = true;
                    IniSolList.Add(curorder);
                    if (Problem.IsMultiVTW)
                        curorder.Order.FatherOrder.IsProcessed = true;
                }
                else
                    curorder.Order.IsAccept = false;
            }

            while (true)
            {
                curorder = Problem.SelectedOrderList[Problem.SelectedOrderList.Count - 1];
                preorder = IniSolList[IniSolList.Count - 1];

                double Tran_time1 = Problem.GetSetupTime(preorder, curorder, preorder.RealEndTime, curorder.RealStartTime);
                if (preorder.RealEndTime + Tran_time1 > curorder.Order.Deadline)
                {
                    preorder.Order.IsAccept = false;
                    IniSolList.Remove(preorder);
                }
                else
                    break;
            }

            IniSolList.Add(Problem.SelectedOrderList[Problem.SelectedOrderList.Count - 1]);
            IniSolList = AddTimeSlack(IniSolList);
            return IniSolList;
        }

        /// <summary>
        /// Calculate the time slack and due time slack of an order
        /// </summary>
        /// <param name="ListWithoutTS">A solution with orders without TS and due TS</param>
        /// <returns></returns>
        public virtual List<SelectedOrder> AddTimeSlack(List<SelectedOrder> ListWithoutTS)
        {
            int i = ListWithoutTS.Count - 1;
            ListWithoutTS[i].TS = ListWithoutTS[i].Order.Deadline - ListWithoutTS[i].RealStartTime;
            if (Problem.IsDueTime)
                ListWithoutTS[i].DueTS = ListWithoutTS[i].Order.DueTime - ListWithoutTS[i].RealEndTime;
            double SucStart = ListWithoutTS[i].Order.Deadline;
            double SucDueStart = ListWithoutTS[i].Order.DueTime - ListWithoutTS[i].Order.ProcessTime;
            i--;
            while (i >= 0)
            {
                SucStart = Problem.GetLatestTime(SucStart, ListWithoutTS[i], ListWithoutTS[i + 1]);
                ListWithoutTS[i].TS = Math.Min(ListWithoutTS[i].Order.Deadline - ListWithoutTS[i].RealStartTime,
                    SucStart - ListWithoutTS[i].RealStartTime);

                if (Problem.IsDueTime)
                {
                    SucDueStart = Problem.GetDueLatestTime(SucDueStart, ListWithoutTS[i], ListWithoutTS[i + 1]);
                    ListWithoutTS[i].DueTS = Math.Min(ListWithoutTS[i].Order.DueTime - ListWithoutTS[i].RealEndTime,
                        SucDueStart - ListWithoutTS[i].RealStartTime);
                }
                i--;
            }
            return ListWithoutTS;
        }

        /// <summary>
        /// Generate a list of scheduled orders. Note that not all of these orders will be included in the solution
        /// </summary>
        /// <param name="ol"></param>
        /// <returns></returns>
        public List<SelectedOrder> CreateSelectedOrderList(List<Order> ol)
        {
            List<SelectedOrder> List = new List<SelectedOrder>(Problem.OrderList.Count);

            for (int i = 0; i < ol.Count; i++)
            {
                SelectedOrder Cur_obs = new SelectedOrder();
                Cur_obs.RealStartTime = ol[i].Release;
                Cur_obs.RealEndTime = Cur_obs.RealStartTime + ol[i].ProcessTime;
                Cur_obs.Order = ol[i];
                List.Add(Cur_obs);
            }
            return List;
        }

        /// <summary>
        /// Select an operator according to the weight
        /// </summary>
        /// <param name="HeurList"></param>
        /// <returns></returns>
        public int SelectOfHeuristic(List<Heuristic> HeurList)
        {
            int HeuId = 0;
            double CurValue = r.NextDouble();
            double SumWeight = 0;
            for (int i = 0; i < HeurList.Count; i++)
            {
                SumWeight = SumWeight + HeurList[i].weight;
                if (CurValue <= SumWeight)
                {
                    HeuId = i;
                    break;
                }
            }
            return HeuId;
        }

        /// <summary>
        /// Remove orders from the current solution and return the first order of every partial sequence for the PSD process
        /// </summary>
        /// <param name="cursol"></param>
        /// <param name="newsol"></param>
        /// <returns>the first order of every partial sequence</returns>
        public List<SelectedOrder> CheckRemove(List<SelectedOrder> cursol, List<SelectedOrder> newsol)
        {
            List<SelectedOrder> StartPoint = new List<SelectedOrder>();
            StartPoint.Add(cursol[0]);
            cursol[0].Order.IsStartPoint = true;
            bool flag = false;
            newsol.Clear();

            for (int i = 0; i < cursol.Count; i++)
            {
                if (!cursol[i].ToBeRemoved)
                {
                    newsol.Add(cursol[i]);
                    if (flag)
                    {
                        cursol[i].Order.IsStartPoint = true;
                        StartPoint.Add(cursol[i]);
                        flag = false;
                    }
                }
                else
                {
                    flag = true;
                    cursol[i].ToBeRemoved = false;
                    JustRemove.Add(cursol[i].Order);
                    cursol[i].Order.JustRemove = true;
                }
            }
            return StartPoint;
        }

        /// <summary>
        /// Deep copy an order
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public virtual void CloneOrder(SelectedOrder a, SelectedOrder b)
        {
            b.TS = a.TS;
            b.DueTS = a.DueTS;
            b.Order = a.Order;
            b.Setup = a.Setup;
            b.Profit = a.Profit;
            b.JustInsert = a.JustInsert;
            b.RealEndTime = a.RealEndTime;
            b.RealStartTime = a.RealStartTime;
        }

        public void CopyListToList(List<SelectedOrder> a, List<SelectedOrder> b)
        {
            if (b.Count != 0)
                b.Clear();
            for (int i = 0; i < a.Count; i++)
                b.Add(a[i]);
        }

        /// <summary>
        /// Update the solution to start each order as early as possible and update the corresponding time slacks
        /// </summary>
        /// <param name="NewObsList"></param>
        public virtual void UpdateSolution(ref List<SelectedOrder> NewObsList)
        {
            for (int i = 1; i < NewObsList.Count - 1; i++)
            {
                NewObsList[i].RealStartTime = Problem.GetEarliestTime(NewObsList[i - 1], NewObsList[i]);
                if (NewObsList[i].RealStartTime > NewObsList[i].Order.Deadline)
                {
                    NewObsList[i].Order.IsInNewCurrent = false;
                    if (NewObsList[i].Order.IsStartPoint)
                    {
                        NewObsList[i].Order.IsStartPoint = false;
                        NewObsList[i + 1].Order.IsStartPoint = true;
                    }
                    NewObsList.Remove(NewObsList[i]);
                    i--;
                    continue;
                }
                NewObsList[i].RealEndTime = NewObsList[i].RealStartTime + NewObsList[i].Order.ProcessTime;
                if (NewObsList[i].RealEndTime >= NewObsList[i + 1].Order.Deadline)
                {
                    NewObsList[i].Order.IsInNewCurrent = false;
                    if (NewObsList[i].Order.IsStartPoint)
                    {
                        NewObsList[i].Order.IsStartPoint = false;
                        NewObsList[i + 1].Order.IsStartPoint = true;
                    }
                    NewObsList.Remove(NewObsList[i]);
                    i--;
                    continue;
                }
                NewObsList[i].Setup = Problem.GetSetupTime(NewObsList[i - 1], NewObsList[i], NewObsList[i - 1].RealEndTime, NewObsList[i].RealStartTime);
            }
            while (true)
            {
                double LastEarliestTime = Problem.GetEarliestTime(NewObsList[NewObsList.Count - 2], NewObsList[NewObsList.Count - 1]);
                if (LastEarliestTime <= NewObsList[NewObsList.Count - 1].RealStartTime || NewObsList.Count == 2)
                    break;
                else
                {
                    NewObsList[NewObsList.Count - 2].Order.IsInNewCurrent = false;
                    if (NewObsList[NewObsList.Count - 2].Order.IsStartPoint)
                    {
                        NewObsList[NewObsList.Count - 2].Order.IsStartPoint = false;
                        NewObsList[NewObsList.Count - 1].Order.IsStartPoint = true;
                    }
                    NewObsList.Remove(NewObsList[NewObsList.Count - 2]);
                }
            }
            NewObsList[NewObsList.Count - 1].Setup = Problem.GetSetupTime(NewObsList[NewObsList.Count - 2], NewObsList[NewObsList.Count - 1], NewObsList[NewObsList.Count - 2].RealEndTime, NewObsList[NewObsList.Count - 1].RealStartTime);
            AddTimeSlack(NewObsList);
        }

        /// <summary>
        /// Return all the possible insertion positions of the candidate order
        /// </summary>
        /// <param name="OldSolution">Current solution</param>
        /// <param name="ObsInsert">Candidate order</param>
        /// <param name="Time">The start time</param>
        /// <returns>All possible positions</returns>
        public List<position> GetPositionListOfInsert(List<SelectedOrder> OldSolution, SelectedOrder ObsInsert, double Time, string CurList)
        {
            List<position> positionlist = new List<position>(1);
            double TimePoint = Time;
            double TimePointEnd = 0;
            double folTimePoint = 0;
            double smalltrans = double.MaxValue;
            string oldsollist = " ";

            if (Problem.IsInstantTabu && ObsInsert.Order.JustRemove)
            {
                for (int j = 0; j < OldSolution.Count; j++)
                    oldsollist += ',' + OldSolution[j].Order.OrderID.ToString("D3");
            }

            for (int i = 1; i < OldSolution.Count; i++)
            {
                if (OldSolution[i].Order.Deadline > Time)
                {
                    Time = OldSolution[i - 1].RealEndTime;
                    if (Time > ObsInsert.Order.Deadline)
                        break;

                    if (Problem.IsInstantTabu && ObsInsert.Order.JustRemove)
                    {
                        string newcur1 = oldsollist.Substring(0, 4 * i + 1);
                        newcur1 += ',' + ObsInsert.Order.OrderID.ToString("D3");
                        newcur1 += oldsollist.Remove(0, 4 * i + 1);
                        if (newcur1 == CurList)
                            continue;
                    }

                    TimePoint = Problem.GetEarliestTime(OldSolution[i - 1], ObsInsert);
                    TimePointEnd = TimePoint + ObsInsert.Order.ProcessTime;
                    if (TimePointEnd > OldSolution[i].Order.Deadline||TimePoint > ObsInsert.Order.Deadline)
                        continue;
                    folTimePoint = Problem.GetTempEarliestTime(TimePointEnd, ObsInsert, OldSolution[i]);

                    if (folTimePoint - OldSolution[i].RealStartTime > OldSolution[i].TS)//Time slack is not enough
                        continue;

                    position tempposi = new position();
                    tempposi.posi = i;

                    tempposi.trans = (Problem.GetSetupTime(ObsInsert, OldSolution[i], TimePointEnd, folTimePoint)
                     + Problem.GetSetupTime(OldSolution[i - 1], ObsInsert, OldSolution[i - 1].RealEndTime, TimePoint)
                     - Problem.GetSetupTime(OldSolution[i - 1], OldSolution[i], OldSolution[i - 1].RealEndTime, OldSolution[i].RealStartTime));

                    tempposi.heuristic = tempposi.trans;

                    if (Problem.IsDueTime)
                        if ((folTimePoint - OldSolution[i].RealStartTime > OldSolution[i].DueTS && i != OldSolution.Count - 1) || TimePoint + ObsInsert.Order.ProcessTime > ObsInsert.Order.DueTime)
                        {
                            tempposi.heuristic = double.MaxValue;
                        }

                    if (tempposi.heuristic < double.MaxValue && tempposi.heuristic < smalltrans)
                    {
                        smalltrans = tempposi.heuristic;
                        positionlist.Clear();
                        positionlist.Add(tempposi);
                    }
                    if (tempposi.heuristic == double.MaxValue && smalltrans == double.MaxValue)
                        positionlist.Add(tempposi);

                }
            }
                
            return positionlist;
        }

        /// <summary>
        /// Insert an order to the solution
        /// </summary>
        /// <param name="OldSolution"></param>
        /// <param name="ObsInsert"></param>
        /// <param name="CurList">the list of current solution, for the instant tabu</param>
        /// <param name="CurRev"></param>
        /// <param name="NewRev"></param>
        /// <returns></returns>
        public double InsertObsIntoSolWithTabu(ref List<SelectedOrder> OldSolution, SelectedOrder ObsInsert, string CurList, double CurRev, out double NewRev)
        {
            List<position> positionlist = GetPositionListOfInsert(OldSolution, ObsInsert, ObsInsert.Order.Release,CurList);
            NewRev = CurRev;

            if (positionlist.Count == 0)
                return 0;

            double DeltaTrans = 0;
            int k = -100;
            double OldStart = 0;
            double OldSetup = 0;

            if (positionlist[0].heuristic < double.MaxValue) //The insertion will not reduce the profit of other orders
            {
                k = positionlist[0].posi;
                OldSetup = OldSolution[k].Setup;
                OldSolution.Insert(k, ObsInsert);

                for (int j = k; j < OldSolution.Count; j++)
                {
                    OldStart = OldSolution[j].RealStartTime;
                    OldSolution[j].RealStartTime = Problem.GetEarliestTime(OldSolution[j - 1], OldSolution[j]);
                    OldSolution[j].Setup = Problem.GetSetupTime(OldSolution[j - 1], OldSolution[j], OldSolution[j - 1].RealEndTime, OldSolution[j].RealStartTime);
                    if (OldSolution[j].RealStartTime == OldStart && j != k)
                        break;
                    OldSolution[j].TS -= (OldSolution[j].RealStartTime - OldStart);
                    OldSolution[j].DueTS -= (OldSolution[j].RealStartTime - OldStart);
                    OldSolution[j].RealEndTime = OldSolution[j].RealStartTime + OldSolution[j].Order.ProcessTime;
                }

                int flag = k;
                double SucStart = OldSolution[flag + 1].RealStartTime + OldSolution[flag + 1].TS;
                double SucDueStart = OldSolution[flag + 1].RealStartTime + OldSolution[flag + 1].DueTS;
                while (flag > 0)
                {
                    SucStart = Problem.GetLatestTime(SucStart, OldSolution[flag], OldSolution[flag + 1]);
                    double oldts = OldSolution[flag].TS;
                    double olddts = OldSolution[flag].DueTS;
                    OldSolution[flag].TS = Math.Min(OldSolution[flag].Order.Deadline - OldSolution[flag].RealStartTime,
                        SucStart - OldSolution[flag].RealStartTime);
                    if (Problem.IsDueTime)
                    {
                        SucDueStart = Problem.GetDueLatestTime(SucDueStart, OldSolution[flag], OldSolution[flag + 1]);
                        OldSolution[flag].DueTS = Math.Min(OldSolution[flag].Order.DueTime - OldSolution[flag].RealEndTime,
                            SucDueStart - OldSolution[flag].RealStartTime);
                    }
                    if (oldts == OldSolution[flag].TS && flag != k && olddts == OldSolution[flag].DueTS)
                    {
                        break;
                    }
                    flag--;
                }

                DeltaTrans = OldSolution[k].Setup + OldSolution[k + 1].Setup - OldSetup;
                NewRev += ObsInsert.Order.Revenue;
            }
            else //The insertion will reduce the profit of other orders because of the due time
            {
                double BestRev = CurRev;
                double BestDeltaTrans = 0;
                List<SelectedOrder> BestTempSol = new List<SelectedOrder>();
                for (int i = 0; i < positionlist.Count;i++ )
                {
                    k = positionlist[i].posi;
                    OldSetup = OldSolution[k].Setup;

                    List<SelectedOrder> TempSol = new List<SelectedOrder>();
                    CloneSol(OldSolution, TempSol);
                    TempSol.Insert(k, ObsInsert);
                    for (int j = k; j < TempSol.Count; j++)
                    {
                        OldStart = TempSol[j].RealStartTime;
                        TempSol[j].RealStartTime = Problem.GetEarliestTime(TempSol[j - 1], TempSol[j]);
                        TempSol[j].Setup = Problem.GetSetupTime(TempSol[j - 1], TempSol[j], TempSol[j - 1].RealEndTime, TempSol[j].RealStartTime);
                        if (TempSol[j].RealStartTime == OldStart && j != k)
                            break;
                        TempSol[j].RealEndTime = TempSol[j].RealStartTime + TempSol[j].Order.ProcessTime;
                    }
                    double TempFit = Problem.GetTotalProfit(TempSol);
                    if (TempFit > BestRev)
                    {
                        CloneSol(TempSol, BestTempSol);
                        BestRev = TempFit;
                        BestDeltaTrans = TempSol[k].Setup + TempSol[k + 1].Setup - OldSetup;
                    }
                }
                if (BestRev > CurRev)
                {
                    OldSolution = BestTempSol;
                    NewRev = BestRev;
                    AddTimeSlack(OldSolution);
                    DeltaTrans = BestDeltaTrans;
                }
            }
            return DeltaTrans;    
        }

        /// <summary>
        /// Deep copy of a solution
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        public void CloneSol(List<SelectedOrder> A, List<SelectedOrder> B)
        {
            B.Clear();
            foreach (SelectedOrder a in A)
            {
                SelectedOrder temp = new SelectedOrder();
                temp.DueTS = a.DueTS;
                temp.Order = a.Order;
                temp.Profit = a.Profit;
                temp.RealEndTime = a.RealEndTime;
                temp.RealStartTime = a.RealStartTime;
                temp.Setup = a.Setup;
                temp.TS = a.TS;
                temp.JustInsert = a.JustInsert;
                B.Add(temp);
            }
        }

        /// <summary>
        /// Generate the compound solution, and if it is better than the new solution but not the same as the current solution, it is used as the new solution
        /// </summary>
        /// <param name="CurSol"></param>
        /// <param name="NewSol"></param>
        /// <param name="Nfit"></param>
        /// <param name="ComRev"></param>
        /// <returns></returns>
        public List<SelectedOrder> GenerateCompoundSolution(List<SelectedOrder> CurSol, List<SelectedOrder> NewSol, double Nfit, out double ComRev)
        {
            List<SelectedOrder> ComSol = new List<SelectedOrder>();
            List<SmallPart> SmallPartList = new List<SmallPart>();
            List<SmallPart> NewSmallPartList = new List<SmallPart>();
            SmallPart sp = new SmallPart();
            SmallPart nsp = new SmallPart();
            SmallPartList.Add(sp);
            NewSmallPartList.Add(nsp);

            int flag = 0;

            while (flag < CurSol.Count)
            {
                SmallPartList[SmallPartList.Count - 1].list.Add(CurSol[flag]);
                SmallPartList[SmallPartList.Count - 1].Revenue += CurSol[flag].Order.Revenue;
                if (flag + 1 < CurSol.Count)
                {
                    if (CurSol[flag + 1].Order.IsStartPoint)
                    {
                        SmallPartList[SmallPartList.Count - 1].UnitRevenue =
                            SmallPartList[SmallPartList.Count - 1].Revenue /
                            (SmallPartList[SmallPartList.Count - 1].list[SmallPartList[SmallPartList.Count - 1].list.Count - 1].RealEndTime
                            - SmallPartList[SmallPartList.Count - 1].list[0].RealStartTime);
                        SmallPart sp1 = new SmallPart();
                        SmallPartList.Add(sp1);
                    }
                }
                flag++;
            }

            while (flag < CurSol.Count)
            {
                SmallPartList[SmallPartList.Count - 1].list.Add(CurSol[flag]);
                SmallPartList[SmallPartList.Count - 1].Revenue += CurSol[flag].Order.Revenue;
                flag++;
            }
            SmallPartList[SmallPartList.Count - 1].UnitRevenue =
                                SmallPartList[SmallPartList.Count - 1].Revenue /
                                (SmallPartList[SmallPartList.Count - 1].list[SmallPartList[SmallPartList.Count - 1].list.Count - 1].RealEndTime
                                - SmallPartList[SmallPartList.Count - 1].list[0].RealStartTime);

            flag = 0;

            while (flag < NewSol.Count)
            {
                NewSmallPartList[NewSmallPartList.Count - 1].list.Add(NewSol[flag]);
                NewSmallPartList[NewSmallPartList.Count - 1].Revenue += NewSol[flag].Order.Revenue;
                if (flag + 1 < NewSol.Count)
                {
                    if (NewSol[flag + 1].Order.IsStartPoint)
                    {
                        NewSmallPartList[NewSmallPartList.Count - 1].UnitRevenue =
                            NewSmallPartList[NewSmallPartList.Count - 1].Revenue /
                            (NewSmallPartList[NewSmallPartList.Count - 1].list[NewSmallPartList[NewSmallPartList.Count - 1].list.Count - 1].RealEndTime
                            - NewSmallPartList[NewSmallPartList.Count - 1].list[0].RealStartTime);
                        SmallPart sp1 = new SmallPart();
                        NewSmallPartList.Add(sp1);
                    }
                }
                flag++;
            }

            while (flag < NewSol.Count)
            {
                NewSmallPartList[NewSmallPartList.Count - 1].list.Add(NewSol[flag]);
                NewSmallPartList[NewSmallPartList.Count - 1].Revenue += NewSol[flag].Order.Revenue;
                flag++;
            }
            NewSmallPartList[NewSmallPartList.Count - 1].UnitRevenue =
                                NewSmallPartList[NewSmallPartList.Count - 1].Revenue /
                                (NewSmallPartList[NewSmallPartList.Count - 1].list[NewSmallPartList[NewSmallPartList.Count - 1].list.Count - 1].RealEndTime
                                - NewSmallPartList[NewSmallPartList.Count - 1].list[0].RealStartTime);

            int count = 0;

            while (count < SmallPartList.Count)
            {
                if (NewSmallPartList[count].UnitRevenue >= SmallPartList[count].UnitRevenue)
                {
                    for (int j = 0; j < NewSmallPartList[count].list.Count; j++)
                    {
                        if (!NewSmallPartList[count].list[j].Order.FatherOrder.IsInCompound)
                        {
                            NewSmallPartList[count].list[j].Order.FatherOrder.IsInCompound = true;
                            SelectedOrder temp = new SelectedOrder();
                            CloneOrder(NewSmallPartList[count].list[j], temp);
                            ComSol.Add(temp);
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < SmallPartList[count].list.Count; j++)
                    {
                        if (!SmallPartList[count].list[j].Order.FatherOrder.IsInCompound)
                        {
                            SmallPartList[count].list[j].Order.FatherOrder.IsInCompound = true;
                            SelectedOrder temp = new SelectedOrder();
                            CloneOrder(SmallPartList[count].list[j], temp);
                            ComSol.Add(temp);
                        }
                    }
                }
                count++;
            }

            if (ComSol.Count == CurSol.Count)
            {
                bool IsCurrentSol = true;
                for (int i = 0; i < ComSol.Count; i++)
                    if (ComSol[i].Order.OrderID != CurSol[i].Order.OrderID)
                    {
                        IsCurrentSol = false;
                        break;
                    }
                if (IsCurrentSol)
                {
                    ComRev = Nfit;
                    return NewSol;
                }
            }

            UpdateSolution(ref ComSol);
            ComRev = Problem.GetTotalProfit(ComSol);

            if (ComRev > Nfit)
            {
                return ComSol;
            }
            else
            {
                ComRev = Nfit;
                return NewSol;
            }
        }

        /// <summary>
        /// Update weights of operators
        /// </summary>
        /// <param name="NameOfList"></param>
        /// <param name="Lam"></param>
        public void UpdateHeuristicList(List<Heuristic> NameOfList, double Lam)
        {
            double total = 0;
            int M = NameOfList.Count;
 
            for (int i = 0; i < NameOfList.Count; i++)
            {
                if (NameOfList[i].CalledTimes != 0)
                    NameOfList[i].score = 1 + (NameOfList[i].score - 1) / NameOfList[i].CalledTimes;
                else
                    NameOfList[i].score = 1;
                total += NameOfList[i].score;
                NameOfList[i].CalledTimes = 0;
            }
            for (int i = 0; i < NameOfList.Count; i++)
            {
                double temp = NameOfList[i].score / total;
                NameOfList[i].weight = NameOfList[i].weight * Lam + (1 - Lam) * temp;
                NameOfList[i].score = 1;
            }
        }

    }
}