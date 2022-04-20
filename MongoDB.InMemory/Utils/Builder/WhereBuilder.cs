using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.InMemory.Extensions;
using MongoDB.InMemory.ObjectModel.Enums;

namespace MongoDB.InMemory.Utils.Builder
{
    internal static class WhereBuilder
    {
        private static readonly MethodInfo CompareMethod = typeof(WhereBuilder).GetMethod(nameof(Predicate), BindingFlags.NonPublic | BindingFlags.Static);

        public static Func<BsonValue, bool> Compile(BsonDocument filter)
        {
            var parameterExpression = Expression.Parameter(typeof(BsonValue), "f");
            var body = filter.Elements.ReadExpression(parameterExpression);
            return Expression
                .Lambda<Func<BsonValue, bool>>(body, parameterExpression)
                .Compile();
        }

        private static Expression ReadExpression(this IEnumerable<BsonElement> self,
            Expression parameterExpression, BsonElement? member = null)
        {
            return self.Aggregate((Expression) null, (acc, element) =>
            {
                var expression = ReadBinaryOrUnaryExpr(element, parameterExpression, member);
                if (acc != null)
                    expression = Expression.And(acc, expression);

                return expression;
            });
        }

        private static Expression ReadBinaryOrUnaryExpr(BsonElement element,
            Expression parameterExpression, BsonElement? member = null)
        {
            var op = element.Name.ToOperator();
            if (!op.HasValue && element.Value.BsonType == BsonType.RegularExpression)
                op = Operator.Regex;

            if (element.Value.BsonType == BsonType.Array && op == Operator.And || op == Operator.Or || op == Operator.Nor)
                return element.Value.AsBsonArray.ReadLogicalExpr(op.Value, parameterExpression);

            if (element.Value.BsonType != BsonType.Document)
                return CallPredicate(parameterExpression, member?.Name ?? element.Name, element.Value, op);

            return op switch
            {
                Operator.ElemMatch when member == null => ReadOpenElemMatchExpr(element, parameterExpression),
                Operator.ElemMatch => ReadElemMatchExpr(member.Value, parameterExpression),
                Operator.Not => ReadNotExpr(element, member.Value, parameterExpression),
                _ => element.Value.AsBsonDocument.Elements.ReadExpression(parameterExpression,
                    op.HasValue ? member : element)
            };
        }

        private static Expression ReadOpenElemMatchExpr(BsonElement bsonElement, Expression parameterExpression)
        {
            return CallPredicate(parameterExpression, null, bsonElement.Value, Operator.ElemMatch);
        }

        private static Expression ReadNotExpr(BsonElement element, BsonElement member,
            Expression parameterExpression)
        {
            var expr = element.Value.AsBsonDocument.Elements.ReadExpression(parameterExpression, new BsonElement(member.Name, element.Value));
            return Expression.Not(expr);
        }

        private static Expression ReadElemMatchExpr(BsonElement member,
            Expression parameterExpression)
        {
            return CallPredicate(parameterExpression, member.Name, member.Value["$elemMatch"], Operator.ElemMatch);
        }

        private static Expression CallPredicate(
            Expression parameterExpression, string memberName, BsonValue right, Operator? op)
        {
            var memberNameExpr = Expression.Constant(memberName, typeof(string));
            var memberValueExpr = Expression.Constant(right, typeof(BsonValue));
            var opExpression = Expression.Constant(op, typeof(Operator?));
            return Expression.Call(CompareMethod, parameterExpression, memberNameExpr,
                memberValueExpr, opExpression);
        }

        private static bool PredicateArray(BsonArray bsonArray, BsonValue right, Operator? op)
        {
            return op switch
            {
                null when right.IsBsonArray => bsonArray == right,
                null when right.IsBsonDocument => PredicateArrayWithFilter(bsonArray, right.AsBsonDocument),
                null => !right.IsBsonDocument && bsonArray.Contains(right),
                Operator.All => right.IsBsonArray && PredicateArrayAllOp(bsonArray, right.AsBsonArray),
                Operator.ElemMatch => ElemMatch(bsonArray, right),
                _ => bsonArray.All(document => Predicate(document, null, right, op))
            };
        }

