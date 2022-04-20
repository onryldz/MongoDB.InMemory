using System;

namespace MongoDB.InMemory.Exceptions
{
    public class UnsupportedUpdateOperator : Exception
    {
        public UnsupportedUpdateOperator(string value) :
            base($"Unsupported update operator [{value}]")
        {
        }
    }
}