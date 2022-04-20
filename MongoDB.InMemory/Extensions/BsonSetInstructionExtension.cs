using MongoDB.Bson;
using MongoDB.InMemory.Extensions.Model.DotNotation;

namespace MongoDB.InMemory.Extensions
{
    internal static class BsonSetInstructionExtension
    {
        public static void SetElementWithPath(this BsonValue self, string path, BsonValue newValue)
        {
            self.ResolveElementWithDotNotation(path, bsonPath =>
            {
                switch (bsonPath)
                {
                    case DotNotationArray arrayPath:
                        arrayPath.Set(newValue);
                        break;
                    case DotNotationDocument documentPath:
                        documentPath.Set(newValue);
                        break;
                }
            }, true);
        }
        
        public static void UnsetElementWithPath(this BsonValue self, string path)
        {
            self.ResolveElementWithDotNotation(path, bsonPath =>
            {
                switch (bsonPath)
                {
                    case DotNotationArray arrayPath:
                        arrayPath.Remove();
                        break;
                    case DotNotationDocument documentPath:
                        documentPath.Remove();
                        break;
                }
            }, false);
        }

    }
}