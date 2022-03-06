using System.Linq;
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
        public void When_update_on_collection_Expect_correct_result()
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
                }
            });
            var updateSet = Builders<Entity>.Update
                .Set(f => f.Int, 12);

            // Act
            collection.UpdateOne(f => f.Str == "Test2", updateSet);
            var entity = collection.Find(f => f.Int == 12).FirstOrDefault();

            // Assert
            entity.Should().BeEquivalentTo(new Entity
            {
                Int = 12,
                Str = "Test2"
            });
        }
    }
}