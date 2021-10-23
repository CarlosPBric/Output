﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XOutput.DependencyInjection
{
    public class ApplicationContext
    {
        private static readonly ApplicationContext global = new ApplicationContext();
        public static ApplicationContext Global => global;

        static ApplicationContext()
        {
            global.Resolvers.Add(Resolver.CreateSingleton(global));
        }

        private readonly List<Resolver> resolvers = new List<Resolver>();
        public List<Resolver> Resolvers => resolvers;
        private readonly ISet<Type> constructorResolvedTypes = new HashSet<Type>();
        private readonly TypeFinder typeFinder = new TypeFinder();

        public void Discover()
        {
            lock (lockObj)
            {
                var types = typeFinder.GetAllTypes(a => a.FullName.StartsWith("XOutput"));
                foreach (var type in types)
                {
                    if (!constructorResolvedTypes.Contains(type))
                    {
                        Resolvers.AddRange(GetConstructorResolvers(type));
                        constructorResolvedTypes.Add(type);
                    }
                }
            }
        }

        public void Discover(IEnumerable<Assembly> assemblies)
        {
            lock (lockObj)
            {
                var types = typeFinder.GetAllTypes(a => assemblies.Contains(a));
                foreach (var type in types)
                {
                    if (!constructorResolvedTypes.Contains(type))
                    {
                        Resolvers.AddRange(GetConstructorResolvers(type));
                        constructorResolvedTypes.Add(type);
                    }
                }
            }
        }

        private readonly object lockObj = new object();

        public T Resolve<T>(bool required = true)
        {
            lock (lockObj)
            {
                return (T)Resolve(new Dependency
                {
                    Type = typeof(T),
                    Required = required,
                });
            }
        }

        private object Resolve(Type type)
        {
            if (!constructorResolvedTypes.Contains(type))
            {
                Resolvers.AddRange(GetConstructorResolvers(type));
                constructorResolvedTypes.Add(type);
            }
            List<Resolver> currentResolvers = resolvers.Where(r => r.CreatedType.IsAssignableFrom(type)).ToList();
            if (currentResolvers.Count == 0)
            {
                throw new NoValueFoundException(type);
            }
            if (currentResolvers.Count > 1)
            {
                throw new MultipleValuesFoundException(type, currentResolvers);
            }
            Resolver resolver = currentResolvers[0];
            return resolver.Create(resolver.GetDependencies().Select(d => Resolve(d)).ToArray());
        }

        private object Resolve(Dependency dependency)
        {
            try
            {
                return Resolve(dependency.Type);
            }
            catch (NoValueFoundException)
            {
                if (dependency.IsEnumerable)
                {
                    Type itemType = dependency.Type.GenericTypeArguments[0];
                    var ResolveEnumerableType = GetType().GetMethod("ResolveEnumerableType", BindingFlags.NonPublic | BindingFlags.Instance, null,
                        new Type[] { typeof(Type) }, new ParameterModifier[] { }).MakeGenericMethod(itemType);
                    var result = (IEnumerable)ResolveEnumerableType.Invoke(this, new object[] { dependency.Type });
                    if (result.GetEnumerator().MoveNext())
                    {
                        return result;
                    }
                }
                if (dependency.Required)
                {
                    throw;
                }
                if (dependency.Type.IsValueType)
                {
                    return Activator.CreateInstance(dependency.Type);
                }
                return null;
            }
        }

        private IEnumerable<T> ResolveEnumerableType<T>(Type enumerableType)
        {
            var result = ResolveAll<T>();
            Type iEnumerableT = typeof(IEnumerable<>).MakeGenericType(typeof(T));
            return (IEnumerable<T>)enumerableType.GetConstructor(new Type[] { iEnumerableT }).Invoke(new object[] { result });
        }

        private IEnumerable<Resolver> GetConstructorResolvers(Type type)
        {
            return type.GetConstructors()
                .Where(m => m.GetCustomAttributes(true).OfType<ResolverMethodAttribute>().Any())
                .ToDictionary(m => m, m => m.GetCustomAttributes(true).OfType<ResolverMethodAttribute>().First())
                .Select(constructor =>
                {
                    Func<object[], object> creator = ((parameters) => constructor.Key.Invoke(parameters));
                    return Resolver.Create(creator, constructor.Key, type, constructor.Value.Scope);
                })
                .ToList();
        }

        public List<T> ResolveAll<T>(bool allowEmpty = true)
        {
            lock (lockObj)
            {
                List<Resolver> currentResolvers = resolvers.Where(r => typeof(T).IsAssignableFrom(r.CreatedType)).ToList();
                var results = currentResolvers.Select(r => r.Create(r.GetDependencies().Select(d => Resolve(d)).ToArray())).OfType<T>().ToList();
                if (!allowEmpty && !results.Any())
                {
                    throw new NoValueFoundException(typeof(T));
                }
                return results;
            }
        }

        public ApplicationContext WithResolvers(params Resolver[] tempResolvers)
        {
            ApplicationContext newContext = new ApplicationContext();
            newContext.Resolvers.AddRange(Resolvers);
            newContext.Resolvers.AddRange(tempResolvers);
            return newContext;
        }

        public ApplicationContext WithSingletons(params object[] instances)
        {
            ApplicationContext newContext = new ApplicationContext();
            foreach (var type in constructorResolvedTypes)
            {
                newContext.constructorResolvedTypes.Add(type);
            }
            newContext.Resolvers.AddRange(Resolvers);
            newContext.Resolvers.AddRange(instances.Select(i => Resolver.CreateSingleton(i)));
            return newContext;
        }

        public void AddFromConfiguration(Type type)
        {
            lock (lockObj)
            {
                foreach (var method in type.GetMethods()
                    .Where(m => m.ReturnType != typeof(void))
                    .Where(m => m.IsStatic)
                    .Where(m => m.GetCustomAttributes(true).OfType<ResolverMethodAttribute>().Any())
                    .ToDictionary(m => m, m => m.GetCustomAttributes(true).OfType<ResolverMethodAttribute>().First()))
                {
                    Func<object[], object> creator = ((parameters) => method.Key.Invoke(null, parameters));
                    var createdType = method.Key.ReturnType;
                    resolvers.Add(Resolver.Create(creator, method.Key, createdType, method.Value.Scope));
                }
            }
        }

        public void Close()
        {
            lock (lockObj)
            {
                foreach (var singleton in resolvers.Where(r => r.IsResolvedSingleton).Where(r => typeof(IDisposable).IsAssignableFrom(r.CreatedType)).Select(r => r.Create(new object[0])).OfType<IDisposable>())
                {
                    singleton.Dispose();
                }
                Resolvers.Clear();
            }
        }
    }
}
