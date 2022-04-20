using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using MongoDB.InMemory.Extensions;
using MongoDB.InMemory.Utils;
using MongoDB.InMemory.Utils.Builder;
using Moq;

namespace MongoDB.InMemory.Proxies
{
    internal class OperationExecutorProxy
    {
        private readonly ConcurrentDictionary<string, SynchronizedCollection<BsonValue>> _collectionDict = new ConcurrentDictionary<string, SynchronizedCollection<BsonValue>>();

        public IMongoClient Client { get; set; }
        
        private TResult ExecuteReadOperation<TResult>(IReadBinding binding, IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            var type = operation.GetType();
            var operationType = type.IsAssignableToGenericType(TypeDefinitions.ReadOperationMethodMapping.Keys.ToArray());
            var documentType = type.GenericTypeArguments[0];
            if (operationType != null)
                return (TResult)TypeDefinitions.ReadOperationMethodMapping[operationType].InvokeGeneric(documentType, this, binding, operation);

            return (TResult)(object)new Utils.AsyncCursor<TResult>(null);
        }

        public Task<TResult> ExecuteReadOperationAsync<TResult>(IReadBinding binding, IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            try
            {
                var result = ExecuteReadOperation(binding, operation, cancellationToken);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                var tcs = new TaskCompletionSource<TResult>();
                tcs.TrySetException(ex);
                return tcs.Task;
            }
        }

        [Map]
        public IAsyncCursor<TDocument> ReadFindOperation<TDocument>(IReadBinding binding, FindOperation<TDocument> operation) where TDocument : class
        {
            var collection = GetCollection(operation.CollectionNamespace);
            var predicate = WhereBuilder.Compile(operation.Filter);
            var results = collection
                .Where(f => f != null && predicate(f))
                .Select(f => BsonSerializer.Deserialize<TDocument>(f.AsBsonDocument));

            return new Utils.AsyncCursor<TDocument>(results);
        }

        [Map]
        public IAsyncCursor<TDocument> ReadAggregateOperation<TDocument>(IReadBinding binding, AggregateOperation<TDocument> operation) where TDocument : class
        {
            var collection = GetCollection(operation.CollectionNamespace);
            var results = AggregateBuilder
                .Apply(operation.Pipeline, collection)
                .Select(f => BsonSerializer.Deserialize<TDocument>(f.AsBsonDocument));

            return new Utils.AsyncCursor<TDocument>(results);
        }

        #region Write Operation

        private TResult ExecuteWriteOperation<TResult>(IWriteBinding binding, IWriteOperation<TResult> operation,
            CancellationToken cancellationToken)
        {
            if (operation is BulkMixedWriteOperation bulkWrite)
                return (TResult)(object)WriteOperationBulk(binding, bulkWrite);

            var type = operation.GetType();
            var operationType = type.IsAssignableToGenericType(TypeDefinitions.WriteOperationMethodMapping.Keys.ToArray());
            var documentType = type.GenericTypeArguments[0];
            if (operationType != null)
                return (TResult)TypeDefinitions.WriteOperationMethodMapping[operationType].InvokeGeneric(documentType, this, binding, operation);

            var resultType = typeof(TResult).GetNestedType("Acknowledged");
            return (TResult)Activator.CreateInstance(resultType);
        }

        private BulkWriteOperationResult WriteOperationBulk(IWriteBinding binding, BulkMixedWriteOperation operation)
        {
            var collection = GetCollection(operation.CollectionNamespace);
            var requests = operation.Requests.ToArray();
            var insertCount = WriteInsertOperations(requests, collection);
            var updateCount = WriteUpdateOperations(requests, collection);
            var deleteCount = WriteDeleteOperations(requests, collection);

            return new BulkWriteOperationResult.Acknowledged
            (
                requests.Length,
                insertCount + updateCount + deleteCount,
                deleteCount,
                insertCount,
                updateCount,
                requests,
                new List<BulkWriteOperationUpsert>()
            );
        }

        private static int WriteUpdateOperations(IEnumerable<WriteRequest> requests, ICollection<BsonValue> collection)
        {
            var updateCount = 0;
            var updateRequests = requests
                .Where(f => f is UpdateRequest)
                .Cast<UpdateRequest>()
                .ToArray();

            var entitiesInCollection = updateRequests
                .Select(updateRequest =>
                {
                    var predicate = WhereBuilder.Compile(updateRequest.Filter);
                    var result = (replaceDocuments: collection.Where(predicate), updareRequest: updateRequest);
                    if (!updateRequest.IsMulti)
                        result.replaceDocuments = result.replaceDocuments.Take(1);
                    return result;
                }).ToArray();

            foreach (var (replaceDocuments, updateRequest) in entitiesInCollection)
            {
                foreach (var replaceItem in replaceDocuments)
                {
                    DocumentUpdater.Update(replaceItem, updateRequest.Update);
                    updateCount++;
                }
            }
           
            return updateCount;
        }

        private static int WriteDeleteOperations(IEnumerable<WriteRequest> requests, SynchronizedCollection<BsonValue> collection)
        {
            var deleteCount = 0;
            foreach (var request in requests)
            {
                if (!(request is DeleteRequest deleteRequest))
                    continue;
                var predicate = WhereBuilder.Compile(deleteRequest.Filter);
                foreach (var entity in collection)
                {
                    if (!predicate(entity))
                        continue;

                    deleteCount++;
                    collection.Remove(entity);
                }
            }

            return deleteCount;
        }

