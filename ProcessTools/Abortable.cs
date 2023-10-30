using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Principal;

namespace NetEti.ApplicationControl
{
    /// <summary>
    /// DotNet 7.0 threading helper class.
    /// Behaves like Thread, except the following specials:
    ///     - Abort() works similarly to previous runtime versions;
    ///     - Abortable offers an additional property 'AbortableException';
    ///     - Abortable implements IDisposable.
    /// Usage: replace "new Thread..." with "new Abortable...",
    ///        Call Dispose on Abortable-instance when it's no longer used.
    /// 
    /// Attention: This class uses <see cref="System.Runtime.ControlledExecution.Run(Action, CancellationToken)"/>,
    /// which was released with.Net 7.0. Although this method is new, it has been marked as "deprecated"!
    /// </summary>
    /// <remarks>
    /// 17.03.2023 Erik Nagel: created.
    /// 06.08.2023 Erik Nagel: Added missing comments.
    /// </remarks>
    public class Abortable
    {
        #region modified Thread members

        private Thread _thread;

        /// <summary>
        /// Returns the current helper-thread or null.
        /// </summary>
        /// <returns>Current helper-thread of this or a given Abortable or null.</returns>
        public Thread? GetThread()
        {
            return this._thread;
        }

        /// <summary>
        /// Contans an optional exception of the executed thread.
        /// </summary>
        public Exception? AbortableException { get; set; }

        /// <summary>
        /// Creates a new Abortable-instance for a parameterless thread.
        /// </summary>
        /// <param name="start">The method to run (parameterless).</param>
        public Abortable(ThreadStart start)
        {
            this.AbortableException = null;
            this._startWithoutParameters = start;
            this._thread = new Thread(ThreadStartWorker);
        }

        /// <summary>
        /// Creates a new Abortable-instance for a parameterless thread
        /// with a given maximum stacksize.
        /// </summary>
        /// <param name="start">The method to run (parameterless).</param>
        /// <param name="maxStackSize">Maximum stacksize that can be consumed by the running method.</param>
        public Abortable(ThreadStart start, int maxStackSize)
        {
            this.AbortableException = null;
            this._startWithoutParameters = start;
            this._thread = new Thread(ThreadStartWorker, maxStackSize);
        }

        /// <summary>
        /// Creates a new Abortable-instance for a parameterized thread.
        /// </summary>
        /// <param name="start">The method to run (including a user-parameter).</param>
        public Abortable(ParameterizedThreadStart start)
        {
            this.AbortableException = null;
            this._startWithParameter = start;
            this._thread = new Thread(ParameterizedThreadStartWorker);
        }

        /// <summary>
        /// Creates a new Abortable-instance for a parameterized thread
        /// with a given maximum stacksize.
        /// </summary>
        /// <param name="start">The method to run (including a user-parameter).</param>
        /// <param name="maxStackSize">Maximum stacksize that can be consumed by the running method.</param>
        public Abortable(ParameterizedThreadStart start, int maxStackSize)
        {
            this.AbortableException = null;
            this._startWithParameter = start;
            this._thread = new Thread(ParameterizedThreadStartWorker, maxStackSize);
        }

        /// <summary>
        /// Throws a System.Threading.ThreadAbortException on the thread on which the call
        /// was done to start thread termination.
        /// By calling this method the thread is usually terminated.
        /// 
        /// Exceptions:
        /// T:System.PlatformNotSupportedException:
        /// .NET Core only: This member is not supported.
        ///
        /// T:System.Security.SecurityException:
        /// The caller does not have the required permission.
        ///
        /// T:System.Threading.ThreadStateException:
        /// The thread that was killed is currently suspended.
        /// 
        /// </summary>
        [SecuritySafeCritical]
        public void Abort()
        {
            _hostileTokenSource?.Cancel();
            _thread.Join(50);
            _hostileTokenSource?.Dispose();
        }

