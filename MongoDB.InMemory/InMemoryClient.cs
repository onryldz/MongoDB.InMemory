using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.InMemory.Proxies;
using MongoDB.InMemory.Utils;
using Moq;

namespace MongoDB.InMemory
{
    public static class InMemoryClient
    {
        public static IMongoClient Create()
        {
            var mockClient = new Mock<IMongoClient>();
            mockClient.SetupGet(m => m.Settings).Returns(new MongoClientSettings());
            mockClient
                .Setup(m => m.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                .Returns((string databaseName, MongoDatabaseSettings databaseSettings) =>
                {
                    var databaseNamespace = new DatabaseNamespace(databaseName);
                    var settings = new MongoDatabaseSettings();
                    settings.ApplyDefaultValues(mockClient.Object.Settings);

                    var cluster = new Mock<ICluster>().Object;
                    return CreateMongoDatabaseImpl(mockClient.Object, databaseNamespace, settings, cluster);
                });
            return mockClient.Object;
        }

        private static IMongoDatabase CreateMongoDatabaseImpl(IMongoClient client, DatabaseNamespace databaseNamespace, MongoDatabaseSettings settings, ICluster cluster)
        {
            var type = typeof(IMongoClient).Assembly.GetType("MongoDB.Driver.MongoDatabaseImpl");
            var mockOperationExecutor = new OperationExecutorProxy { Client = client };
            var moeProxy = mockOperationExecutor.GenerateProxy();

            var constructor = type.GetConstructors().First(f => f.GetParameters().Any());
            var args = new Expression[] {
                Expression.Constant(client, typeof(IMongoClient)),
                Expression.Constant(databaseNamespace, typeof(DatabaseNamespace)),
                Expression.Constant(settings, typeof(MongoDatabaseSettings)),
                Expression.Constant(cluster, typeof(ICluster)),
                Expression.Constant(moeProxy, TypeDefinitions.IOperationExecutorType)
            };
            var lambda = Expression.Lambda<Func<IMongoDatabase>>(Expression.New(constructor, args));
            var func = lambda.Compile();

            return func();
        }
        
        private static void ApplyDefaultValues(this MongoDatabaseSettings settings, MongoClientSettings databaseSettings)
        {
            settings.ReadConcern = databaseSettings.ReadConcern;
            settings.ReadEncoding = databaseSettings.ReadEncoding;
            settings.ReadPreference = databaseSettings.ReadPreference;
            settings.WriteConcern = databaseSettings.WriteConcern;
            settings.WriteEncoding = databaseSettings.WriteEncoding;
        }

    }
}