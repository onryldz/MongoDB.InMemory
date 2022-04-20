using System.Linq;
using MongoDB.Bson;
using MongoDB.InMemory.Extensions.Model.DotNotation;
using MongoDB.InMemory.Utils.Builder;

namespace MongoDB.InMemory.Extensions
{
    internal static class BsonPullInstructionExtension
    {
        public static void PullElementWithPath(this BsonValue self, string path, BsonValue filter)
        {
            var predicate = filter.IsBsonDocument
                ? WhereBuilder.Compile(filter.AsBsonDocument)
                : null;
            
            self.ResolveElementWithDotNotation(path, bsonPath =>
            {
                switch (bsonPath)
                {
                    case DotNotationDocument documentPath:
                        var element = documentPath.Element;
                        if (element == null || element.IsBsonArray == false)
                            break;

                        var arrayElement = element.AsBsonArray;
                        var removeItems = arrayElement
                            .Where(f => predicate?.Invoke(f) ?? filter == f)
                            .ToList();
                        
                        removeItems.ForEach(f => arrayElement.Remove(f));
                        break;
                }
            }, false);
        }
    }
}