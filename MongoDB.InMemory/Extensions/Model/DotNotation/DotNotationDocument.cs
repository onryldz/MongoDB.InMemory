using MongoDB.Bson;

namespace MongoDB.InMemory.Extensions.Model.DotNotation
{
    internal class DotNotationDocument : DotNotation<string>
    {
        private new BsonDocument Instance => (BsonDocument) base.Instance;
        public BsonValue Element => Instance[Member];
        public bool HasMember => Instance.Contains(Member);
        public override void Set(BsonValue value) => Instance[Member] = value;
        public override void Remove() => Instance.Remove(Member);
        public DotNotationDocument(BsonValue instance) : base(instance)
        {
        }
    }
}