        /// <summary>Causes the operating system to change the state of the current instance to <see cref="ThreadState.Running"/>, and optionally supplies an object containing data to be used by the method the thread executes.</summary>
        /// <param name="parameter">An object that contains data to be used by the method the thread executes.</param>
        /// <exception cref="ThreadStateException">The thread has already been started.</exception>
        /// <exception cref="OutOfMemoryException">There is not enough memory available to start this thread.</exception>
        /// <exception cref="InvalidOperationException">This thread was created using a <see cref="ThreadStart"/> delegate instead of a <see cref="ParameterizedThreadStart"/> delegate.</exception>
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public void Start(object? parameter)
        {
            this._startParameter = parameter;
            _hostileTokenSource?.Dispose();
            _hostileTokenSource = new CancellationTokenSource();
            this._hostileToken = _hostileTokenSource.Token;
            _thread.Start(parameter);
        }

        /// <summary>Causes the operating system to change the state of the current instance to <see cref="ThreadState.Running"/>.</summary>
        /// <exception cref="ThreadStateException">The thread has already been started.</exception>
        /// <exception cref="OutOfMemoryException">There is not enough memory available to start this thread.</exception>
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public void Start()
        {
            _hostileTokenSource?.Dispose();
            _hostileTokenSource = new CancellationTokenSource();
            this._hostileToken = _hostileTokenSource.Token;
            _thread.Start();
        }

        private ParameterizedThreadStart? _startWithParameter;
        private ThreadStart? _startWithoutParameters;
        private object? _startParameter;
        private CancellationTokenSource? _hostileTokenSource;
        private CancellationToken _hostileToken;

        private void ParameterizedThreadStartWorker(object? obj)
        {
            // Console.WriteLine("here is ParameterizedThreadStartWorker");
#pragma warning disable SYSLIB0046
            try
            {
                ControlledExecution.Run(new Action(ShellActionForParameterizedMethod), _hostileToken);
            }
            catch (Exception ex)
            {
                this.AbortableException = ex;
            }
#pragma warning restore SYSLIB0046
        }

        private void ShellActionForParameterizedMethod()
        {
            if (_startWithParameter != null)
            {
                _startWithParameter(this._startParameter);
            }
        }

        private void ThreadStartWorker()
        {
            // Console.WriteLine("here is ParameterizedThreadStartWorker");
#pragma warning disable SYSLIB0046
            try
            {
                ControlledExecution.Run(new Action(ShellActionForMethod), _hostileToken);
            }
            catch (Exception ex)
            {
                this.AbortableException = ex;
            }
#pragma warning restore SYSLIB0046
        }

        private void ShellActionForMethod()
        {
            if (_startWithoutParameters != null)
            {
                _startWithoutParameters();
            }
        }

        #endregion modified Thread members

        #region unmodified routed to Thread

        // #pragma warning disable CS1591

        /// <summary>
        /// Leads to the extern int thread-id.
        /// </summary>
        public int ManagedThreadId => _thread.ManagedThreadId;

        /// <summary>
        /// Wait for a length of time proportional to 'iterations'.  Each iteration is should
        /// only take a few machine instructions.  Calling this API is preferable to coding
        /// a explicit busy loop because the hardware can be informed that it is busy waiting.
        /// </summary>
        /// <param name="iterations">Iteration - only takes a few machine instructions.</param>
        public static void SpinWait(int iterations) => Thread.SpinWait(iterations);

        /// <summary>
        /// Leads to YieldInternal.
        /// </summary>
        /// <returns>A boolean, that indicates, whether to continue or not.</returns>
        public static bool Yield() => Thread.Yield();

        /// <summary>Returns true if the thread has been started and is not dead.</summary>
        public bool IsAlive => _thread.IsAlive;

        /// <summary>
        /// Return whether or not this thread is a background thread.  Background
        /// threads do not affect when the Execution Engine shuts down.
        /// </summary>
        public bool IsBackground
        {
            get => _thread.IsBackground;
            set => _thread.IsBackground = value;
        }

        /// <summary>Returns true if the thread is a threadpool thread.</summary>
        public bool IsThreadPoolThread => _thread.IsThreadPoolThread;

        /// <summary>Returns the priority of the thread.</summary>
        public ThreadPriority Priority
        {
            get => _thread.Priority;
            set => _thread.Priority = value;
        }

        /// <summary>
        /// Return the thread state as a consistent set of bits.  This is more
        /// general then IsAlive or IsBackground.
        /// </summary>
        public ThreadState ThreadState => _thread.ThreadState;

