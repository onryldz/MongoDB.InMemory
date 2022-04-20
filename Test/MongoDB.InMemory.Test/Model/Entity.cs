 namespace MongoDB.InMemory.Test.Model
{
    public class Entity
    {
        public string Str { get; set; }
        public int Int { get; set; }
        public long Int64 { get; set; }
        public bool Bool { get; set; }
        public SubEntity Sub { get; set; }
        public SubEntity[] Subs { get; set; }
        public int[] IntArray { get; set; }
    }
}