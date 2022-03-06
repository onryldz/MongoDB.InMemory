using System;
using System.Collections.Generic;
using System.Linq;
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
            var parameterExpression = System.Linq.Expressions.Expression.Parameter(typeof(BsonValue), "f");
            var body = filter.Elements.ReadExpression(parameterExpression);
            return System.Linq.Expressions.Expression
                .Lambda<Func<BsonValue, bool>>(body, parameterExpression)
                .Compile();
        }

        private static System.Linq.Expressions.Expression ReadExpression(this IEnumerable<BsonElement> self, System.Linq.Expressions.Expression parameterExpression, BsonElement? member = null)
        {
            return self.Aggregate((System.Linq.Expressions.Expression)null, (acc, element) =>
            {
                var expression = ReadBinaryOrUnaryExpr(element, parameterExpression, member);
                if (acc != null)
                    expression = System.Linq.Expressions.Expression.And(acc, expression);

                return expression;
            });
        }

        private static System.Linq.Expressions.Expression ReadBinaryOrUnaryExpr(BsonElement element, System.Linq.Expressions.Expression parameterExpression, BsonElement? member = null)
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
                Operator.ElemMatch => ReadElemMatchExpr(member.Value, parameterExpression),
                Operator.Not => ReadNotExpr(element, member.Value, parameterExpression),
                _ => element.Value.AsBsonDocument.Elements.ReadExpression(parameterExpression, op.HasValue ? member : element)
            };
        }

        private static System.Linq.Expressions.Expression ReadNotExpr(BsonElement element, BsonElement member, System.Linq.Expressions.Expression parameterExpression)
        {
            var expr = element.Value.AsBsonDocument.Elements.ReadExpression(parameterExpression, new BsonElement(member.Name, element.Value));
            return System.Linq.Expressions.Expression.Not(expr);
        }

        private static System.Linq.Expressions.Expression ReadElemMatchExpr(BsonElement member, System.Linq.Expressions.Expression parameterExpression)
        {
            try
            {
                return CallPredicate(parameterExpression, member.Name, member.Value["$elemMatch"], Operator.ElemMatch);
            }
            catch (Exception e)
            {
                throw new Exception(member.ToString(), e);
            }
        }

        private static System.Linq.Expressions.Expression CallPredicate(System.Linq.Expressions.Expression parameterExpression, string memberName, BsonValue right, Operator? op)
        {
            var memberNameExpr = System.Linq.Expressions.Expression.Constant(memberName, typeof(string));
            var memberValueExpr = System.Linq.Expressions.Expression.Constant(right, typeof(BsonValue));
            var opExpression = System.Linq.Expressions.Expression.Constant(op, typeof(Operator?));
            return System.Linq.Expressions.Expression.Call(CompareMethod, parameterExpression, memberNameExpr, memberValueExpr, opExpression);
        }

        private static bool Predicate(BsonValue document, string memberName, BsonValue right, Operator? op)
        {
            var found = document.GetValueWithPath(memberName, out var left);
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

            if (!found || left.BsonType != right.BsonType && op != Operator.In && op != Operator.NotIn && op != Operator.Regex && op != Operator.ElemMatch)
                return false;

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
                Operator.Regex => right.AsRegex.IsMatch(left.AsString),
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

        private static System.Linq.Expressions.Expression ReadLogicalExpr(this BsonArray self, Operator op, System.Linq.Expressions.Expression parameterExpression)
        {
            return self.Aggregate((System.Linq.Expressions.Expression)null, (acc, right) =>
            {
                var expression = right.AsBsonDocument.ReadExpression(parameterExpression);
                if (acc != null)
                {
                    expression = op switch
                    {
                        Operator.Or => System.Linq.Expressions.Expression.Or(acc, expression),
                        Operator.And => System.Linq.Expressions.Expression.And(acc, expression),
                        Operator.Nor => System.Linq.Expressions.Expression.Not(System.Linq.Expressions.Expression.Or(acc, expression)),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
                return expression;
            });
        }
    }
}