        /// <summary>
        /// Leads to the internal Interop ApartmentState.
        /// </summary>
        /// <returns>The internal Interop ApartmentState.</returns>
        public ApartmentState GetApartmentState() => _thread.GetApartmentState();

        /// <summary>
        /// Leads to the extern void DisableComObjectEagerCleanup.
        /// </summary>
        public void DisableComObjectEagerCleanup() => _thread.DisableComObjectEagerCleanup();

        /// <summary>
        /// Interrupts a thread that is inside a Wait(), Sleep() or Join().  If that
        /// thread is not currently blocked in that manner, it will be interrupted
        /// when it next begins to block.
        /// </summary>
        public void Interrupt() => _thread.Interrupt();

        /// <summary>
        /// Waits for the thread to die or for timeout milliseconds to elapse.
        /// </summary>
        /// <param name="millisecondsTimeout">Timeout in milliseconds</param>
        /// <returns>
        /// Returns true if the thread died, or false if the wait timed out. If
        /// -1 is given as the parameter, no timeout will occur.
        /// </returns>
        /// <exception cref="ArgumentException">if timeout &lt; -1 (Timeout.Infinite)</exception>
        /// <exception cref="ThreadInterruptedException">if the thread is interrupted while waiting</exception>
        /// <exception cref="ThreadStateException">if the thread has not been started yet</exception>
        public bool Join(int millisecondsTimeout) => _thread.Join(millisecondsTimeout);

        /// <summary>
        /// Returns the current processor-id.
        /// </summary>
        /// <returns>The current processor-id.</returns>
        public static int GetCurrentProcessorId() => Thread.GetCurrentProcessorId();

        /// <summary>
        /// Contains the current culture.
        /// </summary>
        public CultureInfo CurrentCulture
        { 
            get => _thread.CurrentCulture;
            set => _thread.CurrentCulture = value;
        }

        /// <summary>
        /// Contains the current ui-culture.
        /// </summary>
        public CultureInfo CurrentUICulture
        { 
            get => _thread.CurrentUICulture;
            set => _thread.CurrentUICulture = value;
        }

        /// <summary>
        /// Contains the current principal or null.
        /// </summary>
        public static IPrincipal? CurrentPrincipal
        {
            get => Thread.CurrentPrincipal;
            set => Thread.CurrentPrincipal = value;
        }

        /// <summary>
        /// Leads to the CurrentThread.
        /// </summary>
        public static Thread CurrentThread => Thread.CurrentThread;

        /// <summary>
        /// Suspends the current thread for timeout milliseconds. If timeout == 0,
        /// forces the thread to give up the remainder of its timeslice.  If timeout
        /// == Timeout.Infinite, no timeout will occur.
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        public static void Sleep(int millisecondsTimeout) => Thread.Sleep(millisecondsTimeout);

        /// <summary>
        /// Leads to Thread.ExecutionContext.
        /// </summary>
        public ExecutionContext? ExecutionContext => _thread.ExecutionContext;

        /// <summary>
        /// Contains the thread-name or null.
        /// </summary>
        public string? Name
        {
            get => _thread.Name;
            set => _thread.Name = value;
        }

        /// <summary>Causes the operating system to change the state of the current instance to <see cref="ThreadState.Running"/>.</summary>
        /// <exception cref="ThreadStateException">The thread has already been started.</exception>
        /// <exception cref="OutOfMemoryException">There is not enough memory available to start this thread.</exception>
        /// <remarks>
        /// Unlike <see cref="Start()"/>, which captures the current <see cref="ExecutionContext"/> and uses that context to invoke the thread's delegate,
        /// <see cref="UnsafeStart()"/> explicitly avoids capturing the current context and flowing it to the invocation.
        /// </remarks>
        public void UnsafeStart() => _thread.UnsafeStart();