        private static int WriteInsertOperations(IEnumerable<WriteRequest> requests, ICollection<BsonValue> collection)
        {
            var insertRequests = requests.Where(f => f is InsertRequest)
                .Cast<InsertRequest>()
                .Select(f => f.Document is BsonDocumentWrapper wrapper ? wrapper : null)
                .Where(f => f != null)
                .ToArray();

            foreach (var entity in insertRequests)
                collection.Add(entity);

            return insertRequests.Length;
        }

        [Map]
        public TDocument WriteOperationFindOneDelete<TDocument>(IWriteBinding binding, FindOneAndDeleteOperation<TDocument> operation)
        {
            var collection = GetCollection(operation.CollectionNamespace);
            var predicate = WhereBuilder.Compile(operation.Filter);
            var index = collection.FindIndex(f => predicate(f));
            if (index <= -1) 
                return default;

            var result = collection[index];
            collection.RemoveAt(index);
            return BsonSerializer.Deserialize<TDocument>(result.AsBsonDocument);
        }

        [Map]
        public TDocument WriteOperationFindOneReplace<TDocument>(IWriteBinding binding, FindOneAndReplaceOperation<TDocument> operation)
        {
            var collection = GetCollection(operation.CollectionNamespace);
            var predicate = WhereBuilder.Compile(operation.Filter);
            var index = collection.FindIndex(f => predicate(f));
            if (index <= -1) 
                return default;

            var replacement = operation.Replacement;
            collection[index] = replacement;
            return BsonSerializer.Deserialize<TDocument>(replacement);
        }

        [Map]
        public TDocument WriteOperationFindOneUpdate<TDocument>(IWriteBinding binding, FindOneAndUpdateOperation<TDocument> operation)
        {
            var collection = GetCollection(operation.CollectionNamespace);
            var predicate = WhereBuilder.Compile(operation.Filter);
            var index = collection.FindIndex(f => predicate(f));
            if (index <= -1)
                return default;

            var update = operation.Update.AsBsonDocument;
            collection[index] = update;
            return BsonSerializer.Deserialize<TDocument>(update);
        }

        public Task<TResult> ExecuteWriteOperationAsync<TResult>(IWriteBinding binding, IWriteOperation<TResult> operation, CancellationToken cancellationToken)
        {
            try
            {
                var result = ExecuteWriteOperation(binding, operation, cancellationToken);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                var tcs = new TaskCompletionSource<TResult>();
                tcs.TrySetException(ex);
                return tcs.Task;
            }
        }

        #endregion

        private SynchronizedCollection<BsonValue> GetCollection(CollectionNamespace nameSpace)
        {
            var collectionKey = nameSpace.DatabaseNamespace.DatabaseName + "@" + nameSpace.CollectionName;
            if (_collectionDict.TryGetValue(collectionKey, out var bag)) 
                return bag;

            bag = new SynchronizedCollection<BsonValue>();
            _collectionDict.TryAdd(collectionKey, bag);
            return bag;
        }

        private IClientSessionHandle StartImplicitSession(CancellationToken cancellationToken)
        {
            var cluster = Mock.Of<ICluster>();
            var options = new ClientSessionOptions();
            var coreSessionOptions = new CoreSessionOptions(options.CausalConsistency ?? true, true, options.DefaultTransactionOptions);
            var coreServerSession = (ICoreServerSession)Activator.CreateInstance(TypeDefinitions.CoreServerSessionType);
            var coreSession = new CoreSession(cluster, coreServerSession, coreSessionOptions);
            var coreSessionHandle = new CoreSessionHandle(coreSession);
            return (IClientSessionHandle)Activator.CreateInstance(TypeDefinitions.ClientSessionHandleType, new object[] { Client, options, coreSessionHandle });
        }

        public Task<IClientSessionHandle> StartImplicitSessionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(StartImplicitSession(cancellationToken));
        }

        public object GenerateProxy()
        {
            var generator = new ProxyGenerator();
            return generator.CreateInterfaceProxyWithoutTarget(TypeDefinitions.IOperationExecutorType, new InterceptorX(this));
        }

        private class InterceptorX : IInterceptor
        {
            private readonly OperationExecutorProxy _executor;
            private readonly Type _executorType;

            public InterceptorX(OperationExecutorProxy executor)
            {
                _executor = executor;
                _executorType = _executor.GetType();
            }

            private MethodInfo GetMatchingMethod(MethodBase methodInfo)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var genericArgs = methodInfo.GetGenericArguments();
                var parameters = methodInfo.GetParameters();

                var targetMethods = _executorType.GetMethods(flags).Where(f => f.Name == methodInfo.Name && f.GetParameters().Length == parameters.Length);
                if (methodInfo.IsGenericMethod)
                    targetMethods = targetMethods.Select(f => f.MakeGenericMethod(genericArgs));

                return targetMethods.Single(f =>
                    f.GetParameters()
                        .All(q => parameters
                            .Any(z =>
                                z.Name == q.Name &&
                                z.Position == q.Position &&
                                z.ParameterType == q.ParameterType
                            )
                        )
                    );
            }

            public void Intercept(Castle.DynamicProxy.IInvocation invocation)
            {
                var method = GetMatchingMethod(invocation.Method);
                invocation.ReturnValue = method.Invoke(_executor, invocation.Arguments);
            }
        }
    }
}