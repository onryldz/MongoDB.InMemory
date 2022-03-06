using System;

namespace MongoDB.InMemory.Exceptions
{
    public class UnsupportedBsonTypeValue : Exception
    {
        public UnsupportedBsonTypeValue(string value) :
            base($"Unsupported bson type [{value}]")
        {
        }
    }
}