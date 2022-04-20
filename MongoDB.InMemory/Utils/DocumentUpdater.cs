using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.InMemory.Exceptions;
using MongoDB.InMemory.Extensions;
using MongoDB.InMemory.ObjectModel.Enums;

namespace MongoDB.InMemory.Utils
{
    internal static class DocumentUpdater
    {
        public static void Update(BsonValue document, BsonValue setInfo)
        {
            if (!setInfo.IsBsonDocument)
                return;
            
            ReadInstructions(setInfo.AsBsonDocument.AsEnumerable(), document);
        }

        private static void ReadInstructions(this IEnumerable<BsonElement> self, BsonValue document)
        {
            foreach (var bsonElement in self)
            {
                var op = bsonElement.Name.ToPipelineOperator();
                switch (op)
                {
                    case PipelineOperator.Set:
                        ApplySetInstructions(document, bsonElement);
                        break;
                    case PipelineOperator.Unset:
                        ApplyUnsetInstructions(document, bsonElement);
                        break;
                    case PipelineOperator.Pull:
                        ApplyPullInstructions(document, bsonElement.Value.AsBsonDocument);
                        break;
                    case PipelineOperator.Push:
                        ApplyPushInstructions(document, bsonElement.Value.AsBsonDocument);
                        break;
                    default:
                        throw new UnsupportedUpdateOperator(bsonElement.Name);
                }
            }
        }

        private static void ApplyPushInstructions(BsonValue document, BsonDocument pushDocument)
        {
            pushDocument
                .AsEnumerable()
                .PushInstruction(document);
        }

        private static void ApplyPullInstructions(BsonValue document, BsonDocument pullDocument)
        {
            pullDocument
                .AsEnumerable()
                .PullInstruction(document);
        }

        private static void ApplyUnsetInstructions(BsonValue document, BsonElement bsonElement)
        {
            bsonElement
                .Value
                .AsBsonDocument
                .AsEnumerable()
                .UnsetInstruction(document);
        }

        private static void ApplySetInstructions(BsonValue document, BsonElement bsonElement)
        {
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

        private static void UnsetInstruction(this IEnumerable<BsonElement> self, BsonValue document)
        {
            foreach (var newValue in self)
                document.UnsetElementWithPath(newValue.Name);
        }
        
        private static void PullInstruction(this IEnumerable<BsonElement> self, BsonValue document)
        {
            foreach (var bsonElement in self)
            {
                var memberPath = bsonElement.Name;
                var filter = bsonElement.Value;
                document.PullElementWithPath(memberPath, filter);
            }
        }
        
        private static void PushInstruction(this IEnumerable<BsonElement> self, BsonValue document)
        {
            foreach (var bsonElement in self)
            {
                var memberPath = bsonElement.Name;
                var modifiers = bsonElement.Value;
                document.PushElementWithPath(memberPath, modifiers);
            }
        }
    }
}