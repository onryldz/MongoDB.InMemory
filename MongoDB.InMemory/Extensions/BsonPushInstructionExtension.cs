using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.InMemory.Extensions.Model.DotNotation;

namespace MongoDB.InMemory.Extensions
{
    public static class BsonPushInstructionExtension
    {
        private static readonly IReadOnlyList<Action<BsonArray>> EmptyModifierList = new List<Action<BsonArray>>();
        
        public static void PushElementWithPath(this BsonValue self, string path, BsonValue value)
        {
            var modifiers = GetPushModifiers(value);
            var anyModifiers = modifiers.Any();
            self.ResolveElementWithDotNotation(path, bsonPath =>
            {
                if (!(bsonPath is DotNotationDocument { HasMember: true } bsonPathDocument))
                    return;

                if (!bsonPathDocument.Element.IsBsonArray)
                    return;

                var instance = bsonPathDocument.Element.AsBsonArray;
                if (anyModifiers)
                {
                    foreach (var modifier in modifiers)
                        modifier(instance);
                }
                else
                    instance.Add(value);
            }, false);
        }
        
        private static IReadOnlyList<Action<BsonArray>> GetPushModifiers(BsonValue value)
        {
            if (!value.IsBsonDocument)
                return EmptyModifierList;

            var result = new List<Action<BsonArray>>();
            var document = value.AsBsonDocument;
            foreach (var element in document)
            {
                if (element.Name == "$each")
                    result.Add(ApplyEach(element.Value.AsBsonArray));
            }

            return result;
        }

        private static Action<BsonArray> ApplyEach(IEnumerable<BsonValue> values)
        {
            return bsonArray => bsonArray.AddRange(values);
        }
        
        private static Action<BsonArray> ApplySort(IEnumerable<BsonValue> values)
        {
            return bsonArray => bsonArray.AddRange(values);
        }
    }
}