//Implementation of heuristic operators. The functions of removal operators are implemented here. The functions of insertion operators are implemented in ALNSTPF.cs
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
    public class Heuristic
    {
        public string type;//type of the operator: removal or insertion
        public int Heur_Id;//the id of the operator
        public double weight;//the weight of the operator
        public double score;//the score of the operator
        public int CalledTimes;//the number of called times

        /// <summary>
        /// Remove orders from current solution. The orders that are to be removed are labeled first, and then are removed in the main algorithm ALNS/TPF
        /// </summary>
        /// <param name="Id">the id of the selected removal operator</param>
        /// <param name="TargetList">the current solutino</param>
        /// <param name="SizeOfBank">the number of removed orders</param>
        /// <param name="r">the random number</param>
        public void DestroyHeu(int Id, List<SelectedOrder> TargetList, int SizeOfBank, Random r)
        {
            if (SizeOfBank > TargetList.Count-2)
            {
                SizeOfBank = TargetList.Count - 2;
            }

            switch (Id)
            {
                case 0:
                    RemoveRandom(TargetList, SizeOfBank,r);
                    break;
                case 1:
                    RemoveRev(TargetList, SizeOfBank, r);
                    break;
                case 2:
                    RemoveUnitRev(TargetList, SizeOfBank, r);
                    break;
                case 3:
                    RemoveSetup(TargetList, SizeOfBank, r);
                    break;
                case 4:
                    RemoveWindowNum(TargetList, SizeOfBank, r);
                    break;
                case 5:
                    RemoveConflict(TargetList, SizeOfBank, r);
                    break;
                case 6:
                    RemoveRoute(TargetList, SizeOfBank, r);
                    break;
                case 7:
                    RemoveWait(TargetList, SizeOfBank, r);
                    break;
                case 8:
                    RemoveHis(TargetList, SizeOfBank, r);
                    break;
                case 9:
                    RemoveHis2(TargetList, SizeOfBank, r);
                    break;
            }
        }

        public void RemoveRandom(List<SelectedOrder> Initial, int Num, Random r)
        {
            int n = Initial.Count;

            List<SelectedOrder> tabuobs = new List<SelectedOrder>();
            int removenum = 0;
            int[] Random = new int[n];
            int[] index = new int[n];
            for (int j = 0; j < n; j++)
            {
                index[j] = j;
            }
            int site = n;
            int id;
            for (int j = 0; j < n; j++)
            {
                id = r.Next(0, site - 1);
                Random[j] = index[id];
                index[id] = index[site - 1];
                site--;
            }
            id = -1;
            
            for (int i = 0; i < Num; i++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                int tag = Random[id];
                obs = Initial[tag];

                if (obs.Order.RemoveTabu >= 0 || obs.Order.IsDummy)
                {
                    i--;
                    if (obs.Order.RemoveTabu >= 0)
                        tabuobs.Add(obs);
                }
                else
                {
                    removenum++;
                    obs.ToBeRemoved = true;
                }
                if (id == n - 1)
                    break;
            }
            for (int i = removenum; i < Num; i++)
                tabuobs[i - removenum].ToBeRemoved = true;
        }

        public void RemoveRev(List<SelectedOrder> Initial, int Num, Random r)
        {
            List<SelectedOrder> tabuobs = new List<SelectedOrder>();
            int removenum = 0;
            for (int inser = 0; inser < Initial.Count; inser++)
                Initial[inser].HeuristicInfo = Initial[inser].Order.Revenue * (1 + r.NextDouble());
            List<SelectedOrder> TempInitial = Initial.OrderBy(s => s.HeuristicInfo).ToList<SelectedOrder>();

            int id = -1;
            for (int j = 0; j < Num; j++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                obs = TempInitial[id];

                if (obs.Order.RemoveTabu >= 0 || obs.Order.IsDummy)
                {
                    j--;
                    if (obs.Order.RemoveTabu >= 0)
                        tabuobs.Add(obs);
                }
                else
                {
                    removenum++;
                    obs.ToBeRemoved = true;
                }
                if (id == TempInitial.Count - 1)
                    break;
            }
            for (int i = removenum; i < Num; i++)
                tabuobs[i - removenum].ToBeRemoved = true;
        }

        public void RemoveUnitRev(List<SelectedOrder> Initial, int Num, Random r)
        {
            List<SelectedOrder> tabuobs = new List<SelectedOrder>();
            int removenum = 0;

            for (int inser = 0; inser < Initial.Count; inser++)
                Initial[inser].HeuristicInfo = Initial[inser].Order.UnitRevenue * (1 + r.NextDouble());
            List<SelectedOrder> TempInitial = Initial.OrderBy(s => s.HeuristicInfo).ToList<SelectedOrder>();

            int id = -1;
            for (int j = 0; j < Num; j++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                obs = TempInitial[id];

                if (obs.Order.RemoveTabu >= 0 || obs.Order.IsDummy)
                {
                    j--;
                    if (obs.Order.RemoveTabu >= 0)
                        tabuobs.Add(obs);
                }
                else
                {
                    removenum++;
                    obs.ToBeRemoved = true;
                }
                if (id == TempInitial.Count - 1)
                    break;
            }
            for (int i = removenum; i < Num; i++)
                tabuobs[i - removenum].ToBeRemoved = true;
        }

        public void RemoveSetup(List<SelectedOrder> Initial, int Num, Random r)
        {
            List<SelectedOrder> tabuobs = new List<SelectedOrder>();
            int removenum = 0;

            for (int i = 1; i < Initial.Count - 1; i++)
                Initial[i].HeuristicInfo = (Initial[i].Setup + Initial[i + 1].Setup) *(1 + r.NextDouble());
            List<SelectedOrder> TempInitial = Initial.OrderByDescending(s => s.HeuristicInfo).ToList<SelectedOrder>();

            int id = -1;
            for (int j = 0; j < Num; j++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                obs = TempInitial[id];

                if (obs.Order.RemoveTabu >= 0 || obs.Order.IsDummy)
                {
                    j--;
                    if (obs.Order.RemoveTabu >= 0)
                        tabuobs.Add(obs);
                }
                else
                {
                    removenum++;
                    obs.ToBeRemoved = true;
                }
                if (id == TempInitial.Count - 1)
                    break;
            }
            for (int i = removenum; i < Num; i++)
                tabuobs[i - removenum].ToBeRemoved = true;
        }

        public void RemoveWindowNum(List<SelectedOrder> Initial, double Num, Random r)
        {
            for (int i = 1; i < Initial.Count - 1; i++)
                Initial[i].HeuristicInfo = (Initial[i].Order.FatherOrder.VTWList.Count) * (1 + r.NextDouble());
            List<SelectedOrder> TempInitial = Initial.OrderByDescending(s => s.HeuristicInfo).ToList<SelectedOrder>();
            List<SelectedOrder> tabuobs = new List<SelectedOrder>();
            int removenum = 0;
            int id = -1;
            for (int j = 0; j < Num; j++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                obs = TempInitial[id];
                if (obs.Order.RemoveTabu >= 0 || obs.Order.IsDummy)
                {
                    j--;
                    if (obs.Order.RemoveTabu >= 0)
                        tabuobs.Add(obs);
                }
                else
                {
                    removenum++;
                    obs.ToBeRemoved = true;
                }
                if (id == TempInitial.Count - 1)
                    break;
            }
            for (int i = removenum; i < Num; i++)
                tabuobs[i - removenum].ToBeRemoved = true;
        }

        public void RemoveConflict(List<SelectedOrder> Initial, double Num, Random r)
        {
            for (int inser = 0; inser < Initial.Count; inser++)
                Initial[inser].HeuristicInfo = Initial[inser].Order.Conflict * (1 + r.NextDouble());
            List<SelectedOrder> TempInitial = Initial.OrderByDescending(s => s.HeuristicInfo).ToList<SelectedOrder>();
            List<SelectedOrder> tabuobs = new List<SelectedOrder>();
            int removenum = 0;
            int id = -1;
            for (int j = 0; j < Num; j++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                obs = TempInitial[id];
                if (obs.Order.RemoveTabu >= 0 || obs.Order.IsDummy)
                {
                    j--;
                    if (obs.Order.RemoveTabu >= 0)
                        tabuobs.Add(obs);
                }
                else
                {
                    removenum++;
                    obs.ToBeRemoved = true;
                }
                if (id == TempInitial.Count - 1)
                    break;
            }
            for (int i = removenum; i < Num; i++)
                tabuobs[i - removenum].ToBeRemoved = true;
        }

        public void RemoveRoute(List<SelectedOrder> Initial, double Num, Random r)
        {
            double Lowest = double.MaxValue;
            int flag = -1;
            double CurrentFit = 0;
            for (int i = 1; i <= Num; i++)
                CurrentFit += Initial[i].Order.Revenue;
            double UnitFit = CurrentFit / (Initial[(int)Num].RealEndTime - Initial[1].RealStartTime);

            for (int i = 1; i < Initial.Count - Num; i++)
            {
                if (UnitFit < Lowest)
                {
                    Lowest = UnitFit;
                    flag = i;
                }
                CurrentFit -= Initial[i].Order.Revenue;
                CurrentFit += Initial[i + (int)Num].Order.Revenue;
                UnitFit = CurrentFit / (Initial[i + (int)Num].RealEndTime - Initial[i + 1].RealStartTime);
            }

            int id = flag - 1;
            for (int j = 0; j < Num; j++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                obs = Initial[id];
                if (obs.Order.IsDummy)
                    j--;
                else
                {
                    obs.ToBeRemoved = true;
                }
                if (id == Initial.Count - 1)
                    break;
            }
        }

        public void RemoveWait(List<SelectedOrder> Initial, double Num, Random r)
        {
            for (int i = 1; i < Initial.Count - 1; i++)
            {
                double preend = Initial[i - 1].RealStartTime + Initial[i - 1].Order.ProcessTime + Initial[i].Setup;
                Initial[i].Wait=Initial[i].RealStartTime - preend;
            }

            for (int inser = 0; inser < Initial.Count; inser++)
                Initial[inser].HeuristicInfo = Initial[inser].Wait * (1 + r.NextDouble());
            List<SelectedOrder> TempInitial = Initial.OrderByDescending(s => s.HeuristicInfo).ToList<SelectedOrder>();

            List<SelectedOrder> tabuobs = new List<SelectedOrder>();

            int removenum = 0;
            int id = -1;
            for (int j = 0; j < Num; j++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                obs = TempInitial[id];
                if (obs.Order.RemoveTabu >= 0 || obs.Order.IsDummy)
                {
                    j--;
                    if (obs.Order.RemoveTabu >= 0)
                        tabuobs.Add(obs);
                }
                else
                {
                    removenum++;
                    obs.ToBeRemoved = true;
                }
                if (id == TempInitial.Count - 1)
                    break;
            }
            for (int i = removenum; i < Num; i++)
                tabuobs[i - removenum].ToBeRemoved = true;
        }

        public void RemoveHis(List<SelectedOrder> Initial, double Num, Random r)
        {
            for (int i = 1; i < Initial.Count - 1; i++)
            {
                Initial[i].HeuristicInfo = (Initial[i + 1].Setup + Initial[i].Setup-Initial[i].Order.BestDis)*(1+r.NextDouble());
            }
            List<SelectedOrder> TempInitial = Initial.OrderByDescending(s => s.HeuristicInfo).ToList<SelectedOrder>();

            List<SelectedOrder> tabuobs = new List<SelectedOrder>();

            int removenum = 0;
            int id = -1;
            for (int j = 0; j < Num; j++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                obs = TempInitial[id];
                if (obs.Order.RemoveTabu >= 0 || obs.Order.IsDummy)
                {
                    j--;
                    if (obs.Order.RemoveTabu >= 0)
                        tabuobs.Add(obs);
                }
                else
                {
                    removenum++;
                    obs.ToBeRemoved = true;
                }
                if (id == TempInitial.Count - 1)
                    break;
            }
            for (int i = removenum; i < Num; i++)
                tabuobs[i - removenum].ToBeRemoved = true;
        }

        public void RemoveHis2(List<SelectedOrder> Initial, double Num, Random r)
        {
            for (int i = 1; i < Initial.Count - 1; i++)
            {
                Initial[i].HeuristicInfo = Initial[i].Order.BestUnitRev * (1 + r.NextDouble());
            }
            List<SelectedOrder> TempInitial = Initial.OrderBy(s => s.HeuristicInfo).ToList<SelectedOrder>();

            List<SelectedOrder> tabuobs = new List<SelectedOrder>();

            int removenum = 0;
            int id = -1;
            for (int j = 0; j < Num; j++)
            {
                id++;
                SelectedOrder obs = new SelectedOrder();
                obs = TempInitial[id];
                if (obs.Order.RemoveTabu >= 0 || obs.Order.IsDummy)
                {
                    j--;
                    if (obs.Order.RemoveTabu >= 0)
                        tabuobs.Add(obs);
                }
                else
                {
                    removenum++;
                    obs.ToBeRemoved = true;
                }
                if (id == TempInitial.Count - 1)
                    break;
            }
            for (int i = removenum; i < Num; i++)
                tabuobs[i - removenum].ToBeRemoved = true;
        }
    }
}