        private static bool PredicateArrayWithFilter(BsonArray instanceValue, BsonDocument filter)
        {
            var predicate = Compile(filter);
            return instanceValue.Any(predicate);
        }
        
        private static bool PredicateArrayAllOp(BsonArray instanceValue, BsonArray allConditions)
        {
            var predicates = allConditions
                .Where(f => f.BsonType == BsonType.Document)
                .Select(value => Compile(value.AsBsonDocument))
                .ToArray();

            bool conditionResult;
            var hasPredicate = predicates.Any();
            if (hasPredicate)
            {
                conditionResult = predicates.All(predicate => predicate(instanceValue));
                if (!conditionResult)
                    return false;    
            }

            var values = allConditions
                .Where(f => f.BsonType != BsonType.Document)
                .ToArray();

            var hasValues = values.Any();
            if (hasValues)
            {
                conditionResult = values.All(f => instanceValue.Any(r => f == r));
                if (!conditionResult)
                    return false;
            }

            return hasValues || hasPredicate;
        }

        private static bool Predicate(BsonValue document, string memberName, BsonValue right, Operator? op)
        {
            if (memberName == null || memberName.IsOperator())
                return PredicateValue(right, document, op, false);
            
            return document.GetValuesWithDotNotation(memberName, out var values) 
                ? values.Any(left => PredicateValue(right, left, op, true))
                : PredicateValue(right, null, op, false);
        }

        private static bool PredicateValue(BsonValue right, BsonValue left, Operator? op, bool found)
        {
            switch (op)
            {
                case Operator.Exists:
                    return found == right;
                case Operator.Type when !found:
                    return false;
                case Operator.Type when right.BsonType != BsonType.Array:
                    return left.BsonType == right.GetBsonTypeFromValue();
                case Operator.Type:
                    return right.AsBsonArray.Select(f => f.GetBsonTypeFromValue()).Any(f => f == left.BsonType);
            }

            if (left?.IsBsonArray == true)
                return PredicateArray(left.AsBsonArray, right, op);

            op ??= Operator.Equal;
            return op switch
            {
                Operator.Equal => left == right,
                Operator.GreaterThan => left > right,
                Operator.GreaterThanOrEqual => left >= right,
                Operator.In => right.AsBsonArray.Any(f => InPredicate(left, f)),
                Operator.NotIn => right.AsBsonArray.All(f => !InPredicate(left, f)),
                Operator.LessThan => left < right,
                Operator.LessThanOrEqual => left <= right,
                Operator.NotEqual => left != right,
                Operator.Not => right == false,
                Operator.Exists => true,
                Operator.Regex => left != null && right.AsRegex.IsMatch(left.AsString),
                Operator.ElemMatch => ElemMatch(left, right),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static bool ElemMatch(BsonValue left, BsonValue right)
        {
            if (left.BsonType != BsonType.Array)
                return false;

            var predicate = Compile(right.AsBsonDocument);
            return left.AsBsonArray.Any(predicate);
        }

        private static bool InPredicate(BsonValue left, BsonValue right)
        {
            if (right.BsonType == BsonType.RegularExpression && left.BsonType == BsonType.String)
                return right.AsRegex.IsMatch(left.AsString);
            return right == left;
        }

        private static Expression ReadLogicalExpr(this BsonArray self, Operator op,
            Expression parameterExpression)
        {
            return self.Aggregate((Expression) null, (acc, right) =>
            {
                var expression = right.AsBsonDocument.ReadExpression(parameterExpression);
                if (acc != null)
                {
                    expression = op switch
                    {
                        Operator.Or => Expression.Or(acc, expression),
                        Operator.And => Expression.And(acc, expression),
                        Operator.Nor => Expression.Not(
                            Expression.Or(acc, expression)),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }

                return expression;
            });
        }
    }
}