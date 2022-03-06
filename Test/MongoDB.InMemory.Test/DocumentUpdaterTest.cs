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
            DocumentUpdater.Set(document, setInfo);

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
            DocumentUpdater.Set(document, setInfo);

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
            DocumentUpdater.Set(document, setInfo);

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
            DocumentUpdater.Set(document, setInfo);

            // Assert
            document.Should().BeEquivalentTo(expected);

        }

    }
}