using System.Runtime.InteropServices;

namespace NetEti.ApplicationControl
{
    // Special thanks to Matt Davis, https://stackoverflow.com/users/51170/matt-davis
    // this class just wraps some Win32 stuff that we're going to use

    /// <summary>
    /// Provides Windows message handling via System.Runtime.InteropServices.
    /// </summary>
    public static class Messaging
    {
        /// <summary>Broadcast message flag.</summary>
        public const int HWND_BROADCAST = 0xffff;

        /// <summary>Registered message type for a Window activation request.</summary>
        public static readonly int WM_SHOWME = RegisterWindowMessage("WM_SHOWME");

        /// <summary>
        /// Wrapper for external PostMessage.
        /// </summary>
        /// <param name="hwnd">Window handle.</param>
        /// <param name="msg">Message to be posted.</param>
        /// <param name="wparam">Pointer to the message.</param>
        /// <param name="lparam">Length of the message.</param>
        /// <returns>True on success.</returns>
        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        /// <summary>
        /// Wrapper for external RegisterMessage.
        /// </summary>
        /// <param name="message">The message to listen for.</param>
        /// <returns>Message-ID on success (0xC000-0xFFFF) or Zero on Error.</returns>
        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message);
    }
}
