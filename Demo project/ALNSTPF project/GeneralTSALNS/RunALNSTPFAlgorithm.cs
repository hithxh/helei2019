//A demo to run the algorithm
//Authors: Lei He (l.he@tudelft.nl), Mathijs de Weerdt (M.M.deWeerdt@tudelft.nl), Neil Yorke-Smith (N.Yorke-Smith@tudelft.nl)
//Date: June 3, 2019
//License: CC-BY-NC

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace GeneralTSALNS
{
    public partial class RunALNSTPFAlgorithm : Form
    {
        public RunALNSTPFAlgorithm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Run each instance for 10 times and store the revenue of the solution. The result of every 1/10 iterations is also stored.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OAS_Click(object sender, EventArgs e)
        {
            #region OAS
            string profitinfo;
            StreamReader or1 = new StreamReader(@"...\...\...\...\OAS\OAS_TS_Solutions_UBs.txt");
            List<double> BKUB = new List<double>();
            or1.ReadLine();
            int count = 0;
            string instanceinfo = " ";
            while (!or1.EndOfStream)
            {
                if (count == 0)
                    instanceinfo = or1.ReadLine();
                profitinfo = or1.ReadLine();
                string[] pi = profitinfo.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                BKUB.Add(double.Parse(pi[1]));
                count++;

                if (count == 10)
                    count = 0;
            }
            or1.Close();
            string nowtime = System.DateTime.Now.ToString("yy-MM-dd HH-mm-ss");


            double GapBKUB = 0;

            int runs = 10;
            int instance = 0;
            int[] ordernum = new int[6] { 10, 15, 20, 25, 50, 100 };
            int[] tau = new int[5] { 1, 3, 5, 7, 9 };
            int[] r = new int[5] { 1, 3, 5, 7, 9 };
            for (int orderno = 3; orderno < 6; orderno++)
                for (int tauno = 0; tauno < 5; tauno++)
                    for (int rno = 0; rno < 5; rno++)
                    {
                        int temp = orderno * 5 * 5 * 10 + tauno * 5 * 10 + rno * 10;
                        int maxite = ordernum[orderno] * 1000;

                        List<Solution> GapList = new List<Solution>();
                        List<Solution> TimeList = new List<Solution>();

                        for (int i = 1; i <= 10; i++)
                        {
                            for (int j = 0; j < runs; j++)
                            {
                                instance = orderno * 5 * 5 * 10 + tauno * 5 * 10 + rno * 10 + i - 1;

                                ALNSTPF newSolver = new ALNSTPF();
                                OASProblem ProblemToSolve = new OASProblem();
                                ProblemToSolve.LoadInstance(ordernum[orderno].ToString(), tau[tauno].ToString(), r[rno].ToString(), i.ToString());

                                newSolver.Problem = ProblemToSolve;
                                List<double> time = new List<double>();
                                List<double> rev = new List<double>();
                                newSolver.StartExecute(out time, out rev);

                                while (rev.Count <= ProblemToSolve.MaxIteration)
                                {
                                    rev.Add(rev[rev.Count - 1]);
                                    time.Add(time[time.Count - 1]);
                                }

                                List<double> revsl = new List<double>();
                                List<double> timesl = new List<double>();
                                for (int k = 0; k < 10; k++)
                                {
                                    double tempt, tempr;
                                    tempr = (BKUB[instance] * 1.0 - rev[(k + 1) * maxite / 10]) / BKUB[instance];
                                    tempt = time[(k + 1) * maxite / 10];
                                    revsl.Add(tempr);
                                    timesl.Add(tempt);
                                }
                                Solution revlist = new Solution();
                                revlist.sl = revsl;
                                Solution timelist = new Solution();
                                timelist.sl = timesl;

                                GapList.Add(revlist);
                                TimeList.Add(timelist);

                            }
                        }

                        StreamWriter solwriter = new StreamWriter(@"...\...\...\...\OAS\" + ordernum[orderno].ToString() + "orders" + tau[tauno].ToString() + "tao" + r[rno].ToString() + "r_10runs.csv");

                        for (int i = 0; i < runs * 10; i++)
                        {
                            if (i % runs == 0)
                                solwriter.WriteLine(ordernum[orderno].ToString() + "orders" + tau[tauno].ToString() + "tao" + r[rno].ToString() + "r_" + i / runs);
                            solwriter.WriteLine(TimeList[i].sl[0].ToString("0.000000") + ',' + GapList[i].sl[0].ToString("0.000000") + ',' +
                                TimeList[i].sl[1].ToString("0.000000") + ',' + GapList[i].sl[1].ToString("0.000000") + ',' +
                                TimeList[i].sl[2].ToString("0.000000") + ',' + GapList[i].sl[2].ToString("0.000000") + ',' +
                                TimeList[i].sl[3].ToString("0.000000") + ',' + GapList[i].sl[3].ToString("0.000000") + ',' +
                                TimeList[i].sl[4].ToString("0.000000") + ',' + GapList[i].sl[4].ToString("0.000000") + ',' +
                                TimeList[i].sl[5].ToString("0.000000") + ',' + GapList[i].sl[5].ToString("0.000000") + ',' +
                                TimeList[i].sl[6].ToString("0.000000") + ',' + GapList[i].sl[6].ToString("0.000000") + ',' +
                                TimeList[i].sl[7].ToString("0.000000") + ',' + GapList[i].sl[7].ToString("0.000000") + ',' +
                                TimeList[i].sl[8].ToString("0.000000") + ',' + GapList[i].sl[8].ToString("0.000000") + ',' +
                                TimeList[i].sl[9].ToString("0.000000") + ',' + GapList[i].sl[9].ToString("0.000000"));
                        }

                        for (int it = 9; it < 10; it++)
                        {
                            StreamWriter stat = new StreamWriter(@"...\...\...\...\OAS\" + nowtime + "_test.csv", true);

                            double bkubbest = 1, tbest = 100000, gap = 0, solnum = 0;
                            List<double> timebest = new List<double>();
                            List<double> gapbest = new List<double>();
                            List<double> gapavg = new List<double>();

                            for (int i = 0; i < runs * 10; i++)
                            {
                                if (TimeList[i].sl[it] < tbest)
                                    tbest = TimeList[i].sl[it];
                                if (GapList[i].sl[it] < bkubbest)
                                    bkubbest = GapList[i].sl[it];
                                gap += GapList[i].sl[it];
                                if (i % runs == runs - 1)
                                {
                                    gap /= runs;
                                    gapavg.Add(gap);
                                    gap = 0;
                                    timebest.Add(tbest);
                                    gapbest.Add(bkubbest);
                                    bkubbest = 1; tbest = 100000;
                                }
                            }

                            solwriter.WriteLine(it.ToString() + ',' + ordernum[orderno].ToString() + "orders" + tau[tauno].ToString() + "tao" + r[rno].ToString() + "r," + gapavg.Min().ToString("0.000000") + ',' + gapavg.Average().ToString("0.000000") + ',' + gapavg.Max().ToString("0.000000") + ',' +
                                        gapbest.Min().ToString("0.000000") + ',' + gapbest.Average().ToString("0.000000") + ',' + gapbest.Max().ToString("0.000000") + ',' + timebest.Average().ToString("0.000000"));
                            stat.WriteLine(it.ToString() + ',' + ordernum[orderno].ToString() + "orders" + tau[tauno].ToString() + "tao" + r[rno].ToString() + "r," + gapavg.Min().ToString("0.000000") + ',' + gapavg.Average().ToString("0.000000") + ',' + gapavg.Max().ToString("0.000000") + ',' +
                                        gapbest.Min().ToString("0.000000") + ',' + gapbest.Average().ToString("0.000000") + ',' + gapbest.Max().ToString("0.000000") + ',' + timebest.Average().ToString("0.000000"));

                            stat.Close();
                        }
                        solwriter.Close();
                    }
            #endregion
        }

        /// <summary>
        /// Run each instance for 10 times and store the revenue of the solution. The result of every 1/10 iterations is also stored.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AEOSS_Click(object sender, EventArgs e)
        {
            #region AEOSS
            string nowtime = System.DateTime.Now.ToString("yy-MM-dd HH-mm-ss");
            string[] dis = new string[2] { "World", "Area" };
            int[] WorldNum = new int[12] { 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600 };
            int[] AreaNum = new int[15] { 50, 75, 100, 125, 150, 175, 200, 225, 250, 275, 300, 325, 350, 375, 400 };

            for (int i = 0; i < 2; i++)
            {
                int insnum = 0;
                if (i == 0)
                    insnum = 12;
                else
                    insnum = 15;
                for (int j = 0; j < insnum; j++)
                {
                    int task = 0;
                    if (i == 0)
                        task = WorldNum[j];
                    else
                        task = AreaNum[j];
                    int runs = 10;

                    StreamWriter solwriter = new StreamWriter(@"...\...\...\...\AEOSS\" + dis[i] + "_" + task.ToString() + "tasks_10runs.csv");
                    List<Solution> GapList = new List<Solution>();
                    List<Solution> TimeList = new List<Solution>();

                    for (int k = 0; k < runs; k++)
                    {
                        ALNSTPF newSolver = new ALNSTPF();
                        AEOSSProblem ProblemToSolve = new AEOSSProblem();
                        ProblemToSolve.LoadInstance(dis[i], task.ToString());
                        List<double> time = new List<double>();
                        List<double> rev = new List<double>();
                        newSolver.Problem = ProblemToSolve;
                        newSolver.StartExecute(out time, out rev);

                        for (int re = 0; re < rev.Count; re++)
                            rev[re] /= ProblemToSolve.TotalRevenue;

                        List<double> revsl = new List<double>();
                        List<double> timesl = new List<double>();

                        while (rev.Count < 10001)
                        {
                            rev.Add(rev[rev.Count - 1]);
                            time.Add(time[time.Count - 1]);
                        }

                        for (int l = 0; l < 10; l++)
                        {
                            double tempt, tempr;
                            tempr = rev[(l + 1) * 1000];
                            tempt = time[(l + 1) * 1000];
                            revsl.Add(tempr);
                            timesl.Add(tempt);
                        }

                        Solution revlist = new Solution();
                        revlist.sl = revsl;
                        Solution timelist = new Solution();
                        timelist.sl = timesl;

                        GapList.Add(revlist);
                        TimeList.Add(timelist);
                    }

                    for (int k = 0; k < runs; k++)
                    {
                        solwriter.WriteLine(TimeList[k].sl[0].ToString("0.00000000") + ',' + GapList[k].sl[0].ToString("0.00000000") + ',' +
                            TimeList[k].sl[1].ToString("0.00000000") + ',' + GapList[k].sl[1].ToString("0.00000000") + ',' +
                            TimeList[k].sl[2].ToString("0.00000000") + ',' + GapList[k].sl[2].ToString("0.00000000") + ',' +
                            TimeList[k].sl[3].ToString("0.00000000") + ',' + GapList[k].sl[3].ToString("0.00000000") + ',' +
                            TimeList[k].sl[4].ToString("0.00000000") + ',' + GapList[k].sl[4].ToString("0.00000000") + ',' +
                            TimeList[k].sl[5].ToString("0.00000000") + ',' + GapList[k].sl[5].ToString("0.00000000") + ',' +
                            TimeList[k].sl[6].ToString("0.00000000") + ',' + GapList[k].sl[6].ToString("0.00000000") + ',' +
                            TimeList[k].sl[7].ToString("0.00000000") + ',' + GapList[k].sl[7].ToString("0.00000000") + ',' +
                            TimeList[k].sl[8].ToString("0.00000000") + ',' + GapList[k].sl[8].ToString("0.00000000") + ',' +
                            TimeList[k].sl[9].ToString("0.00000000") + ',' + GapList[k].sl[9].ToString("0.00000000"));
                    }

                    for (int it = 9; it < 10; it++)
                    {
                        StreamWriter stat = new StreamWriter(@"...\...\...\...\AEOSS\" + nowtime + "_test.csv", true);

                        List<double> revavg = new List<double>();
                        List<double> timeavg = new List<double>();

                        for (int k = 0; k < runs; k++)
                        {
                            revavg.Add(GapList[k].sl[it]);
                            timeavg.Add(TimeList[k].sl[it]);
                        }

                        solwriter.WriteLine(it.ToString() + ',' + dis[i] + "_" + task.ToString() + "tasks:" + revavg.Min().ToString("0.00000000") + ',' + revavg.Average().ToString("0.00000000") + ',' + revavg.Max().ToString("0.00000000") + ',' + timeavg.Average().ToString("0.00000000"));
                        stat.WriteLine(it.ToString() + ',' + dis[i] + "_" + task.ToString() + "tasks:" + revavg.Min().ToString("0.00000000") + ',' + revavg.Average().ToString("0.00000000") + ',' + revavg.Max().ToString("0.00000000") + ',' + timeavg.Average().ToString("0.00000000"));

                        stat.Close();
                    }
                    solwriter.Close();
                }
            }
            #endregion
        }

        /// <summary>
        /// Run each instance for 10 times and store the revenue of the solution.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TDOPTW_Click(object sender, EventArgs e)
        {
            #region TDOPTW

            int[] ordernum = new int[3] { 20, 50, 100 };
            string nowtime = System.DateTime.Now.ToString("yy-MM-dd HH-mm-ss");

            int runs = 10;

            for (int i = 0; i <= 2; i++)
                for (int j = 1; j <= 4; j++)
                    for (int k = 1; k <= 3; k++)
                    {
                        List<double> revlist = new List<double>();
                        List<double> timelist = new List<double>();
                        StreamWriter result = new StreamWriter(@"...\...\...\...\TDOPTW\TDOPTW_stat_" + nowtime + ".csv", true);
                        for (int r = 0; r < runs; r++)
                        {
                            StreamWriter stat = new StreamWriter(@"...\...\...\...\TDOPTW\TDOPTW_detail_" + nowtime + ".csv", true);
                            ALNSTPF newSolver = new ALNSTPF();
                            TDOPTWProblem ProblemToSolve = new TDOPTWProblem();
                            ProblemToSolve.LoadInstance(ordernum[i].ToString(), j.ToString(), k.ToString());

                            newSolver.Problem = ProblemToSolve;
                            List<double> time = new List<double>();
                            List<double> rev = new List<double>();
                            newSolver.StartExecute(out time, out rev);
                            revlist.Add(rev[rev.Count - 1]);
                            timelist.Add(time[time.Count - 1]);
                            stat.WriteLine(ordernum[i].ToString() + ',' + j.ToString() + ',' + k.ToString() + ',' + rev[rev.Count - 1].ToString("0.000000") + ',' + time[time.Count - 1].ToString("0.000000"));

                            stat.Close();
                        }
                        result.WriteLine(ordernum[i].ToString() + ',' + j.ToString() + ',' + k.ToString() + ',' + revlist.Min().ToString("0.000000") + ',' + revlist.Average().ToString("0.000000") + ',' + revlist.Max().ToString("0.000000") + ',' + timelist.Average().ToString("0.000000"));

                        result.Close();
                    }
            #endregion
        }
    }

    public class Solution
    {
        public List<double> sl = new List<double>();
    }
}
