using System.Reflection;
using System.Runtime.Versioning;

namespace CurrencyCloud.Environment
{
    internal static class Platform
    {
        /// <summary>
        /// Gets .NET version number.
        /// </summary>
        public static readonly string Version;

        static Platform()
        {
            Version = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ??
                      ".NET";
        }
    }
}