namespace Defra.PTS.Common.Models.CustomException
{
    public class OfflineApplicationProcessingException : Exception
    {
        public OfflineApplicationProcessingException(string message) : base(message) { }
        public OfflineApplicationProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }
}
