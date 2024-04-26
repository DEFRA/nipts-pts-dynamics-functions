using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.CustomException
{
    [ExcludeFromCodeCoverageAttribute]
    public class QueueWriterException : Exception
    {
        public QueueWriterException() { }

        public QueueWriterException(string message) : base(message) { }

        public QueueWriterException(string message, Exception innerException) : base(message, innerException) { }
    }
}
