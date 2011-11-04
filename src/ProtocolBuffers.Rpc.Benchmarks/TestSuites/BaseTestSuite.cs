#region Copyright 2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ProtocolBuffers.Rpc.Benchmarks.TestSuites
{
    abstract class BaseTestSuite
    {
        public int RunServer(IList<string> args)
        {
            using (TestSignals signals = new TestSignals(args[0]))
            using (StartServer(int.Parse(args[1])))
            {
                Console.WriteLine("{0} started.", GetType().Name);
                signals.Ready();
                signals.ExitWait();
            }
            Console.WriteLine("{0} stopped.", GetType().Name);
            return 0;
        }

        public int RunClient(IList<string> args)
        {
            using (TestSignals signals = new TestSignals(args[0]))
            using (signals.AcquireSemaphore())
            {
                int[] counter = new int[2];
                int runCount = int.Parse(args[3]);
                for (int run = 0; run <= runCount; run++)
                {
                    bool bReport = run > 0;
                    int nThreads = int.Parse(args[2]);
                    Thread[] threads = new Thread[nThreads];
                    counter[0] = 0;

                    for (int i = 0; i < nThreads; i++)
                    {
                        string[] info = args.ToArray();
                        info[0] = GetType().Name;
                        info[2] = i.ToString();
                        info[3] = run.ToString();
                        threads[i] = new Thread(
                            () =>
                                {
                                    Interlocked.Increment(ref counter[0]);
                                    if (signals.BeginWait())
                                    {
                                        Stopwatch timer = Stopwatch.StartNew();
                                        int success = 0, requests = int.Parse(args[4]);
                                        RunClient(
                                            requests >= 0 ? requests : int.MaxValue,
                                            requests < 0 ? DateTime.UtcNow.AddMilliseconds(-requests) : DateTime.UtcNow.AddMinutes(1), 
                                            int.Parse(args[5]), out success);
                                        timer.Stop();

                                        if (requests != success)
                                            Interlocked.Increment(ref counter[1]);

                                        if (bReport)
                                        {
                                            if (requests < 0)
                                                info[4] = (-requests).ToString();
                                            lock (typeof (Console))
                                                Console.WriteLine("{0} \t{1} \t{2:n0} \t{3:n2}", String.Join(" \t", info),
                                                                  success, timer.ElapsedMilliseconds,
                                                                  success / (timer.ElapsedMilliseconds / 1000.0));
                                        }
                                    }
                                }
                            );
                        threads[i].Start();
                    }

                    while (counter[0] != nThreads)
                        Thread.Sleep(1);

                    signals.Ready(int.Parse(args[1]));
                    foreach (Thread t in threads)
                        t.Join();

                    //Collect and cool-down
                    GC.Collect(2, GCCollectionMode.Forced);
                    GC.GetTotalMemory(true);
                    GC.WaitForPendingFinalizers();

                    long stop = DateTime.UtcNow.AddMilliseconds(100).Ticks;
                    while (DateTime.UtcNow.Ticks < stop)
                        Thread.Sleep(1);
                }
                return counter[1];
            }
        }

        protected abstract IDisposable StartServer(int responseSize);
        protected abstract void RunClient(int repeatedCount, DateTime stopTime, int responseSize, out int successful);
    }
}