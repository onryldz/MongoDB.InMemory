using MongoDB.Bson;

namespace MongoDB.InMemory.Extensions.Model.DotNotation
{
    internal class DotNotationArray : DotNotation<int>
    {
        private new BsonArray Instance => (BsonArray) base.Instance;
        public override void Set(BsonValue value) => Instance[Member] = value;
        public override void Remove() => Instance.RemoveAt(Member);
        public BsonValue Element => Instance[Member];
        public bool HasElement => Member < Instance.Count;

        public DotNotationArray(BsonValue instance) : base(instance)
        {
        }
    }
}