        /// <summary>Causes the operating system to change the state of the current instance to <see cref="ThreadState.Running"/>, and optionally supplies an object containing data to be used by the method the thread executes.</summary>
        /// <param name="parameter">An object that contains data to be used by the method the thread executes.</param>
        /// <exception cref="ThreadStateException">The thread has already been started.</exception>
        /// <exception cref="OutOfMemoryException">There is not enough memory available to start this thread.</exception>
        /// <exception cref="InvalidOperationException">This thread was created using a <see cref="ThreadStart"/> delegate instead of a <see cref="ParameterizedThreadStart"/> delegate.</exception>
        /// <remarks>
        /// Unlike <see cref="Start(object?)"/>, which captures the current <see cref="ExecutionContext"/> and uses that context to invoke the thread's delegate,
        /// <see cref="UnsafeStart()"/> explicitly avoids capturing the current context and flowing it to the invocation.
        /// </remarks>
        public void UnsafeStart(object? parameter) => _thread.UnsafeStart(parameter);

        /// <summary>
        /// Is no longer supported.
        /// 
        /// Throws a System.Threading.ThreadAbortException on the thread on which the call
        /// was done to start thread termination.
        /// By calling this method the thread is usually terminated.
        /// 
        /// Exceptions:
        /// T:System.PlatformNotSupportedException:
        /// .NET Core only: This member is not supported.
        ///
        /// T:System.Security.SecurityException:
        /// The caller does not have the required permission.
        ///
        /// T:System.Threading.ThreadStateException:
        /// The thread that was killed is currently suspended.
        /// 
        /// </summary>
        /// <param name="stateInfo">StateInfo or null.</param>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [Obsolete()]
        public void Abort(object? stateInfo)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Is no longer supported.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [Obsolete()]
        public static void ResetAbort()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Is no longer supported.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [Obsolete("Thread.Suspend has been deprecated. Use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.")]
        public void Suspend()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Is no longer supported.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [Obsolete("Thread.Resume has been deprecated. Use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.")]
        public void Resume()
        {
            throw new PlatformNotSupportedException();
        }

        // Currently, no special handling is done for critical regions, and no special handling is necessary to ensure thread
        // affinity. If that changes, the relevant functions would instead need to delegate to RuntimeThread.

        /// <summary>
        /// Leads to Thread.BeginCriticalRegion().
        /// </summary>
        public static void BeginCriticalRegion() => Thread.BeginCriticalRegion();

        /// <summary>
        /// Leads to Thread.EndCriticalRegion().
        /// </summary>
        public static void EndCriticalRegion() => Thread.EndCriticalRegion();

        /// <summary>
        /// Leads to Thread.BeginThreadAffinity().
        /// </summary>
        public static void BeginThreadAffinity() => Thread.BeginThreadAffinity();

        /// <summary>
        /// Leads to Thread.EndThreadAffinity().
        /// </summary>
        public static void EndThreadAffinity() => Thread.EndThreadAffinity();

        /// <summary>
        /// Leads to Thread.AllocateDataSlot().
        /// </summary>
        /// <returns>LocalDataStoreSlot</returns>
        public static LocalDataStoreSlot AllocateDataSlot() => Thread.AllocateDataSlot();

        /// <summary>
        /// Leads to Thread.AllocateNamedDataSlot(...).
        /// </summary>
        /// <param name="name">Name of the DataSlot.</param>
        /// <returns>LocalDataStoreSlot</returns>
        public static LocalDataStoreSlot AllocateNamedDataSlot(string name) => Thread.AllocateNamedDataSlot(name);

        /// <summary>
        /// Leads to Thread.GetNamedDataSlot(...).
        /// </summary>
        /// <param name="name">The name of the DataSlot.</param>
        /// <returns>LocalDataStoreSlot</returns>
        public static LocalDataStoreSlot GetNamedDataSlot(string name) => Thread.GetNamedDataSlot(name);

        /// <summary>
        /// Leads to Thread.FreeNamedDataSlot(...).
        /// </summary>
        /// <param name="name">The name of the DataSlot.</param>
        public static void FreeNamedDataSlot(string name) => Thread.FreeNamedDataSlot(name);

        /// <summary>
        /// Leads to Thread.GetNamedDataSlot(...).
        /// </summary>
        /// <param name="slot">The LocalDataStoreSlot.</param>
        /// <returns>object?</returns>
        public static object? GetData(LocalDataStoreSlot slot) => Thread.GetData(slot);

        /// <summary>
        /// Leads to Thread.SetData(...).
        /// </summary>
        /// <param name="slot">The LocalDataStoreSlot.</param>
        /// <param name="data">The data to store.</param>
        public static void SetData(LocalDataStoreSlot slot, object? data) => Thread.SetData(slot, data);

