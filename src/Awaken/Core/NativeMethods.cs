using System.Runtime.InteropServices;

namespace Awaken.Core;

internal static partial class NativeMethods
{
    [LibraryImport("kernel32.dll")]
    internal static partial ExecutionState SetThreadExecutionState(ExecutionState esFlags);
}
