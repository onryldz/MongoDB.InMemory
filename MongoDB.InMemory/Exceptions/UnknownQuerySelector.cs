using System;

namespace MongoDB.InMemory.Exceptions
{
    public class UnknownQuerySelector : Exception
    {
        public UnknownQuerySelector(string selector) :
            base($"Unknown query selector [{selector}]")
        {
        }
    }
}