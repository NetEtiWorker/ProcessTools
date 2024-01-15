using NetEti.ApplicationControl;
using System.Runtime;

namespace ProcessToolsDemo
{
    internal class ThreadParameter
    {
        /// <summary>
        /// Externally generated CancellationToken for a
        /// controlled termination of a thread.
        /// </summary>
        public CancellationToken Token { get; set; }

        /// <summary>
        /// If true, the method responds to an external cancellation request,
        /// which is transmitted via 'Token'.
        /// </summary>
        public bool IsCooperativeCancellingEnabled { get; set; }

        /// <summary>
        /// Optional additional user-object.
        /// </summary>
        public object? UserObject { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ThreadParameter()
        {
            this.IsCooperativeCancellingEnabled = true;
        }
    }

    internal static class Program
    {
        private static ThreadParameter? _asyncPara;

        static void Main(string[] args)
        {
            Prog2(args); // this is the actual full demo.

            // Prog3(args); // for parameterless pattern.
            // Prog1(args); // abandoned!
        }

        #region Prog3
        // Go to "Prog2"!

        // Prog3 is only for Testing the parameterless threading pattern.
        // Prog1 ist the first attempt to check out the functionality of System.Runtime.ControlledExecution,
        //       which has been issued with .net 7.0.
        // Prog1 is abandoned, the actual full demo is "Prog2".

