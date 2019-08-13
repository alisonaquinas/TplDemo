using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace LifeTAP2
{
    class MainClass
    {
        static double[,]
            msCells;

        static CancellationTokenSource
            TokenSource = new CancellationTokenSource();

        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            return MainAsync(args, TokenSource.Token).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            TokenSource.Cancel();
        }

        private static async Task<int> MainAsync(string[] args, CancellationToken ct)
        {
            msCells = new double[Console.BufferHeight - 4, Console.BufferWidth/3];

            Task<string>
                fReadTask = Task.Run(()=>Console.In.ReadLine());
            bool
                fReset = true;
            Random
                random = new Random();

            List<Task>
                LifeTasks = new List<Task>();

            for (int i = 0; i < msCells.GetLength(0); ++i)
            {
                for (int j = 0; j < msCells.GetLength(1); ++j)
                {
                    Task fCellTask = CellLife(i, j, msCells, ct);
                    await Task.Yield();
                    LifeTasks.Add(fCellTask);
                }
            }

            while (!ct.IsCancellationRequested)
            {
                if (fReset)
                {
                    for (int i = 0; i < msCells.GetLength(0); ++i)
                    {
                        for (int j = 0; j < msCells.GetLength(1); ++j)
                        {
                            msCells[i, j] = random.NextDouble();
                        }
                    }
                    fReset = false;
                    Console.Clear();
                }

                Task
                    fPlayTask = Task.Delay(20);

                await
                    Task.WhenAny(
                        LifeTasks.Concat(new[] {
                        fReadTask,
                        fPlayTask })).ConfigureAwait(false);

                if (fReadTask.IsCompleted)
                {
                    switch (fReadTask.Result)
                    {
                        case "x":
                            TokenSource.Cancel();
                            break;
                        default:
                            fReset = true;
                            break;
                    }
                    fReadTask = Task.Run(() => Console.In.ReadLine());
                }
                Console.SetCursorPosition(0, 0);
                for (int i = 0; i < msCells.GetLength(0); ++i)
                {
                    for (int j = 0; j < msCells.GetLength(1); ++j)
                    { 
                        Console.ForegroundColor = (ConsoleColor)Math.Round(15 * msCells[i, j]);
                        Console.Write($"{(byte)(msCells[i,j] * 255):X} ".PadRight(3));
                    }
                    Console.WriteLine();
                }
                Console.ForegroundColor = ConsoleColor.White;

                ThreadPool.GetAvailableThreads(out int fWorker, out int fComp);
                ThreadPool.GetMaxThreads(out int fMaxWorker, out int fMaxComp);
                ThreadPool.GetMinThreads(out int fMinWorker, out int fMinComp);


                Console.WriteLine($"{fMaxWorker,4}, {fMaxComp,4}");
                Console.WriteLine($"{fMaxWorker - fWorker,4}, {fMaxComp - fComp,4}");
                Console.WriteLine($"{fMinWorker,4}, {fMinComp,4}");

            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Environment.Exit(1);
            return 1;
        }

        private static async Task CellLife(
            int aRow,
            int aCol,
            double[,] aWorld,
            CancellationToken cancellation)
        {
            bool
                fLive = aWorld[aRow, aCol] >= .5;

           
            while(!cancellation.IsCancellationRequested)
            {
                double
                    fNeigborPop = 0;

                //Top Row
                if (aRow > 0)
                {
                    if (aCol > 0)
                        fNeigborPop += aWorld[(aRow - 1), (aCol - 1)];

                    fNeigborPop += aWorld[(aRow - 1), (aCol)];

                    if (aCol < (aWorld.GetLength(1) - 1))
                        fNeigborPop += aWorld[(aRow - 1), (aCol + 1)];
                }

                //Bottom Row
                if (aRow > (aWorld.GetLength(0) -1))
                {
                    if (aCol > 0)
                        fNeigborPop += aWorld[aRow + 1, (aCol - 1)];

                    fNeigborPop += aWorld[aRow + 1, (aCol)];

                    if (aCol < (aWorld.GetLength(1) - 1))
                        fNeigborPop += aWorld[aRow + 1, (aCol + 1)];
                }

                //Left
                if (aCol > 0)
                    fNeigborPop += aWorld[aRow, (aCol - 1)];

                //Right
                if (aCol < (aWorld.GetLength(1) - 1))
                    fNeigborPop += aWorld[aRow, (aCol + 1)];
                bool
                    fTransition = false;
                if(fLive)
                {
                    if (fNeigborPop <= 1.5 || fNeigborPop >= 3.5)
                    {
                        fTransition = true;
                        fLive = false;
                    }
                }
                else
                {
                    if (fNeigborPop > 2.25 && fNeigborPop < 3.75)
                    {
                        fTransition = true;
                        fLive = true;
                    }
                }

                double
                    fMyVal = aWorld[aRow, aCol];

                if (fLive)
                {
                    fMyVal += fTransition? .01 : .001;
                    aWorld[aRow, aCol] = Math.Min(fMyVal, 1d);
                }
                else
                {
                    fMyVal -= fTransition ? .01 : .001;
                    aWorld[aRow, aCol] = Math.Max(fMyVal, 0d);
                }

                await Task.Delay(10);
            }
        }
    }
}
