using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ByteCounter.Handlers
{
    /// <summary>
    /// Threads handler.
    /// </summary>
    public class ThreadsHandler
    {
        private readonly Queue<Thread> _waitingThreads = new Queue<Thread>();

        private readonly Dictionary<int, Worker> _runningThreads =
            new Dictionary<int, Worker>();

        private readonly int _maxThreads;
        private readonly object _locker = new object();


        /// <summary>
        /// Release flow.
        /// </summary>
        public bool Done
        {
            get
            {
                lock (_locker)
                {
                    return _waitingThreads.Count == 0 && _runningThreads.Count == 0;
                }
            }
        }

        /// <summary>
        /// Creating an instance of ThreadSHandler with the parameters of the
        /// maximum number of threads and the root path of the process.
        /// </summary>
        /// <param name="maxThreads"></param>
        /// <param name="dirPath"></param>
        public ThreadsHandler(int maxThreads, string dirPath)
        {
            _maxThreads = maxThreads;
            // queue up a thread for each file
            Directory.GetFiles(dirPath).ToList()
                .ForEach(n => _waitingThreads.Enqueue(CreateThread(n)));
        }

        private Thread CreateThread(string fileNameArg)
        {
            var thread =
                new Thread(new Worker(fileNameArg, WorkerStart, WorkerDone).FileHandler)
                {
                    IsBackground = true
                };
            return thread;
        }

        /// <summary>
        /// Called when a worker starts
        /// </summary>
        /// <param name="threadIdArg"></param>
        /// <param name="workerArg"></param>
        public void WorkerStart(int threadIdArg, Worker workerArg)
        {
            lock (_locker)
            {
                // update with worker instance
                _runningThreads[threadIdArg] = workerArg;
            }
        }

        /// <summary>
        /// Called when a worker finishes
        /// </summary>
        /// <param name="threadIdArg"></param>
        public void WorkerDone(int threadIdArg)
        {
            lock (_locker)
            {
                _runningThreads.Remove(threadIdArg);
            }

            //Console.WriteLine($"  Thread {threadIdArg.ToString()} done.");
            LaunchWaitingThreads();
        }

        /// <summary>
        /// Launches workers until max is reached.
        /// </summary>
        public void LaunchWaitingThreads()
        {
            lock (_locker)
            {
                while (_runningThreads.Count < _maxThreads && _waitingThreads.Count > 0)
                {
                    var thread = _waitingThreads.Dequeue();
                    _runningThreads.Add(thread.ManagedThreadId,
                        null); // place holder so count is accurate
                    thread.Start();
                }
            }
        }
    }
}