        private static void Prog3(string[] args)
        {
            CancellationTokenSource ctsCooperative = new CancellationTokenSource();
            CancellationToken cooperativeToken = ctsCooperative.Token;

            Console.WriteLine("Disable cooperative-canceling (j/n)? ");
            string? answer = Console.ReadLine();
            ThreadParameter asyncPara = new ThreadParameter() { Token = cooperativeToken, UserObject = "Harry" };
            if (!String.IsNullOrEmpty(answer) && answer.ToLower() == "j")
            {
                asyncPara.IsCooperativeCancellingEnabled = false;
            }
            Abortable demoThread = new Abortable(() =>
            {
                Abortable.CurrentThread.Name = "DemoThread";
                RunAsync();
            });
            if (OperatingSystem.IsWindows())
            {
                demoThread.SetApartmentState(ApartmentState.STA);
            }
            demoThread.IsBackground = true;

            Console.WriteLine($"Main: starting {demoThread.Name}...");
            demoThread.Start();

            Thread.Sleep(5000);
            Console.WriteLine($"Main: trying to stop {demoThread.Name} cooperatively...");
            ctsCooperative.Cancel();
            demoThread.Join(4000); // maximum wait time until the thread has ended by itself.

            if (!demoThread.IsAlive)
            {
                Console.WriteLine($"Main: stopped {demoThread.Name} successfully in a cooperative way.");
            }
            else
            {
                Console.WriteLine($"Main: {demoThread.Name} is still alive - trying to kill it...");
                try
                {
                    demoThread.Abort();
                    demoThread.Join(4000); // maximum wait time until the thread has ended by itself.
                    if (demoThread.AbortableException != null)
                    {
                        Console.WriteLine($"Main: {demoThread.Name} has been stopped by brute force.\r\n{demoThread.AbortableException.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
            ctsCooperative.Dispose();
            Console.WriteLine("Main: good bye!");
            Console.ReadKey();
        }

        /*
        // falsche Überlegung => bringt gar nichts
        private static void ShellActionForParameterizedMethod2(ThreadParameters asyncPara)
        {
            Console.WriteLine("ShellActionForParameterizedMethod: calling RunAsync2.");
            ControlledExecution.Run(new Action(RunAsync2), Program.HostileToken);
        }

        private static void RunAsync2()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + " still running");
                    Thread.Sleep(2000);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("RunAsync finishing because cancellation was requested.");
            }
        }
        */

        private static void RunAsync()
        {
            Console.WriteLine("no UserObject");
            while (true)
            {
                Console.WriteLine(Thread.CurrentThread.Name + " still running");
                Thread.Sleep(2000);
            }
        }

        #endregion Prog3

        #region Prog2
        // Stay here!

        // Prog2 the actual full demo for "Abortable".
        // Prog1 ist the first attempt to check out the functionality of System.Runtime.ControlledExecution,
        //       which has been issued with .net 7.0.
        // Prog3 is only for testing the parameterless threading pattern.

        private static void Prog2(string[] args)
        {
            CancellationTokenSource ctsCooperative = new CancellationTokenSource();
            CancellationToken cooperativeToken = ctsCooperative.Token;

            Console.WriteLine("Disable cooperative-canceling (j/n)? ");
            string? answer = Console.ReadLine();
            ThreadParameter asyncPara = new ThreadParameter() { Token = cooperativeToken, UserObject = "Harry" };
            if (!String.IsNullOrEmpty(answer) && answer.ToLower() == "j")
            {
                asyncPara.IsCooperativeCancellingEnabled = false;
            }
            Abortable demoThread = new Abortable((p1) =>
            {
                Abortable.CurrentThread.Name = "DemoThread";
                RunAsync(p1);
            });
            if (OperatingSystem.IsWindows())
            {
                demoThread.SetApartmentState(ApartmentState.STA);
            }
            demoThread.IsBackground = true;

            Console.WriteLine($"Main: starting {demoThread.Name}...");
            demoThread.Start(asyncPara);

            Thread.Sleep(5000);
            Console.WriteLine($"Main: trying to stop {demoThread.Name} cooperatively...");
            ctsCooperative.Cancel();
            demoThread.Join(4000); // maximum wait time until the thread has ended by itself.

            if (!demoThread.IsAlive)
            {
                Console.WriteLine($"Main: stopped {demoThread.Name} successfully in a cooperative way.");
            }
            else
            {
                Console.WriteLine($"Main: {demoThread.Name} is still alive - trying to kill it...");
                try
                {
                    demoThread.Abort();
                    demoThread.Join(4000); // maximum wait time until the thread has ended by itself.
                    if (demoThread.AbortableException != null)
                    {
                        Console.WriteLine($"Main: {demoThread.Name} has been stopped by brute force.\r\n{demoThread.AbortableException.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
            ctsCooperative.Dispose();
            Console.WriteLine("Main: good bye!");
            Console.ReadKey();
        }

        /*
        // falsche Überlegung => bringt gar nichts
        private static void ShellActionForParameterizedMethod2(ThreadParameters asyncPara)
        {
            Console.WriteLine("ShellActionForParameterizedMethod: calling RunAsync2.");
            ControlledExecution.Run(new Action(RunAsync2), Program.HostileToken);
        }

        private static void RunAsync2()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + " still running");
                    Thread.Sleep(2000);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("RunAsync finishing because cancellation was requested.");
            }
        }
        */

        #endregion Prog2

        private static void RunAsync(object? tp)
        {
            ArgumentNullException.ThrowIfNull(tp, nameof(tp));
            ThreadParameter asyncPara = (ThreadParameter)tp;
            Console.WriteLine("UserObject is '" + asyncPara?.UserObject?.ToString() + "'");
            try
            {
                while (true)
                {
                    if (asyncPara?.IsCooperativeCancellingEnabled == true)
                    {
                        asyncPara.Token.ThrowIfCancellationRequested();
                    }

                    Console.WriteLine(Thread.CurrentThread.Name + " still running");
                    Thread.Sleep(2000);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("RunAsync finishing because cancellation was requested.");
            }
        }

        private static void ShellActionForParameterizedMethod()
        {
            Console.WriteLine("ShellActionForParameterizedMethod: calling RunAsync.");
            RunAsync(Program._asyncPara);
        }

        #region Prog1
        // Go to "Prog2"!

        // Prog1 ist the first attempt to check out the functionality of System.Runtime.ControlledExecution,
        // which has been issued with .net 7.0.
        // Prog1 is abandoned, the actual full demo is "Prog2".
        // Prog3 is only for testing the parameterless threading pattern.

        private static void Prog1(string[] args)
        {
            CancellationTokenSource ctsCooperative = new CancellationTokenSource();
            CancellationToken cooperativeToken = ctsCooperative.Token;
            CancellationTokenSource ctsHostile = new CancellationTokenSource();
            CancellationToken hostileToken = ctsHostile.Token;

            Console.WriteLine("Disable cooperative-canceling (j/n)? ");
            string? answer = Console.ReadLine();
            Program._asyncPara = new ThreadParameter() { Token = cooperativeToken };
            if (!String.IsNullOrEmpty(answer) && answer.ToLower() == "j")
            {
                Program._asyncPara.IsCooperativeCancellingEnabled = false;
            }
            Thread demoThread = new Thread((tp) =>
            {
                Thread.CurrentThread.Name = "DemoThread";
#pragma warning disable SYSLIB0046
                try
                {
                    ControlledExecution.Run(new Action(ShellActionForParameterizedMethod), hostileToken);
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine($"Main: Harry has been stopped by brute force.\r\n{ex.Message}!");
                }
#pragma warning restore SYSLIB0046
            });
            if (OperatingSystem.IsWindows())
            {
                demoThread.SetApartmentState(ApartmentState.STA);
            }
            demoThread.IsBackground = true;

            Console.WriteLine("Main: starting DemoThread(Harry)...");
            demoThread.Start(_asyncPara);

            Thread.Sleep(5000);
            Console.WriteLine("Main: trying to stop Harry cooperatively...");
            ctsCooperative.Cancel();
            demoThread.Join(4000); // maximale Wartezeit, bis der Thread von selbst geendet hat.

            if (!demoThread.IsAlive)
            {
                Console.WriteLine($"Main: stopped {demoThread.Name} successfully in a cooperative way.");
            }
            else
            {
                Console.WriteLine($"Main: {demoThread.Name} is still alive, trying to kill it...");
                try
                {
                    // demoThread.Abort();
                    ctsHostile.Cancel();
                    demoThread.Join(4000); // maximale Wartezeit, bis der Thread von selbst geendet hat.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
            ctsHostile.Dispose();
            ctsCooperative.Dispose();
            Console.WriteLine("Main: good bye!");
            Console.ReadKey();
        }

        #endregion Prog1

    }
}