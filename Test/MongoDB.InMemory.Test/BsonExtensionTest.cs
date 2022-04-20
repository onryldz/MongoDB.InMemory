using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.InMemory.Extensions;
using MongoDB.InMemory.Extensions.Model.DotNotation;
using Xunit;

namespace MongoDB.InMemory.Test
{
    public class BsonExtensionTest
    {
        [Fact]
        public void DotNotation_PositionalOperator_Test()
        {
            // Arrange
            var json = BsonDocument.Parse("{Array:[1,2,3,4]}");
            const string pathExpr = "Array.$";
            var resolvedItems = new List<int>();

            // Act
            json.ResolveElementWithDotNotation(pathExpr, path =>
            {
                if(path is DotNotationArray pathArray)
                    resolvedItems.Add(pathArray.Element.AsInt32);
            }, false);

            // Assert
            resolvedItems.Should().BeEquivalentTo(1, 2, 3, 4);
        }
        
        [Fact]
        public void DotNotation_PositionalOperator_With_BsonObject_Test()
        {
            // Arrange
            var json = BsonDocument.Parse("{Array:[{id: 1}, {id:2}, {id:3}, {id:4}]}");
            const string pathExpr = "Array.$.id";
            var resolvedItems = new List<int>();

            // Act
            json.ResolveElementWithDotNotation(pathExpr, path =>
            {
                if(path is DotNotationDocument pathDocument)
                    resolvedItems.Add(pathDocument.Element.AsInt32);
            }, false);

            // Assert
            resolvedItems.Should().BeEquivalentTo(1, 2, 3, 4);
        }
        
        [Fact]
        public void DotNotation_PositionalOperator_With_Nested_BsonObject_Test()
        {
            // Arrange
            var json = BsonDocument.Parse(@"
                {
                    Array: 
                    [
                        { 
                            SubArray: 
                            [
                                {id: 1}, 
                                {id: 2}
                            ]
                        },
                        { 
                            SubArray: 
                            [
                                {id: 5}, 
                                {id: 6}
                            ]
                        },
                        { 
                            SubArray: 
                            [
                                {id: 3}, 
                                {id: 4}
                            ]
                        }
                    ]
                }");
            
            const string pathExpr = "Array.$.SubArray.$.id";
            var resolvedItems = new List<int>();

            // Act
            json.ResolveElementWithDotNotation(pathExpr, path =>
            {
                if(path is DotNotationDocument pathDocument)
                    resolvedItems.Add(pathDocument.Element.AsInt32);
            }, false);

            // Assert
            resolvedItems.Should().BeEquivalentTo(1, 2, 5, 6, 3, 4);
        }
        
        [Fact]
        public void DotNotation_WithoutPositionalOperator_With_BsonObject_Test()
        {
            // Arrange
            var json = BsonDocument.Parse("{Array:[{id: 1}, {id:2}, {id:3}, {id:4}]}");
            const string pathExpr = "Array.id";
            var resolvedItems = new List<int>();

            // Act
            json.ResolveElementWithDotNotation(pathExpr, path =>
            {
                if(path is DotNotationDocument pathDocument)
                    resolvedItems.Add(pathDocument.Element.AsInt32);
            }, false);

            // Assert
            resolvedItems.Should().BeEquivalentTo(1, 2, 3, 4);
        }
        
        [Fact]
        public void DotNotation_WithoutPositionalOperator_With_Nested_BsonObject_Test()
        {
            // Arrange
            var json = BsonDocument.Parse(@"
                {
                    Array: 
                    [
                        { 
                            SubArray: 
                            [
                                {id: 1}, 
                                {id: 2}
                            ]
                        },
                        { 
                            SubArray: 
                            [
                                {id: 5}, 
                                {id: 6}
                            ]
                        },
                        { 
                            SubArray: 
                            [
                                {id: 3}, 
                                {id: 4}
                            ]
                        }
                    ]
                }");
            
            const string pathExpr = "Array.SubArray.id";
            var resolvedItems = new List<int>();

            // Act
            json.ResolveElementWithDotNotation(pathExpr, path =>
            {
                if(path is DotNotationDocument pathDocument)
                    resolvedItems.Add(pathDocument.Element.AsInt32);
            }, false);

            // Assert
            resolvedItems.Should().BeEquivalentTo(1, 2, 5, 6, 3, 4);
        }
    }
}