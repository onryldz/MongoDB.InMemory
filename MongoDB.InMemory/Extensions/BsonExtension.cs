using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Xml.Serialization;
using MongoDB.Bson;
using MongoDB.InMemory.Exceptions;

namespace MongoDB.InMemory.Extensions
{
    internal static class BsonExtension
    {
        public static void SetElementWithPath(this BsonValue instance, string path, BsonValue newValue)
        {
            var notations = path.Split(".");
            if (!notations.Any())
                return;

            string member;
            var total = notations.Length - 1;
            for (var i = 0; i < total; i++)
            {
                member = notations[i];
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
                    if (int.TryParse(member, out var index) && bsonArray.Count > index)
                    {
                        instance = bsonArray[index];
                        continue;
                    }
                }

                var newInstance = new BsonDocument();
                instance[member] = newInstance;
                instance = newInstance;
            }

            member = notations.Last();
            switch (instance.BsonType)
            {
                case BsonType.Document:
                    instance[member] = newValue;
                    break;

                case BsonType.Array:
                {
                    var index = int.Parse(member);
                    CheckArraySize(instance.AsBsonArray, index);
                    instance[index] = newValue;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private static void CheckArraySize(BsonArray instanceAsBsonArray, int index)
        {
            var delta = index - instanceAsBsonArray.Count + 1;
            if (delta <= 0)
                return;

            instanceAsBsonArray.AddRange(Enumerable.Repeat(BsonValue.Create(null), delta));
        }

        public static bool GetValueWithPath(this BsonValue document, string path, out BsonValue result)
        {
            var notations = path.Split(".");
            result = notations.Aggregate(document, (acc, member) =>
            {
                switch (acc?.BsonType)
                {
                    case BsonType.Document:
                        {
                            var doc = acc.AsBsonDocument;
                            return doc.TryGetValue(member, out var bsonValue)
                                ? bsonValue
                                : null;
                        }
                    case BsonType.Array:
                        {
                            var bsonArray = acc.AsBsonArray;
                            return int.TryParse(member, out var index) && bsonArray.Count > index
                                ? bsonArray[index]
                                : null;
                        }
                    default:
                        return null;
                }
            });
            return result != null;
        }

        public static BsonValue GetValueWithPath(this BsonValue document, string path)
        {
            return document.GetValueWithPath(path, out var result) ? result : null;
        }

        private static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
        {
            var result = seed;
            foreach (var element in source)
            {
                result = func(result, element);
                if (result == null)
                    break;
            }

            return result;
        }

        public static BsonType GetBsonTypeFromValue(this BsonValue typeValue)
        {
            switch (typeValue.BsonType)
            {
                case BsonType.String:
                    {
                        var value = typeValue.AsString;
                        return value switch
                        {
                            "double" => BsonType.Double,
                            "string" => BsonType.String,
                            "object" => BsonType.Document,
                            "array" => BsonType.Array,
                            "binData" => BsonType.Binary,
                            "undefined" => BsonType.Undefined,
                            "objectId" => BsonType.ObjectId,
                            "bool" => BsonType.Boolean,
                            "date" => BsonType.DateTime,
                            "null" => BsonType.Null,
                            "regex" => BsonType.RegularExpression,
                            "javascript" => BsonType.JavaScript,
                            "symbol" => BsonType.Symbol,
                            "javascriptWithScope" => BsonType.JavaScriptWithScope,
                            "int" => BsonType.Int32,
                            "timestamp" => BsonType.Timestamp,
                            "long" => BsonType.Int64,
                            "decimal" => BsonType.Decimal128,
                            "minKey" => BsonType.MinKey,
                            "MaxKey" => BsonType.MaxKey,
                            _ => throw new UnsupportedBsonTypeValue(value)
                        };
                    }
                case BsonType.Int32:
                    {
                        var value = typeValue.AsInt32;
                        return value switch
                        {
                            1 => BsonType.Double,
                            2 => BsonType.String,
                            3 => BsonType.Document,
                            4 => BsonType.Array,
                            5 => BsonType.Binary,
                            6 => BsonType.Undefined,
                            7 => BsonType.ObjectId,
                            8 => BsonType.Boolean,
                            9 => BsonType.DateTime,
                            10 => BsonType.Null,
                            11 => BsonType.RegularExpression,
                            13 => BsonType.JavaScript,
                            14 => BsonType.Symbol,
                            15 => BsonType.JavaScriptWithScope,
                            16 => BsonType.Int32,
                            17 => BsonType.Timestamp,
                            18 => BsonType.Int64,
                            19 => BsonType.Decimal128,
                            -1 => BsonType.MinKey,
                            127 => BsonType.MaxKey,
                            _ => throw new UnsupportedBsonTypeValue(value.ToString())
                        };
                    }
                default:
                    throw new UnsupportedBsonTypeValue(typeValue.ToString());
            }
        }
    }
}