using System;
using System.Linq;
using System.Reflection;

namespace MongoDB.InMemory.Extensions
{
    internal static class TypeExtensions
    {
        public static bool IsAssignableToGenericInterfaceType(this Type givenType, Type intf)
        {
            return givenType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == intf);
        }

        public static Type IsAssignableToGenericType(this Type givenType, params Type[] genericType)
        {
            while (true)
            {
                var interfaceTypes = givenType.GetInterfaces();

                foreach (var it in interfaceTypes)
                {
                    if (!it.IsGenericType) continue;

                    var index = Array.IndexOf(genericType, it.GetGenericTypeDefinition());
                    if (index > -1) return genericType[index];
                }

                if (givenType.IsGenericType)
                {
                    var index = Array.IndexOf(genericType, givenType.GetGenericTypeDefinition());
                    if (index > -1) return genericType[index];
                }

                var baseType = givenType.BaseType;
                if (baseType == null) return null;
                givenType = baseType;
            }
        }

        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            return IsAssignableToGenericType(givenType, new[] { genericType }) != null;
        }

        public static object InvokeGeneric(this MethodInfo method, Type genericArg0, object instance, params object[] args)
        {
            return method
                .MakeGenericMethod(genericArg0)
                .Invoke(instance, args);
        }
    }
}