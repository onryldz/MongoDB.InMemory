using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.InMemory.Extensions;
using MongoDB.InMemory.ObjectModel.Enums;
using MongoDB.Libmongocrypt;

namespace MongoDB.InMemory.Utils
{
    internal static class DocumentUpdater
    {
        public static void Set(BsonValue document, BsonValue setInfo)
        {
            if (!setInfo.IsBsonDocument)
                return;

            ReadInstructions(setInfo.AsBsonDocument.AsEnumerable(), document);
        }

        private static void ReadInstructions(this IEnumerable<BsonElement> self, BsonValue document)
        {
            foreach (var bsonElement in self)
                if (bsonElement.Name.ToPipelineOperator() == PipelineOperator.Set)
                    bsonElement
                        .Value
                        .AsBsonDocument
                        .AsEnumerable()
                        .SetInstruction(document);
        }

        private static void SetInstruction(this IEnumerable<BsonElement> self, BsonValue document)
        {
            foreach (var newValue in self)
               document.SetElementWithPath(newValue.Name, newValue.Value);
        }

        
    }
}