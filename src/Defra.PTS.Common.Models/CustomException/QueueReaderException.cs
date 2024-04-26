using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.CustomException
{
    [ExcludeFromCodeCoverageAttribute]
    public class QueueReaderException : Exception
    {
        public QueueReaderException() { }

        public QueueReaderException(string message) : base(message) { }

        public QueueReaderException(string message, Exception innerException) : base(message, innerException) { }
    }
}
