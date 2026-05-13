using System;

namespace TwinSpool.Services
{
    public sealed class TransportException : Exception
    {
        public TransportException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
