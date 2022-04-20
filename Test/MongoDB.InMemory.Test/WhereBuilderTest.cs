using System.Collections.Generic;
using System.Linq;
using Bogus;
using Bogus.DataSets;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.InMemory.Test.Model;
using MongoDB.InMemory.Utils.Builder;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.InMemory.Test
{
    public class WhereBuilderTest
    {
        private readonly Randomizer _random = new Randomizer();
        private readonly Lorem _lorem = new Lorem("tr");
        private readonly ITestOutputHelper _output;

        public WhereBuilderTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private IEnumerable<Entity> GenerateEntities(int range, params Entity[] adds)
        {
            var result = Enumerable
                .Range(1, range)
                .Select(_ => new Entity
                {
                    Str = _lorem.Sentence(),
                    Int = _random.Number(),
                    Bool = _random.Bool(),
                    Int64 = _random.Long(),
                    Sub = _random.Bool()
                        ? new SubEntity
                        {
                            Str = _lorem.Sentence()
                        }
                        : null,
                    Subs = new[]
                    {
                        new SubEntity
                        {
                            Str = _lorem.Sentence()
                        }
                    }
                })
                .ToList();

            result.AddRange(adds);
            return result;
        }

        
        [Fact]
        public void GreaterThanComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Int: {$gt: 9}}");
            var func = WhereBuilder.Compile(filter);
            var expect = new Entity { Int = 11 };
            var items = new List<Entity> { expect, new Entity {Int = 9}}
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expect);
        }

        [Fact]
        public void GreaterThanOrEqualComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Int: {$gte: 9}}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new Entity { Int = 11 };
            var expectSecond = new Entity { Int = 9 };
            var items = new List<Entity> { expectFirst, expectSecond, new Entity { Int = 8 } }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

        [Fact]
        public void LessThanComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Int: {$lt: 9}}");
            var func = WhereBuilder.Compile(filter);
            var expect = new Entity { Int = 8 };
            var items = new List<Entity> { expect, new Entity { Int = 9 } }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expect);
        }

        [Fact]
        public void LessThanOrEqualComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Int: {$lte: 9}}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new Entity { Int = 8 };
            var expectSecond = new Entity { Int = 9 };
            var items = new List<Entity> { expectFirst, expectSecond, new Entity { Int = 18 } }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

        [Fact]
        public void EqualComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Str: \"test\"}");
            var func = WhereBuilder.Compile(filter);
            var expect = new Entity
            {
                Str = "test",
                Int = 11
            };
            var items = GenerateEntities(3, expect)
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expect);
        }

        [Fact]
        public void NotEqualComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Int: {$ne: 18}}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new Entity { Int = 8 };
            var expectSecond = new Entity { Int = 9 };
            var items = new List<Entity> { expectFirst, expectSecond, new Entity { Int = 18 } }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

        [Fact]
        public void InComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Int: {$in: [8,9]}}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new Entity { Int = 8 };
            var expectSecond = new Entity { Int = 9 };
            var items = new List<Entity> { expectFirst, expectSecond, new Entity { Int = 18 } }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

       
        [Fact]
        public void RegexInComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Val: {$in: [ /1/s, /2/s ]}}");
            var func = WhereBuilder.Compile(filter);
            var items = new[]
            {
                BsonDocument.Parse("{Val: 1}"),
                BsonDocument.Parse("{Val: \"1\"}"),
                BsonDocument.Parse("{Val: \"2\"}"),
                BsonDocument.Parse("{Val: 2}")
            };

            // Act
            var result = items.Where(func);

            // Assert
            result.Should().BeEquivalentTo(items[1], items[2]);
        }

        [Fact]
        public void RegexNotInComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Val: {$not: {$in: [ /1/s, /2/s ]}}}");
            var func = WhereBuilder.Compile(filter);
            var items = new[]
            {
                BsonDocument.Parse("{Val: \"3\"}"),
                BsonDocument.Parse("{Val: \"1\"}"),
                BsonDocument.Parse("{Val: \"2\"}"),
                BsonDocument.Parse("{Val: 2}")
            };

            // Act
            var result = items.Where(func);

            // Assert
            result.Should().BeEquivalentTo(items[0], items[3]);
        }

        [Fact]
        public void NotInComparisionTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Int: {$nin: [18, 19]}}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new Entity { Int = 8 };
            var expectSecond = new Entity { Int = 9 };
            var items = new List<Entity> { expectFirst, expectSecond, new Entity { Int = 18 }, new Entity { Int = 19 } }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

        [Theory]
        [InlineData("sub", true)]
        [InlineData("@@@@", false)]
        public void DotNotationTest(string findValue, bool shouldBeFound)
        {
            // Arrange
            var filter = BsonDocument.Parse($"{{\"Sub.Str\": \"{findValue}\"}}");
            var func = WhereBuilder.Compile(filter);
            var expect = new Entity
            {
                Str = "test",
                Int = 11,
                Sub = new SubEntity
                {
                    Str = "sub"
                }
            };
            var items = GenerateEntities(4, expect)
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items
                .Where(func)
                .Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped)
                .ToArray();

            // Assert
            result.Length.Should().Be(shouldBeFound ? 1 : 0);
            if (shouldBeFound)
                result[0].Should().BeEquivalentTo(expect);
        }

        [Theory]
        [InlineData("@sub", true)]
        [InlineData("@@@@", false)]
        public void DotNotationArrayTest_WithPosition(string findValue, bool shouldBeFound)
        {
            // Arrange
            var filter = BsonDocument.Parse($"{{\"Subs.1.Str\": \"{findValue}\"}}");
            var func = WhereBuilder.Compile(filter);
            var expect = new Entity
            {
                Str = "test",
                Int = 11,
                Subs = new[]
                {
                    new SubEntity
                    {
                        Str = "sub1"
                    },
                    new SubEntity
                    {
                        Str = "@sub"
                    }
                }
            };
            var items = GenerateEntities(4, expect)
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items
                .Where(func)
                .Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped)
                .ToArray();

            // Assert
            result.Length.Should().Be(shouldBeFound ? 1 : 0);
            if (shouldBeFound)
                result[0].Should().BeEquivalentTo(expect);
        }
        
        [Theory]
        [InlineData("@sub", true)]
        [InlineData("@@@@", false)]
        public void DotNotationArrayTest_WithoutPosition(string findValue, bool shouldBeFound)
        {
            // Arrange
            var filter = BsonDocument.Parse($"{{\"Subs.Str\": \"{findValue}\"}}");
            var func = WhereBuilder.Compile(filter);
            var expect = new Entity
            {
                Str = "test",
                Int = 11,
                Subs = new[]
                {
                    new SubEntity
                    {
                        Str = "sub1"
                    },
                    new SubEntity
                    {
                        Str = "@sub"
                    }
                }
            };
            var items = GenerateEntities(4, expect)
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items
                .Where(func)
                .Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped)
                .ToArray();

            // Assert
            result.Length.Should().Be(shouldBeFound ? 1 : 0);
            if (shouldBeFound)
                result[0].Should().BeEquivalentTo(expect);
        }

        [Fact]
        public void NotLogicalTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Int: {$not: {$in: [18, 19]}}}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new Entity { Int = 8 };
            var expectSecond = new Entity { Int = 9 };
            var items = new List<Entity> { expectFirst, expectSecond, new Entity { Int = 18 }, new Entity { Int = 19 } }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

        [Fact]
        public void OrLogicalTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{$or: [{Int: 8, Str:\"test\"}, {Int: 9}]}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new Entity { Int = 8, Str = "test"};
            var expectSecond = new Entity { Int = 9 };
            var items = new List<Entity> { expectFirst, expectSecond, new Entity { Int = 18, Str = "test" }, new Entity { Int = 19 } }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

        [Fact]
        public void AndLogicalTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{$and: [{Str:\"test\"}, {Int: {$in: [9,8]}}]}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new Entity { Int = 8, Str = "test" };
            var expectSecond = new Entity { Int = 9, Str = "test" };
            var items = new List<Entity> { expectFirst, expectSecond, new Entity { Int = 18, Str = "test" }, new Entity { Int = 19, Str = "Test" } }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items.Where(func).Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

        [Fact]
        public void ExistsElementTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Str: {$exists: true}}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new BsonDocumentWrapper(new Entity { Int = 8, Str = "test" });
            var expectSecond = new BsonDocumentWrapper(new Entity { Int = 9, Str = "test" });
            var items = new List<BsonDocumentWrapper>
            {
                expectFirst,
                expectSecond,
                new BsonDocumentWrapper(new Entity {Int = 18}),
                new BsonDocumentWrapper(new Entity {Int = 19})
            };
            items[2].Remove(nameof(Entity.Str));
            items[3].Remove(nameof(Entity.Str));

            // Act
            var result = items.Where(func);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

        [Fact]
        public void TypeElementTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Str: {$type: \"string\"}}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new BsonDocumentWrapper(new Entity { Int = 8, Str = "test" });
            var expectSecond = new BsonDocumentWrapper(new Entity { Int = 9, Str = "test" });
            var items = new List<BsonDocumentWrapper>
            {
                expectFirst,
                expectSecond,
                new BsonDocumentWrapper(new Entity {Int = 18}),
                new BsonDocumentWrapper(new Entity {Int = 19})
            };
            items[2][nameof(Entity.Str)] = 12;
            items[3][nameof(Entity.Str)] = false;

            // Act
            var result = items.Where(func);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond);
        }

        [Fact]
        public void TypeMultipleElementTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Str: {$type: [\"string\", \"double\"]}}");
            var func = WhereBuilder.Compile(filter);
            var expectFirst = new BsonDocumentWrapper(new Entity { Int = 8, Str = "test" });
            var expectSecond = new BsonDocumentWrapper(new Entity { Int = 9, Str = "test" });
            var expectThird = new BsonDocumentWrapper(new Entity { Int = 19, Str = "test" });
            var items = new List<BsonDocumentWrapper>
            {
                expectFirst,
                expectSecond,
                expectThird,
                new BsonDocumentWrapper(new Entity {Int = 18}),
                new BsonDocumentWrapper(new Entity {Int = 19})
            };
            expectThird[nameof(Entity.Str)] = 22.2d;
            items[3][nameof(Entity.Str)] = 12;
            items[4][nameof(Entity.Str)] = false;

            // Act
            var result = items.Where(func);

            // Assert
            result.Should().BeEquivalentTo(expectFirst, expectSecond, expectThird);
        }

        [Fact]
        public void RegexTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Str: /y/s}");
            var func = WhereBuilder.Compile(filter);
            var items = new[]
            {
                BsonDocument.Parse("{Str: \"string\"}"),
                BsonDocument.Parse("{Str: \"miray\"}"),
                BsonDocument.Parse("{Str: \"poyraz\"}")
            };

            // Act
            var result = items.Where(func).ToArray();

            // Assert
            result.Length.Should().Be(2);
            result.Should().Contain(items[1]);
            result.Should().Contain(items[2]);
        }

        [Fact]
        public void ElemMatchTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Subs: {$elemMatch: {Str: \"Sub1\", Sub: {$elemMatch: {Str: \"Sub2\"}}}}}");
            var func = WhereBuilder.Compile(filter);
            var expectation = new Entity
            {
                Int = 8, 
                Str = "test", 
                Subs = new []
                {
                    new SubEntity
                    {
                        Str = "Sub1",
                        Sub = new []
                        {
                            new SubEntity
                            {
                                Str = "Sub2"
                            }
                        }
                    }
                }
            };
            var items = new List<Entity>
                {
                    new Entity { Int = 18, Str = "test" }, 
                    expectation, 
                    new Entity { Int = 19 }
                }
                .Select(f => new BsonDocumentWrapper(f))
                .ToList();

            // Act
            var result = items
                .Where(func)
                .Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped)
                .ToArray();

            // Assert
            result.Length.Should().Be(1);
            result[0].Should().BeEquivalentTo(expectation);
        }

        [Fact]
        public void ElemMatchWithNotTest()
        {
            // Arrange
            var filter = BsonDocument.Parse("{Subs: {$not: {$elemMatch: {Str: \"Sub1\", Sub: {$elemMatch: {Str: \"Sub2\"}}}}}}");
            var func = WhereBuilder.Compile(filter);
            var unexpected = new Entity
            {
                Int = 8,
                Str = "test",
                Subs = new[]
                {
                    new SubEntity
                    {
                        Str = "Sub1",
                        Sub = new []
                        {
                            new SubEntity
                            {
                                Str = "Sub2"
                            }
                        }
                    }
                }
            };
            var items = new List<Entity>
                {
                    new Entity { Int = 18, Str = "test" }, 
                    unexpected, 
                    new Entity { Int = 19 }
                }
                .Select(f => new BsonDocumentWrapper(f))
                .ToArray();

            // Act
            var result = items
                .Where(func)
                .Select(f => (Entity)((BsonDocumentWrapper)f).Wrapped);

            // Assert
            result.Should().NotContain(unexpected);
        }
        
        [Fact]
        public void ArrayOfAnElement()
        {
            // Arrange
            var filter = BsonDocument.Parse("{IntArray: 2}");
            var func = WhereBuilder.Compile(filter);
            var expectation = new BsonDocumentWrapper(new Entity { IntArray = new []{1, 2, 3}});
            var items = new List<BsonDocumentWrapper>
            {
                new BsonDocumentWrapper(new Entity { IntArray = new []{11, 32, 43}}),
                expectation,
                new BsonDocumentWrapper(new Entity { IntArray = new []{51, 62, 73}})
            };

            // Act
            var result = items.FirstOrDefault(func);

            // Assert
            result.Should().BeEquivalentTo(expectation);
        }
        
        [Fact]
        public void ArrayEquals()
        {
            // Arrange
            var filter = BsonDocument.Parse("{IntArray: [1,2]}");
            var func = WhereBuilder.Compile(filter);
            var expectation = new BsonDocumentWrapper(new Entity { IntArray = new []{1, 2}});
            var items = new List<BsonDocumentWrapper>
            {
                new BsonDocumentWrapper(new Entity { IntArray = new []{2, 1}}),
                expectation,
                new BsonDocumentWrapper(new Entity { IntArray = new []{1,2,3}})
            };

            // Act
            var result = items.FirstOrDefault(func);

            // Assert
            result.Should().BeEquivalentTo(expectation);
        }
        
        
        [Fact]
        public void ArrayAllOperatorWithElemMatch()
        {
            // Arrange
            var filter = BsonDocument.Parse("{IntArray: {$all: [{$elemMatch:{$gt:0, $lt:3}}]}}");
            var func = WhereBuilder.Compile(filter);
            var expectation1 = new BsonDocumentWrapper(new Entity { IntArray = new []{1, 2}});
            var expectation2 = new BsonDocumentWrapper(new Entity { IntArray = new []{2, 1}});
            var items = new List<BsonDocumentWrapper>
            {
                expectation1,
                new BsonDocumentWrapper(new Entity { IntArray = new []{4,5}}),
                expectation2
            };

            // Act
            var result = items.Where(func);

            // Assert
            result.Should().BeEquivalentTo(expectation1, expectation2);
        }
        
        [Fact]
        public void ArrayAllOperatorWithElemMatchAndNot()
        {
            // Arrange
            var filter = BsonDocument.Parse("{IntArray: {$not: {$all: [{$elemMatch:{$gt:0, $lt:3}}]}}}");
            var func = WhereBuilder.Compile(filter);
            var expectation = new BsonDocumentWrapper(new Entity {IntArray = new[] {4, 5}});
            var items = new List<BsonDocumentWrapper>
            {
                new BsonDocumentWrapper(new Entity { IntArray = new []{1, 2}}),
                expectation,
                new BsonDocumentWrapper(new Entity { IntArray = new []{2, 1}})
            };

            // Act
            var result = items
                .Where(func)
                .ToArray();

            // Assert
            result.Length.Should().Be(1);
            result[0].Should().BeEquivalentTo(expectation);
        }
        
        [Fact]
        public void ArrayAllOperator()
        {
            // Arrange
            var filter = BsonDocument.Parse("{IntArray: {$all: [1,2]}}");
            var func = WhereBuilder.Compile(filter);
            var expectation1 = new BsonDocumentWrapper(new Entity { IntArray = new []{1, 2}});
            var expectation2 = new BsonDocumentWrapper(new Entity { IntArray = new []{2, 1}});
            var items = new List<BsonDocumentWrapper>
            {
                expectation1,
                new BsonDocumentWrapper(new Entity { IntArray = new []{2,3,4}}),
                expectation2
            };

            // Act
            var result = items.Where(func);

            // Assert
            result.Should().BeEquivalentTo(expectation1, expectation2);
        }
        
        [Fact]
        public void ArrayFilterWithBsonObject()
        {
            // Arrange
            var filter = BsonDocument.Parse("{IntArray: {$gt:0, $lt:3}}");
            var func = WhereBuilder.Compile(filter);
            var expectation1 = new BsonDocumentWrapper(new Entity { IntArray = new []{1, 2}});
            var expectation2 = new BsonDocumentWrapper(new Entity { IntArray = new []{2, 1}});
            var items = new List<BsonDocumentWrapper>
            {
                expectation1,
                new BsonDocumentWrapper(new Entity { IntArray = new []{4,5}}),
                expectation2
            };

            // Act
            var result = items.Where(func);

            // Assert
            result.Should().BeEquivalentTo(expectation1, expectation2);
        }
    }
}
