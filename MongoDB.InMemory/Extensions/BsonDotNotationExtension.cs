using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.InMemory.Extensions.Model.DotNotation;

namespace MongoDB.InMemory.Extensions
{
    public static class BsonDotNotationExtension
    {
        internal static void ResolveElementWithDotNotation(this BsonValue instance, string path, Action<DotNotation> callback, bool autoCreateIfNotExists)
        {
            var notations = path.Split(".").ToList();
            instance.ResolveElementWithDotNotation(notations, 0, callback, autoCreateIfNotExists);
        }

        private static void ResolveElementWithDotNotation(this BsonValue instance, List<string> notations, int offset, Action<DotNotation> callback, bool autoCreateIfNotExists)
        {
            string member;
            var total = notations.Count - 1;
            for (; offset < total; offset++)
            {
                member = notations[offset].Trim();
                if (instance.BsonType == BsonType.Document)
                {
                    var bsonDocument = instance.AsBsonDocument;
                    if (bsonDocument.TryGetValue(member, out var bsonValue))
                    {
                        instance = bsonValue;
                        continue;
                    }
                }

                if (instance.BsonType == BsonType.Array)
                {
                    var bsonArray = instance.AsBsonArray;
                    if (member == "$")
                    {
                        ResolveArrayPositionalOperator(bsonArray, callback, notations, offset, autoCreateIfNotExists);
                        return;
                    }

                    if (!int.TryParse(member, out var index))
                    {
                        ResolveArrayLikePositionalOperator(instance.AsBsonArray, notations, offset, callback, autoCreateIfNotExists);
                        return;
                    }

                    if (bsonArray.Count <= index) 
                        return;
                    
                    instance = bsonArray[index];
                    continue;
                }

                if (autoCreateIfNotExists)
                {
                    var newInstance = new BsonDocument();
                    instance[member] = newInstance;
                    instance = newInstance;
                }
            }

            member = notations.Last();
            switch (instance.BsonType)
            {
                case BsonType.Document:
                {
                    callback(new DotNotationDocument(instance) { Member = member });
                    return;
                } 

                case BsonType.Array:
                {
                    if (member == "$")
                    {
                        ResolveArrayPositionalOperator(instance.AsBsonArray, callback);
                        return;
                    }
                    
                    if (!int.TryParse(member, out var index))
                    {
                        ResolveArrayLikePositionalOperator(instance.AsBsonArray, notations, offset, callback, autoCreateIfNotExists);
                        return;
                    }

                    if (autoCreateIfNotExists)
                        CheckArraySize(instance.AsBsonArray, index);

                    callback(new DotNotationArray(instance) {Member = index});
                    return;
                }
                default: return;
            }
        }

        private static void ResolveArrayLikePositionalOperator(BsonArray instance, List<string> notations, int offset, Action<DotNotation> callback, bool autoCreateIfNotExists)
        {
            notations.Insert(offset, "$");
            ResolveArrayPositionalOperator(instance, callback, notations, offset, autoCreateIfNotExists);
            notations.RemoveAt(offset);
        }

        private static void ResolveArrayPositionalOperator(BsonArray instance, Action<DotNotation> callback)
        {
            if (instance.Count == 0)
                return;

            var path = new DotNotationArray(instance);
            for (var i = 0; i < instance.Count; i++)
            {
                path.Member = i;
                callback(path);
            }
        }
        
        private static void ResolveArrayPositionalOperator(BsonArray instance, Action<DotNotation> callback, List<string> notations, int offset, bool autoCreateIfNotExists)
        {
            for (var i = 0; i < instance.Count; i++)
            {
                notations[offset] = i.ToString();
                ResolveElementWithDotNotation(instance, notations, offset, callback, autoCreateIfNotExists);
                notations[offset] = "$";
            }
        }

        private static void CheckArraySize(BsonArray instanceAsBsonArray, int index)
        {
            var delta = index - instanceAsBsonArray.Count + 1;
            if (delta <= 0)
                return;

            instanceAsBsonArray.AddRange(Enumerable.Repeat(BsonValue.Create(null), delta));
        }

        public static bool GetValuesWithDotNotation(this BsonValue document, string path, out List<BsonValue> values)
        {
            var result = new List<BsonValue>();
            document.ResolveElementWithDotNotation(path, bsonPath =>
            {
                var value = bsonPath switch
                {
                    DotNotationDocument documentPath => documentPath.HasMember ? documentPath.Element : null,
                    DotNotationArray pathArray => pathArray.HasElement ? pathArray.Element : null,
                    _ => null
                };
                if (value != null)
                    result.Add(value);
            }, false);

            values = result;
            return result.Any();
        }
    }
}