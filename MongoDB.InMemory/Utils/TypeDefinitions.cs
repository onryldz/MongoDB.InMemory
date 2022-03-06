using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using MongoDB.InMemory.Extensions;
using MongoDB.InMemory.Proxies;

namespace MongoDB.InMemory.Utils
{
    internal static class TypeDefinitions
    {
        public static readonly Type CoreServerSessionType;
        public static readonly Type ClientSessionHandleType;
        public static readonly Type IOperationExecutorType;
        public static readonly IReadOnlyDictionary<Type, MethodInfo> WriteOperationMethodMapping;
        public static readonly IReadOnlyDictionary<Type, MethodInfo> ReadOperationMethodMapping;

        static TypeDefinitions()
        {
            var type = typeof(IMongoClient).Assembly.GetType("MongoDB.Driver.MongoDatabaseImpl");
            var constructor = type.GetConstructors().First(f => f.GetParameters().Any()).GetParameters();
            CoreServerSessionType = typeof(ICoreServerSession).Assembly.GetType("MongoDB.Driver.CoreServerSession");
            ClientSessionHandleType = typeof(IClientSessionHandle).Assembly.GetType("MongoDB.Driver.ClientSessionHandle");
            IOperationExecutorType = constructor.First(f => f.ParameterType.FullName == "MongoDB.Driver.IOperationExecutor").ParameterType;
           
            var methods = typeof(OperationExecutorProxy).GetMethods();
            WriteOperationMethodMapping = methods
                .Where(f => f.GetCustomAttribute<MapAttribute>() != null)
                .Select(f => (
                    Type: f.GetParameters()
                        .FirstOrDefault(q => q.ParameterType.IsAssignableToGenericInterfaceType(typeof(IWriteOperation<>)))
                        ?.ParameterType
                        .GetGenericTypeDefinition(),
                    Method: f
                ))
                .Where(f => f.Type != null)
                .ToDictionary(k => k.Type, v => v.Method);

            ReadOperationMethodMapping = methods
                .Where(f => f.GetCustomAttribute<MapAttribute>() != null)
                .Select(f => (
                    Type: f.GetParameters()
                        .FirstOrDefault(q => q.ParameterType.IsAssignableToGenericInterfaceType(typeof(IReadOperation<>)))
                        ?.ParameterType
                        .GetGenericTypeDefinition(),
                    Method: f
                ))
                .Where(f => f.Type != null)
                .ToDictionary(k => k.Type, v => v.Method);
        }
    }
}