using System;
using MongoDB.InMemory.Exceptions;
using MongoDB.InMemory.ObjectModel.Enums;

namespace MongoDB.InMemory.Extensions
{
    internal static class OperatorExtension
    {

        public static Operator? ToOperator(this string op)
        {
            if (!op.IsOperator())
                return null;

            return op switch
            {
                "$eq" => Operator.Equal,
                "$in" => Operator.In,
                "$gt" => Operator.GreaterThan,
                "$gte" => Operator.GreaterThanOrEqual,
                "$lt" => Operator.LessThan,
                "$lte" => Operator.LessThanOrEqual,
                "$ne" => Operator.NotEqual,
                "$nin" => Operator.NotIn,
                "$and" => Operator.And,
                "$not" => Operator.Not,
                "$nor" => Operator.Nor,
                "$or" => Operator.Or,
                "$exists" => Operator.Exists,
                "$type" => Operator.Type,
                "$regex" => Operator.Regex,
                "$elemMatch" => Operator.ElemMatch,
                "$all" => Operator.All,
                _ => throw new UnknownQuerySelector(op)
            };
        }
        
        public static string ToOperatorString(this Operator op)
        {
            return op switch
            {
                Operator.Equal => "$eq",
                Operator.In => "$in",
                Operator.GreaterThan => "$gt",
                Operator.GreaterThanOrEqual => "$gte",
                Operator.LessThan => "$lt",
                Operator.LessThanOrEqual => "$lte",
                Operator.NotEqual => "$ne",
                Operator.NotIn => "$nin",
                Operator.And => "$and",
                Operator.Not => "$not",
                Operator.Nor => "$nor",
                Operator.Or => "$or",
                Operator.Exists => "$exists",
                Operator.Type => "$type",
                Operator.Regex => "$regex",
                Operator.ElemMatch => "$elemMatch",
                Operator.All => "$all",
                _ => throw new ArgumentOutOfRangeException(op.ToString())
            };
        }

        public static bool IsOperator(this string value)
        {
            return value != null & !string.IsNullOrWhiteSpace(value) && value[0] == '$';
        }
        public static PipelineOperator? ToPipelineOperator(this string op)
        {
            if (!op.IsOperator())
                return null;

            return op switch
            {
                "$addFields" => PipelineOperator.AddFields,
                "$bucket" => PipelineOperator.Bucket,
                "$bucketAuto" => PipelineOperator.BucketAuto,
                "$collStats" => PipelineOperator.CollStats,
                "$count" => PipelineOperator.Count,
                "$facet" => PipelineOperator.Facet,
                "$geoNear" => PipelineOperator.GeoNear,
                "$graphLookup" => PipelineOperator.GraphLookup,
                "$group" => PipelineOperator.Group,
                "$indexStats" => PipelineOperator.IndexStats,
                "$limit" => PipelineOperator.Limit,
                "$listSessions" => PipelineOperator.ListSessions,
                "$lookup" => PipelineOperator.Lookup,
                "$match" => PipelineOperator.Match,
                "$merge" => PipelineOperator.Merge,
                "$out" => PipelineOperator.Out,
                "$planCacheStats" => PipelineOperator.PlanCacheStats,
                "$project" => PipelineOperator.Project,
                "$redact" => PipelineOperator.Redact,
                "$replaceRoot" => PipelineOperator.ReplaceRoot,
                "$replaceWith" => PipelineOperator.ReplaceWith,
                "$sample" => PipelineOperator.Sample,
                "$search" => PipelineOperator.Search,
                "$set" => PipelineOperator.Set,
                "$skip" => PipelineOperator.Skip,
                "$sort" => PipelineOperator.Sort,
                "$sortByCount" => PipelineOperator.SortByCount,
                "$unionWith" => PipelineOperator.UnionWith,
                "$unset" => PipelineOperator.Unset,
                "$pull" => PipelineOperator.Pull,
                "$push" => PipelineOperator.Push,
                "$unwind" => PipelineOperator.Unwind,
                _ => throw new UnknownQuerySelector(op)
            };
        }
    }
}