        /// <summary>
        /// Is no longer supported.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [Obsolete("The ApartmentState property has been deprecated. Use GetApartmentState, SetApartmentState or TrySetApartmentState instead.")]
        public ApartmentState ApartmentState => _thread.ApartmentState;

        /// <summary>
        /// Leads to Thread.SetApartmentState(...).
        /// </summary>
        /// <param name="state">ApartmentState</param>
        [SupportedOSPlatform("windows")]
        public void SetApartmentState(ApartmentState state) => _thread.SetApartmentState(state);

        /// <summary>
        /// Leads to Thread.TrySetApartmentState(...).
        /// </summary>
        /// <param name="state">ApartmentState</param>
        /// <returns>Boolean indicating success.</returns>
        public bool TrySetApartmentState(ApartmentState state) => _thread.TrySetApartmentState(state);

        /// <summary>
        /// Is no longer supported.
        /// </summary>
        /// <returns>CompressedStack.</returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [Obsolete()]
        public CompressedStack GetCompressedStack()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Is no longer supported.
        /// </summary>
        /// <param name="stack">CompressedStack</param>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [Obsolete()]
        public void SetCompressedStack(CompressedStack stack)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Leads to Thread.GetDomain().
        /// </summary>
        /// <returns>AppDomain</returns>
        public static AppDomain GetDomain() => Thread.GetDomain();

        /// <summary>
        /// Leads to Thread.GetDomainID().
        /// </summary>
        /// <returns>DomainID</returns>
        public static int GetDomainID() => Thread.GetDomainID();

        /// <summary>
        /// Leads to Thread.GetHashCode().
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => _thread.GetHashCode();

        /// <summary>
        /// Leads to Thread.Join().
        /// </summary>
        public void Join() => _thread.Join();

        /// <summary>
        /// Leads to Thread.Join(...).
        /// </summary>
        /// <param name="timeout">TimeSpan timeout.</param>
        /// <returns>bool</returns>
        public bool Join(TimeSpan timeout) => _thread.Join(timeout);

        /// <summary>
        /// Leads to Thread.MemoryBarrier.
        /// </summary>
        public static void MemoryBarrier() => Thread.MemoryBarrier();

        /// <summary>
        /// Leads to Thread.Sleep(...).
        /// </summary>
        /// <param name="timeout">Timeout as TimeSpan.</param>
        public static void Sleep(TimeSpan timeout) => Thread.Sleep(timeout);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>byte</returns>
        public static byte VolatileRead(ref byte address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>double</returns>
        public static double VolatileRead(ref double address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>short</returns>
        public static short VolatileRead(ref short address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>int</returns>
        public static int VolatileRead(ref int address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>long</returns>
        public static long VolatileRead(ref long address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>IntPtr</returns>
        public static IntPtr VolatileRead(ref IntPtr address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>object?</returns>
        public static object? VolatileRead([NotNullIfNotNull(nameof(address))] ref object? address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>sbyte</returns>
        public static sbyte VolatileRead(ref sbyte address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>float</returns>
        public static float VolatileRead(ref float address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>ushort</returns>
        public static ushort VolatileRead(ref ushort address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>uint</returns>
        public static uint VolatileRead(ref uint address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>ulong</returns>
        public static ulong VolatileRead(ref ulong address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileRead(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <returns>UIntPtr</returns>
        public static UIntPtr VolatileRead(ref UIntPtr address) => Thread.VolatileRead(ref address);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref byte address, byte value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref double address, double value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref short address, short value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref int address, int value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref long address, long value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref IntPtr address, IntPtr value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite([NotNullIfNotNull(nameof(value))] ref object? address, object? value)
                                     => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref sbyte address, sbyte value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref float address, float value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref ushort address, ushort value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref uint address, uint value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref ulong address, ulong value) => Thread.VolatileWrite(ref address, value);

        /// <summary>
        /// Leads to Thread.VolatileWrite(...).
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">value</param>
        public static void VolatileWrite(ref UIntPtr address, UIntPtr value) => Thread.VolatileWrite(ref address, value);

        // #pragma warning restore CS1591

        #endregion unmodified routed to Thread

    }
}
