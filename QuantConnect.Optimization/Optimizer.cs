using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;

using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Packets;
using QuantConnect.Logging;
using System.Collections.Concurrent;
using QuantConnect.Util;
using System.Runtime.CompilerServices;

namespace QuantConnect.Optimization
{
    class Optimizer
    {
        private static HashSet<Algorithm.CSharp.model.Parameters> _parameterSet;
        private static RabbitmqHandler _workerQueue = new RabbitmqHandler();
        private static ConcurrentDictionary<int, string[]> _optimizeSummaries = new ConcurrentDictionary<int, string[]>();

        public static void Main(string[] args)
        {
            /*
            // 1. Set or Get the shared environment
            Config.Set("live-mode", "false");
            Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
            Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
            Config.Set("api-handler", "QuantConnect.Api.Api");
            Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.ConsoleResultHandler");
            Config.Set("param-set", "");
            var algorithName = "ParameterAlgorithm";
            */

            // 2. Generete all the possible parameters sets
            _parameterSet = GenerateParameterSpace();

            // 3. Concurrently running all parameters sets
            if (_parameterSet.Count == 0)
            {
                Log.Trace("No parameter set to run, Exit!");
                return;
            }
            int i = 0;
            List<Worker> workerList = new List<Worker>();
            foreach (Algorithm.CSharp.model.Parameters parameters in _parameterSet)
            {
                _workerQueue.Publish(parameters);
                Log.Trace("Worker Thread " + i + " is init to handle the job");
                Worker worker = new Worker();
                Thread workerThread = new Thread(worker.Run) { IsBackground = true, Name = "WorkerThread" + i++ };            
                workerThread.Start();
                workerList.Add(worker);
                Thread.Sleep(5 * 1000);
            }

            // 4. Summarize the statistics
            var ts = Stopwatch.StartNew();
            while (workerList.Any(w => w.IsActive) && ts.ElapsedMilliseconds < 3000 * 1000)
            {
                Thread.Sleep(10 * 1000);
                Log.Trace("Waiting for worker threads to exit...");
            }

            Log.Trace("All the workers have finished their job. Ready to quit. Press any key to exit the program.");
            Console.ReadLine();

        }

        private static HashSet<Algorithm.CSharp.model.Parameters> GenerateParameterSpace()
        {
            var result = new HashSet<Algorithm.CSharp.model.Parameters>();

            var fastPeriodConfig = Config.Get("FastPeriod");  // e.g. 100:200:10
            var slowPeriodConfig = Config.Get("SlowPeriod");  // e.g. 150:250:10
            var fastPeriodRange = fastPeriodConfig.Split(':').Select(x => x.ToInt32()).ToList();
            var slowPeriodRange = slowPeriodConfig.Split(':').Select(x => x.ToInt32()).ToList();
            int steps = 0;
            while (fastPeriodRange[0] + steps * fastPeriodRange[2] <= fastPeriodRange[1]
                && slowPeriodRange[0] + steps * slowPeriodRange[2] <= slowPeriodRange[1]
                && fastPeriodRange[0] + steps * fastPeriodRange[2] < slowPeriodRange[0] + steps * slowPeriodRange[2])
            {
                var param = new Algorithm.CSharp.model.Parameters()
                {
                    FastPeriod = fastPeriodRange[0] + steps * fastPeriodRange[2],
                    SlowPeriod = slowPeriodRange[0] + steps * slowPeriodRange[2]
                };
                result.Add(param);
                steps++;
            }

            return result;
        }

    }


    public class Worker
    {
        public bool IsActive { get; private set; }
        private const string _collapseMessage = "Unhandled exception breaking past controls and causing collapse of algorithm node. This is likely a memory leak of an external dependency or the underlying OS terminating the LEAN engine.";

        public void Run()
        {
            IsActive = true;
            LaunchLean();
            IsActive = false;
            //summary = _resultshandler.GetSummary();
        }

