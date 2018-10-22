using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScannerX
{
    class Scanner
    {
        #region Delegates And Events

        public delegate void ScannerResultDelegate(Scanner from, IPEndPoint ep);

        public event ScannerResultDelegate OnResult;

        #endregion

        /// <summary>
        /// Cancels when calling <code>Stop</code> method.
        /// </summary>
        protected CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();

        /// <summary>
        /// The list storing <code>IPEndPoint</code>s to be scanned.
        /// </summary>
        protected LinkedList<IPEndPoint> ScanList { get; } = new LinkedList<IPEndPoint>();

        /// <summary>
        /// The semaphore to control parallelism.
        /// </summary>
        protected SemaphoreSlim SemaphoreParallelism { get; }

        /// <summary>
        /// The semaphore indicating the amount of elements in <code>ScanList</code>.
        /// </summary>
        protected SemaphoreSlim SemaphoreScanList { get; }
        
        /// <summary>
        /// Maximum degree of parallelism.
        /// </summary>
        public int DegreeOfParallelism { get; }

        /// <summary>
        /// Whether the instance has called <code>Start</code>.
        /// </summary>
        public bool IsStarted { get; protected set; } = false;

        /// <summary>
        /// Whether the instance has called <code>Stop</code>.
        /// </summary>
        public bool IsStopped { get; protected set; } = false;

        /// <summary>
        /// Timeout in miliseconds when connecting to an <code>IPEndPoint</code>.
        /// </summary>
        public int Timeout { get; set; } = 3000;

        /// <summary>
        /// Remaining amount of <code>IPEndPoint</code>s to be scanned.
        /// </summary>
        public int Count
        {
            get
            {
                return SemaphoreScanList.CurrentCount       //To be scanned
                    + WorkingWorkers;                       //Is scanning
            }
        }

        /// <summary>
        /// Workers that is scanning.
        /// </summary>
        public int WorkingWorkers
        {
            get
            {
                return DegreeOfParallelism - SemaphoreParallelism.CurrentCount;
            }
        }

        /// <summary>
        /// Creating an instance.
        /// </summary>
        /// <param name="degreeOfParallelism">The maximum degree of parallelism.</param>
        public Scanner(int degreeOfParallelism)
        {
            DegreeOfParallelism = degreeOfParallelism;
            SemaphoreParallelism = new SemaphoreSlim(DegreeOfParallelism);
            SemaphoreScanList = new SemaphoreSlim(0);
        }

        /// <summary>
        /// Add <paramref name="ep"/> to the scan list. 
        /// </summary>
        public void Add(IPEndPoint ep)
        {
            lock (ScanList)
            {
                ScanList.AddLast(ep);
            }
            SemaphoreScanList.Release();
        }

        /// <summary>
        /// Start scanning.
        /// </summary>
        public void Start()
        {
            if (!IsStarted)
            {
                IsStarted = true;
                Loop();
            }
        }

        /// <summary>
        /// Stop scanning.
        /// </summary>
        public void Stop()
        {
            if (!IsStopped)
            {
                IsStopped = true;
                TokenSource.Cancel();
            }
        }

        /// <summary>
        /// Scanning loop.
        /// </summary>
        protected async void Loop()
        {
            while (!IsStopped)
            {
                try
                {
                    await SemaphoreScanList.WaitAsync(TokenSource.Token).ConfigureAwait(false);
                    IPEndPoint ep;
                    lock (ScanList)
                    {
                        ep = ScanList.First.Value;
                        ScanList.RemoveFirst();
                    }

                    await SemaphoreParallelism.WaitAsync(TokenSource.Token).ConfigureAwait(false);
                    Scan(ep);
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// Try connecting to <paramref name="ep"/> and invokes <code>OnResult</code> event if success.
        /// </summary>
        protected async void Scan(IPEndPoint ep)
        {
            try
            {
                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    client.BeginConnect(ep, null, null);
                    await Task.Delay(Timeout, TokenSource.Token).ConfigureAwait(false);
                    if (client.Connected)
                    {
                        OnResult?.BeginInvoke(this, ep, null, null);
                    }
                }
            }
            catch
            {

            }
            finally
            {
                SemaphoreParallelism.Release();
            }
        }
    }
}
