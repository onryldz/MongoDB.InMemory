using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.InMemory.Utils.Builder;
using Xunit;

namespace MongoDB.InMemory.Test
{
    public class AggregateBuilderTest
    {
        [Fact]
        public void MatchTest()
        {
            // Arrange
            var pipelines = new []{ BsonDocument.Parse("{$match: {Int: 9}}") };
            var collections = new[]
            {
                BsonDocument.Parse("{Int: 9}"),
                BsonDocument.Parse("{Int: 8}")
            };

            // Act
            var result = AggregateBuilder
                .Apply(pipelines, collections)
                .ToArray();

            // Assert
            result.Length.Should().Be(1);
            result[0]["Int"].AsInt32.Should().Be(9);
        }

        [Fact]
        public void LimitTest()
        {
            // Arrange
            var pipelines = new[] { BsonDocument.Parse("{$limit: 1}") };
            var collections = new[]
            {
                BsonDocument.Parse("{Int: 9}"),
                BsonDocument.Parse("{Int: 8}")
            };

            // Act
            var result = AggregateBuilder
                .Apply(pipelines, collections)
                .ToArray();

            // Assert
            result.Length.Should().Be(1);
            result[0]["Int"].AsInt32.Should().Be(9);
        }

        [Fact]
        public void SkipTest()
        {
            // Arrange
            var pipelines = new[] { BsonDocument.Parse("{$skip: 1}") };
            var collections = new[]
            {
                BsonDocument.Parse("{Int: 9}"),
                BsonDocument.Parse("{Int: 8}")
            };

            // Act
            var result = AggregateBuilder
                .Apply(pipelines, collections)
                .ToArray();

            // Assert
            result.Length.Should().Be(1);
            result[0]["Int"].AsInt32.Should().Be(8);
        }

        [Fact]
        public void SortTest()
        {
            // Arrange
            var pipelines = new[] { BsonDocument.Parse("{$sort: {Int: -1, Str: 1}}") };
            var collections = new[]
            {
                BsonDocument.Parse("{Int: 9, Str: \"B\"}"),
                BsonDocument.Parse("{Int: 9, Str: \"A\"}"),
                BsonDocument.Parse("{Int: 8, Str: \"B\"}"),
                BsonDocument.Parse("{Int: 8, Str: \"A\"}")
            };

            // Act
            var result = AggregateBuilder
                .Apply(pipelines, collections)
                .ToArray();

            // Assert
            result.Length.Should().Be(4);
            result.Should().BeEquivalentTo(
                collections[1], // 9 - A
                collections[3], // 8 - A
                collections[0], // 9 - B
                collections[2] // 8 - B
            );
        }

        [Fact]
        public void ProjectTest()
        {
            // Arrange
            var pipelines = new[] { BsonDocument.Parse("{$project: {Identity: \"$key\", Name: \"$value\", static: 1, Static2: \"Test\"}}") };
            var collections = new[]
            {
                BsonDocument.Parse("{key: 1, value: \"Miray\"}"),
                BsonDocument.Parse("{key: 2, value: \"Poyraz\"}")
            };

            // Act
            var result = AggregateBuilder
                .Apply(pipelines, collections)
                .ToArray();

            // Assert
            result.Should().BeEquivalentTo(
                BsonDocument.Parse("{Identity: 1, Name: \"Miray\", static: 1, Static2: \"Test\"}"),
            BsonDocument.Parse("{Identity: 2, Name: \"Poyraz\", static: 1, Static2: \"Test\"}")
            );
        }
    }
}