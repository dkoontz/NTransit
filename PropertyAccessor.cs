// This code was originally written by Thibaud 
// http://thibaud60.blogspot.com/2010/10/fast-property-accessor-without-dynamic.html

using System;
using System.Collections.Generic;
using System.Reflection;

namespace NTransit {
	public interface IPropertyAccessor {
		PropertyInfo PropertyInfo { get; }

		string Name { get; }

		object GetValue(object source);

		void SetValue(object source, object value);
	}

	public static class PropertyAccessor {
		class PropertyWrapper<TObject, TValue> : IPropertyAccessor {
			public string Name { get { return propertyInfo.Name; } }

			public PropertyInfo PropertyInfo { get { return propertyInfo; } }

			PropertyInfo propertyInfo;
			Func<TObject, TValue> getMethod;
			Action<TObject, TValue> setMethod;

			public PropertyWrapper(PropertyInfo propertyInfo) {
				this.propertyInfo = propertyInfo;

				MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
				MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);

				getMethod = (Func<TObject, TValue>)Delegate.CreateDelegate(typeof(Func<TObject, TValue>), getMethodInfo);
				setMethod = (Action<TObject, TValue>)Delegate.CreateDelegate(typeof(Action<TObject, TValue>), setMethodInfo);
			}

			object IPropertyAccessor.GetValue(object source) {
				return getMethod((TObject)source);
			}

			void IPropertyAccessor.SetValue(object source, object value) {
				setMethod((TObject)source, (TValue)value);
			}
		}

		static Dictionary<PropertyInfo, IPropertyAccessor> cache = new Dictionary<PropertyInfo, IPropertyAccessor>();

		public static IPropertyAccessor GetFastAccessor(PropertyInfo propertyInfo) {
			IPropertyAccessor result;
			lock (cache) {
				if (!cache.TryGetValue(propertyInfo, out result)) {
					result = CreateAccessor(propertyInfo);
					cache.Add(propertyInfo, result);
				}
			}
			return result;
		}

		public static IPropertyAccessor CreateAccessor(PropertyInfo propertyInfo) {
			return (IPropertyAccessor)Activator.CreateInstance(typeof(PropertyWrapper<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType), propertyInfo);
		}
	}
}