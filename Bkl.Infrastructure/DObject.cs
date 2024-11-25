using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace Bkl.Infrastructure
{
    [Serializable]
    public class ExtensionDynamicObject : DynamicObject, IDictionary<string, object>, ICloneable, INotifyPropertyChanged
    {
        public ExtensionDynamicObject()
        {

        }
        public ExtensionDynamicObject(object obj) : base()
        {
            var properties = obj.GetType().GetProperties();
            foreach (System.Reflection.PropertyInfo info in properties)
            {
                var value = info.GetValue(obj, null);
                _values.Add(info.Name, value);
            }
        }

        private readonly IDictionary<string, object> _values = new Dictionary<string, object>();

        #region IDictionary<String, Object> 接口实现

        public object this[string key]
        {
            get { return _values[key]; }

            set
            {
                _values[key] = value;


                OnPropertyChanged(key);
            }
        }

        public int Count
        {
            get { return _values.Count; }
        }

        public bool IsReadOnly
        {
            get { return _values.IsReadOnly; }
        }

        public ICollection<string> Keys
        {
            get { return _values.Keys; }
        }

        public ICollection<object> Values
        {
            get { return _values.Values; }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _values.Add(item);
        }

        public void Add(string key, object value)
        {
            _values.Add(key, value);
        }

        public void Clear()
        {
            _values.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _values.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _values.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            _values.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return _values.Remove(item);
        }

        public bool Remove(string key)
        {
            return _values.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return _values.TryGetValue(key, out value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        #endregion

        #region ICloneable 接口实现

        public object Clone()
        {
            var clone = new ExtensionDynamicObject() as IDictionary<string, object>;

            foreach (var key in _values.Keys)
            {
                clone[key] = _values[key] is ICloneable ? ((ICloneable)_values[key]).Clone() : _values[key];
            }

            return clone;
        }

        #endregion

        #region INotifyPropertyChanged 接口实现

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /// <summary>  
        /// 获取属性值  
        /// </summary>  
        /// <param name="propertyName"></param>  
        /// <returns></returns>  
        public object GetPropertyValue(string propertyName)
        {
            if (_values.ContainsKey(propertyName) == true)
            {
                return _values[propertyName];
            }
            return null;
        }

        /// <summary>  
        /// 设置属性值  
        /// </summary>  
        /// <param name="propertyName"></param>  
        /// <param name="value"></param>  
        public void SetPropertyValue(string propertyName, object value)
        {
            if (_values.ContainsKey(propertyName) == true)
            {
                _values[propertyName] = value;
            }
            else
            {
                _values.Add(propertyName, value);
            }
        }

        /// <summary>  
        /// 实现动态对象属性成员访问的方法，得到返回指定属性的值  
        /// </summary>  
        /// <param name="binder"></param>  
        /// <param name="result"></param>  
        /// <returns></returns>  
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = GetPropertyValue(binder.Name);
            return result != null;
        }

        /// <summary>  
        /// 实现动态对象属性值设置的方法。  
        /// </summary>  
        /// <param name="binder"></param>  
        /// <param name="value"></param>  
        /// <returns></returns>  
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            SetPropertyValue(binder.Name, value);
            return true;
        }

        ///// <summary>
        ///   http://blog.csdn.net/hawksoft/article/details/7534332
        ///// 动态对象动态方法调用时执行的实际代码  
        ///// </summary>  
        ///// <param name="binder"></param>  
        ///// <param name="args"></param>  
        ///// <param name="result"></param>  
        ///// <returns></returns>  
        //public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        //{
        //    var theDelegateObj = GetPropertyValue(binder.Name) as DelegateObj;
        //    if (theDelegateObj == null || theDelegateObj.CallMethod == null)
        //    {
        //        result = null;
        //        return false;
        //    }
        //    result = theDelegateObj.CallMethod(this, args);
        //    return true;
        //}

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            return base.TryInvoke(binder, args, out result);
        }
      
    }

    public sealed class DObject : DynamicObject
    {
        private readonly Dictionary<string, object> _properties;

        public DObject(object obj)
        {
            _properties = new Dictionary<string, object>();
            var properties = obj.GetType().GetProperties();
            foreach (System.Reflection.PropertyInfo info in properties)
            {
                var value = info.GetValue(obj, null);
                _properties.Add(info.Name, value);
            }
        } 
        public object ToObject(ref object obj)
        {
            obj=Activator.CreateInstance(obj.GetType());
            var properties = obj.GetType().GetProperties();
            foreach (System.Reflection.PropertyInfo info in properties)
            {
                 info.SetValue(obj, _properties[info.Name]);
            }
            return obj;
        }

        public DObject(Dictionary<string, object> properties)
        {
            _properties = properties;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _properties.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_properties.ContainsKey(binder.Name))
            {
                result = _properties[binder.Name];
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (_properties.ContainsKey(binder.Name))
            {
                _properties[binder.Name] = value;
                return true;
            }
            else
            {
                _properties.Add(binder.Name, value);
                return true;
            }
        }
    }
}
