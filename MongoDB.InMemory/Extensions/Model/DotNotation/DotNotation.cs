using MongoDB.Bson;

namespace MongoDB.InMemory.Extensions.Model.DotNotation
{
    internal abstract class DotNotation
    {
        protected DotNotation(BsonValue instance)
        {
            Instance = instance;
        }
        protected BsonValue Instance { get; set; }
    }
    
    internal abstract class DotNotation<T>: DotNotation
    {
        public T Member { get; set; }

        public abstract void Set(BsonValue value);
        public abstract void Remove();

        protected DotNotation(BsonValue instance) : base(instance)
        {
        }
    }
}