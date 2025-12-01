using System;

namespace TradeDataStudio.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when configuration validation fails.
    /// </summary>
    public class ConfigurationValidationException : Exception
    {
        public ConfigurationValidationException(string message) : base(message)
        {
        }

        public ConfigurationValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}