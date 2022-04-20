using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.InMemory.Exceptions;

namespace MongoDB.InMemory.Extensions
{
    internal static class BsonTypeExtension
    {
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