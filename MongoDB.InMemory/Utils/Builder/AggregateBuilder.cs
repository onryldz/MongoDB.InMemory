using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.InMemory.Extensions;
using MongoDB.InMemory.ObjectModel.Enums;

namespace MongoDB.InMemory.Utils.Builder
{
    internal static class AggregateBuilder
    {
        public static IEnumerable<BsonValue> Apply(IEnumerable<BsonDocument> pipelines, IEnumerable<BsonValue> collection)
        {
            return Apply(pipelines, collection, out _);
        }

        public static IEnumerable<BsonValue> Apply(IEnumerable<BsonDocument> pipelines, IEnumerable<BsonValue> collection, out string outResults)
        {
            outResults = null;
            foreach (var pipeline in pipelines)
                foreach (var stage in pipeline)
                {
                    var op = stage.Name.ToPipelineOperator();
                    if (op == PipelineOperator.Out)
                    {
                        outResults = stage.Value.AsString;
                        continue;
                    }

                    collection = op switch
                    {
                        PipelineOperator.Match => Match(collection, stage.Value.AsBsonDocument),
                        PipelineOperator.Limit => collection.Take(stage.Value.AsInt32),
                        PipelineOperator.Skip => collection.Skip(stage.Value.AsInt32),
                        PipelineOperator.Sort => Sort(collection, stage.Value.AsBsonDocument),
                        PipelineOperator.Project => Select(collection, stage.Value.AsBsonDocument),
                        PipelineOperator.Group => Group(collection, stage.Value.AsBsonDocument),
                        _ => collection
                    };
                }

            return collection;
        }

        private static IEnumerable<BsonValue> Select(IEnumerable<BsonValue> documents, BsonDocument select)
        {
            return documents.Select(document => new BsonDocument(
                select.Select(element => new BsonElement(element.Name, element.AsReference(document))))
            );
        }

        private static IEnumerable<BsonValue> Group(IEnumerable<BsonValue> documents, BsonDocument select)
        {
            return documents.Select(document => new BsonDocument(
                select.Select(element => new BsonElement(element.Name, element.AsReference(document))))
            );
        }

        private static BsonValue AsReference(this BsonElement self, BsonValue reference)
        {
            if (self.Value.BsonType != BsonType.String || !self.Value.AsString.StartsWith("$"))
                return self.Value;
            return reference[self.Value.AsString.Substring(1)];
        }

        private static IEnumerable<BsonValue> Match(IEnumerable<BsonValue> documents, BsonDocument filter)
        {
            var predicate = WhereBuilder.Compile(filter);
            return documents.Where(f => predicate(f));
        }

        private static IEnumerable<BsonValue> Sort(IEnumerable<BsonValue> documents, BsonDocument sortFields)
        {
            return sortFields.Aggregate((IOrderedEnumerable<BsonValue>)null, (agg, cur) =>
           {
               var isDescending = cur.Value.AsInt32 == -1;
               if (agg == null)
               {
                   return isDescending
                       ? documents.OrderByDescending(f => BsonTypeMapper.MapToDotNetValue(f[cur.Name]))
                       : documents.OrderBy(f => BsonTypeMapper.MapToDotNetValue(f[cur.Name]));
               }
               return isDescending
                   ? agg.ThenByDescending(f => BsonTypeMapper.MapToDotNetValue(f[cur.Name]))
                   : agg.ThenBy(f => BsonTypeMapper.MapToDotNetValue(f[cur.Name]));
           });
        }
    }
}