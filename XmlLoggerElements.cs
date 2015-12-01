using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kobush.Build
{
    internal static class XmlLoggerElements
    {
        public const string Build = "msbuild";
        public const string Error = "error";
        public const string Warning = "warning";
        public const string Message = "message";
        public const string Project = "project";
        public const string Target = "target";
        public const string Task = "task";
        public const string Custom = "custom";
        public const string Failure = "failure";
        public const string Duration = "duration";
    }
}
