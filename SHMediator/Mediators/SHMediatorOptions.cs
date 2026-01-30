using Microsoft.Extensions.DependencyInjection;
using SH.Mediator.Exceptions;
using SH.Mediator.SHMediatorInterceptors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SH.Mediator
{
    public class SHMediatorOptions
    {
        public SHMediatorOptions()
        {
        }

        public bool UseLoggingInterceptor
        {
            get; set;
        } = true;
        public bool UseFluentValidationInterceptor
        {
            get;
            set;
        } = true;
        public List<Type> Interceptors { get; } = new();

        public SHMediatorOptions UseSHMediatorInterceptor(Type type)
        {
          
            var interfaceType = typeof(IMediatorInterceptor<>);
            if (!ImplementsOpenGeneric(type, typeof(IMediatorInterceptor<>)))
            {
                throw new MediatorException($"{type.FullName} 必须实现 IMediatorInterceptor 接口");
            }

            if (!Interceptors.Contains(type))
            {
                Interceptors.Insert(0, type);
            }
            return this;
        }
        private static bool ImplementsOpenGeneric(Type candidate, Type openGenericInterface)
        {
            if (candidate == null) return false;
            if (openGenericInterface == null) return false;
            if (!openGenericInterface.IsInterface || !openGenericInterface.IsGenericTypeDefinition)
            {
                throw new ArgumentException("openGenericInterface 必须是开放泛型接口类型，例如 typeof(IMediatorInterceptor<>)。", nameof(openGenericInterface));
            }

            // 1) 直接接口
            if (candidate.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface))
            {
                return true;
            }

            // 2) 递归查基类（处理通过基类间接实现接口的情况）
            var baseType = candidate.BaseType;
            return baseType != null && baseType != typeof(object) && ImplementsOpenGeneric(baseType, openGenericInterface);
        }

        public SHMediatorOptions UseSHMediatorInterceptor<T>()
        {
            // 判断 T 是否实现了 IMediatorInterceptor<TResponse>
            var type = typeof(T);
            if (!ImplementsOpenGeneric(type, typeof(IMediatorInterceptor<>)))
            {
                throw new MediatorException($"{type.FullName} 必须实现 IMediatorInterceptor 接口");
            }
            if (!Interceptors.Contains(type))
            {
                Interceptors.Insert(0, type);
            }
            return this;
        }
    }

}
