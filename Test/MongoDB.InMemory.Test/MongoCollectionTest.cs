using FluentAssertions;
using MongoDB.Driver;
using MongoDB.InMemory.Test.Model;
using Xunit;

namespace MongoDB.InMemory.Test
{
    public class MongoCollectionTest
    {
        [Fact]
        public void When_find_on_collection_Expect_correct_result()
        {
            // Arrange
            var client = InMemoryClient.Create();
            var collection = client.GetDatabase("foo").GetCollection<Entity>("entity");
            collection.InsertMany(new Entity[]
            {
                new Entity
                {
                    Str =  "Test"
                },
                new Entity
                {
                    Str =  "Test2"
                }
            });

            // Act
            var result = collection.Find(f => f.Str == "Test2").ToList();

            // Assert
            result.Should().BeEquivalentTo(new Entity
            {
                Str = "Test2"
            });
        }

        [Fact]
        public void When_updateOne_on_collection_Expect_correct_result()
        {
            // Arrange
            var client = InMemoryClient.Create();
            var collection = client.GetDatabase("foo").GetCollection<Entity>("entity");
            collection.InsertMany(new[]
            {
                new Entity
                {
                    Int64 = 1,
                    Str =  "Test"
                },
                new Entity
                {
                    Int64 = 2,
                    Str =  "Test2"
                },
                new Entity
                {
                    Int64 = 3,
                    Str =  "Test2"
                }
            });
            var updateSet = Builders<Entity>.Update
                .Set(f => f.Int, 12);

            // Act
            var result = collection.UpdateOne(f => f.Str == "Test2", updateSet);
            var entity = collection
                .Find(f => f.Int == 12)
                .ToList();

            // Assert
            result.ModifiedCount.Should().Be(1);
            entity.Should().BeEquivalentTo(new Entity
            {
                Int64 = 2,
                Int = 12,
                Str = "Test2"
            });
        }
        
        [Fact]
        public void When_updateMany_on_collection_Expect_correct_result()
        {
            // Arrange
            var client = InMemoryClient.Create();
            var collection = client.GetDatabase("foo").GetCollection<Entity>("entity");
            collection.InsertMany(new[]
            {
                new Entity
                {
                    Str =  "Test"
                },
                new Entity
                {
                    Str =  "Test2"
                },
                new Entity
                {
                Str =  "Test3"
                }
            });
            var updateSet = Builders<Entity>.Update
                .Set(f => f.Int, 12);

            // Act
            var result = collection.UpdateMany(f => f.Str == "Test2" || f.Str == "Test3", updateSet);
            var entity = collection
                .Find(f => f.Int == 12)
                .ToList();

            // Assert
            result.ModifiedCount.Should().Be(2);
            entity.Should().BeEquivalentTo(new Entity
                {
                    Int = 12,
                    Str = "Test2"
                },
                new Entity
                {
                    Int = 12,
                    Str = "Test3"
                });
        }
        
    }
}