        private void LaunchLean()
        {
            //Initialize:
            var mode = "RELEASE";
            #if DEBUG
            mode = "DEBUG";
            #endif

            if (OS.IsWindows)
            {
                Console.OutputEncoding = System.Text.Encoding.Unicode;
            }

            var environment = Config.Get("environment");
            var liveMode = Config.GetBool("live-mode");
            Log.DebuggingEnabled = Config.GetBool("debug-mode");
            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

            //Name thread for the profiler:
            //Thread.CurrentThread.Name = "Algorithm Analysis Thread";
            Log.Trace("Engine.Main(): LEAN ALGORITHMIC TRADING ENGINE v" + Globals.Version + " Mode: " + mode + " (" + (Environment.Is64BitProcess ? "64" : "32") + "bit)");
            Log.Trace("Engine.Main(): Started " + DateTime.Now.ToShortTimeString());
            Log.Trace("Engine.Main(): Memory " + OS.ApplicationMemoryUsed + "Mb-App  " + +OS.TotalPhysicalMemoryUsed + "Mb-Used  " + OS.TotalPhysicalMemory + "Mb-Total");

            //Import external libraries specific to physical server location (cloud/local)
            LeanEngineSystemHandlers leanEngineSystemHandlers;
            try
            {
                leanEngineSystemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            }
            catch (Exception compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            //Setup packeting, queue and controls system: These don't do much locally.
            leanEngineSystemHandlers.Initialize();

            //-> Pull job from QuantConnect job queue, or, pull local build:
            string assemblyPath;
            var job = leanEngineSystemHandlers.JobQueue.NextJob(out assemblyPath);

            if (job == null)
            {
                throw new Exception("Engine.Main(): Job was null.");
            }

            LeanEngineAlgorithmHandlers leanEngineAlgorithmHandlers;
            try
            {
                leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            }
            catch (Exception compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            if (environment.EndsWith("-desktop"))
            {
                var info = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    FileName = Config.Get("desktop-exe"),
                    Arguments = Config.Get("desktop-http-port")
                };
                Process.Start(info);
            }

            // if the job version doesn't match this instance version then we can't process it
            // we also don't want to reprocess redelivered jobs
            if (VersionHelper.IsNotEqualVersion(job.Version) || job.Redelivered)
            {
                Log.Error("Engine.Run(): Job Version: " + job.Version + "  Deployed Version: " + Globals.Version + " Redelivered: " + job.Redelivered);
                //Tiny chance there was an uncontrolled collapse of a server, resulting in an old user task circulating.
                //In this event kill the old algorithm and leave a message so the user can later review.
                leanEngineSystemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, _collapseMessage);
                leanEngineSystemHandlers.Notify.SetAuthentication(job);
                leanEngineSystemHandlers.Notify.Send(new RuntimeErrorPacket(job.UserId, job.AlgorithmId, _collapseMessage));
                leanEngineSystemHandlers.JobQueue.AcknowledgeJob(job);
                return;
            }

            try
            {
                var algorithmManager = new AlgorithmManager(liveMode);

                leanEngineSystemHandlers.LeanManager.Initialize(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, job, algorithmManager);

                var engine = new Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, liveMode);
                engine.Run(job, algorithmManager, assemblyPath);
            }
            finally
            {
                //Delete the message from the job queue:
                leanEngineSystemHandlers.JobQueue.AcknowledgeJob(job);
                Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);

                // clean up resources
                leanEngineSystemHandlers.Dispose();
                leanEngineAlgorithmHandlers.Dispose();
                Log.LogHandler.Dispose();

                Log.Trace("Program.Main(): Exiting Lean...");
                
                Environment.Exit(0);
            }
        }


        /*
         // job related variables
         private Api.Api _api;
         private Messaging.Messaging _notify;
         private JobQueue _jobQueue;
        
         // result related variables
         // each worker produce one run summary
         // all workers write one line summary to _optimizeSummaries 
         private IResultHandler _resultshandler;
         public string[] summary;
         public ConcurrentDictionary<int, string[]> _optimizeSummaries;
        
         // lean related 
         private FileSystemDataFeed _dataFeed;
         private ConsoleSetupHandler _setup;
         private BacktestingRealTimeHandler _realTime;
         private ITransactionHandler _transactions;
         private IFactorFileProvider _factorProvider;
         private IHistoryProvider _historyProvider;
         private IDataProvider _dataProvider;
         private IMapFileProvider _mapFileProvider;
         */


        /*
        private void LaunchLean()
        {
            // read from config
            Config.Set("environment", "backtesting");
            string algorithm = "EMATest";
            Config.Set("algorithm-type-name", algorithm);

            //create lean variable instances
            _jobQueue = new JobQueue();
            _notify = new Messaging.Messaging();
            _api = new Api.Api();
            _resultshandler = new OptimizationResultHandler();
            //_resultshandler = new ConsoleResultHandler();
            _dataFeed = new FileSystemDataFeed();
            _setup = new ConsoleSetupHandler();
            _realTime = new BacktestingRealTimeHandler();
            _factorProvider = new LocalDiskFactorFileProvider();
            _mapFileProvider = new LocalDiskMapFileProvider();
            _dataProvider = new RedisDataProvider();
            _transactions = new BacktestingTransactionHandler();
            Log.LogHandler = (ILogHandler)new FileLogHandler();
            Log.DebuggingEnabled = true;
            Log.DebuggingLevel = 2;

            // run engine instance
            //var systemHandlers = new LeanEngineSystemHandlers(_jobQueue, _api, _notify);
            LeanEngineSystemHandlers systemHandlers;
            systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);  
            systemHandlers.Initialize();

            var algorithmHandlers = new LeanEngineAlgorithmHandlers(_resultshandler, _setup, _dataFeed, _transactions, 
                _realTime, _mapFileProvider, _factorProvider, _dataProvider);
            string algorithmPath;
            var liveMode = Config.GetBool("live-mode");
            var algorithmManager = new AlgorithmManager(liveMode);
            AlgorithmNodePacket job = systemHandlers.JobQueue.NextJob(out algorithmPath);
            var _engine = new Engine(systemHandlers, algorithmHandlers, Config.GetBool("live-mode"));
            _engine.Run(job, algorithmManager, algorithmPath);

            // write summary to sharedResult
            writeToSharedResult();

            //Delete the message from the job queue:
            systemHandlers.Dispose();
            algorithmHandlers.Dispose();
        }
        */

    }

}
