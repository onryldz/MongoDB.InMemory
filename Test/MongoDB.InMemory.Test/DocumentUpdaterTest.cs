using FluentAssertions;
using MongoDB.Bson;
using MongoDB.InMemory.Utils;
using Xunit;

namespace MongoDB.InMemory.Test
{
    public class DocumentUpdaterTest
    {
        [Fact]
        public void When_set_key_not_contain_in_document_Expect_add()
        {
            // Arrange
            var setInfo = BsonDocument.Parse("{$set: {NewKey: 9}}");
            var document = BsonDocument.Parse("{Str:\"test\", Bool: true, Int: 1}");
            var expected = BsonDocument.Parse("{Str:\"test\", Bool: true, Int: 1, NewKey: 9}");

            // Act
            DocumentUpdater.Update(document, setInfo);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }

        [Fact]
        public void When_set_one_of_the_key_contain_and_other_is_not_Expect_correct_result()
        {
            // Arrange
            var setInfo = BsonDocument.Parse(@"{$set: {ExistsKey: ""UP"", NewKey: 9}}");
            var document = BsonDocument.Parse(@"{ExistsKey: ""OK""}");
            var expected = BsonDocument.Parse(@"{ExistsKey: ""UP"", NewKey: 9}");

            // Act
            DocumentUpdater.Update(document, setInfo);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }

        [Theory]
        [InlineData("Arr.1", "9", "[0,1]", "[0,9]")]
        [InlineData("Arr.0", "9", "[0,1]", "[9,1]")]
        [InlineData("Arr.0", "9", "[0]", "[9]")]
        [InlineData("Arr.0", "1", "[]", "[1]")]
        [InlineData("Arr.3", "8", "[]", "[null, null, null, 8]")]
        [InlineData("Arr.0.c", "true", "[{c:1}]", "[{c:true}]")]
        public void When_set_array_type_with_dot_notation_key_Expect_correct_result(string key, string newValue, string currentValue, string expectedValue)
        {
            // Arrange
            var setInfo = BsonDocument.Parse($"{{$set: {{\"{key}\": {newValue}}}}}");
            var document = BsonDocument.Parse($"{{Str:\"test\", Bool: true, Int: 1, Arr: {currentValue}}}");
            var expected = BsonDocument.Parse($"{{Str:\"test\", Bool: true, Int: 1, Arr: {expectedValue}}}");

            // Act
            DocumentUpdater.Update(document, setInfo);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }

        [Theory]
        [InlineData("Obj.a", "1", "{a:9}", "{a:1}")]
        [InlineData("Obj.0", "9", @"{""0"":6}", @"{""0"":9}")]
        [InlineData("Obj.a.b", "true", "{}", "{a:{b:true}}")]
        [InlineData("Obj.c.d", "false", "{c:{d:true}}", "{c:{d:false}}")]
        [InlineData("Obj.c.d.0", "2", "{c:{d:[1]}}", "{c:{d:[2]}}")]
        public void When_set_with_dot_notation_key_Expect_correct_result(string key, string newValue, string currentValue, string expectedValue)
        {
            // Arrange
            var setInfo = BsonDocument.Parse($"{{$set: {{\"{key}\": {newValue}}}}}");
            var document = BsonDocument.Parse($"{{Str:\"test\", Bool: true, Int: 1, Obj: {currentValue}}}");
            var expected = BsonDocument.Parse($"{{Str:\"test\", Bool: true, Int: 1, Obj: {expectedValue}}}");

            // Act
            DocumentUpdater.Update(document, setInfo);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }
        
        [Fact]
        public void When_unset_key_in_document_Expect_remove_correct_keys()
        {
            // Arrange
            var unset = BsonDocument.Parse("{$unset: {Str: 1}}");
            var document = BsonDocument.Parse("{Str:\"test\", Bool: true, Int: 1}");
            var expected = BsonDocument.Parse("{Bool: true, Int: 1}");

            // Act
            DocumentUpdater.Update(document, unset);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }
        
        [Fact]
        public void When_pull_element_in_array_Expect_remove_correct_element()
        {
            // Arrange
            var pull = BsonDocument.Parse("{$pull: {ints: {$gt: 2, $lt:6}}}");
            var document = BsonDocument.Parse("{ints:[1,2,4,5,5,6,7]}");
            var expected = BsonDocument.Parse("{ints:[1,2,6,7]}");

            // Act
            DocumentUpdater.Update(document, pull);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }
        
        [Fact]
        public void When_pull_element_in_array_with_multiple_condition_Expect_remove_correct_element()
        {
            // Arrange
            var pull = BsonDocument.Parse(@"
            {
                $pull: 
                {
                    results: 
                    {
                        answers: { $elemMatch: { q: 2, a: { $gte: 8 } } }
                     } 
                } 
            }");
            
            var document = BsonDocument.Parse(@"
            {              
                results: 
                [
                    {
                        item: 'A',
                        score: 5,
                        answers: [ { q: 1, a: 4 }, { q: 2, a: 6 } ]
                    },
                    {
                        item: 'B',
                        score: 8,
                        answers: [ { q: 1, a: 8 }, { q: 2, a: 9 } ]
                    }
                ]
            }");
            
            var expected = BsonDocument.Parse(@"
            {              
                results: 
                [
                    {
                        item: 'A',
                        score: 5,
                        answers: [ { q: 1, a: 4 }, { q: 2, a: 6 } ]
                    }
                ]
            }");

            // Act
            DocumentUpdater.Update(document, pull);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }
        
        [Fact]
        public void When_pull_element_in_array_with_value_condition_Expect_remove_correct_element()
        {
            // Arrange
            var pull = BsonDocument.Parse("{$pull: {ints: 5}}");
            var document = BsonDocument.Parse("{ints:[1,2,4,5,5,6,7]}");
            var expected = BsonDocument.Parse("{ints:[1,2,4,6,7]}");

            // Act
            DocumentUpdater.Update(document, pull);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }
        
        [Fact]
        public void When_push_element_in_array_Expect_add_correct_element()
        {
            // Arrange
            var pull = BsonDocument.Parse("{$push: {ints: 8}}");
            var document = BsonDocument.Parse("{ints:[1,2,3,4,5,6,7]}");
            var expected = BsonDocument.Parse("{ints:[1,2,3,4,5,6,7,8]}");

            // Act
            DocumentUpdater.Update(document, pull);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }
        
        [Fact]
        public void When_push_element_in_array_with_each_modifier_Expect_add_correct_elements()
        {
            // Arrange
            var pull = BsonDocument.Parse("{$push: {ints: {$each: [8, 9]}}}");
            var document = BsonDocument.Parse("{ints:[1,2,3,4,5,6,7]}");
            var expected = BsonDocument.Parse("{ints:[1,2,3,4,5,6,7,8,9]}");

            // Act
            DocumentUpdater.Update(document, pull);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }
    }
}