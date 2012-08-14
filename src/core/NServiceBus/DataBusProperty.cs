using System;
using System.Runtime.Serialization;

namespace NServiceBus
{
    [Serializable]
	public class DataBusProperty<T> : IDataBusProperty, ISerializable where T : class
    {
        T value;
    	
        public DataBusProperty(T value)
        {
            SetValue(value);
        }

        public DataBusProperty(SerializationInfo info, StreamingContext context)
        {
            Key = info.GetString("Key");
			HasValue = info.GetBoolean("HasValue");
        }

        public string Key { get; set; }
		public bool HasValue { get; set; }

    	public T Value
        {
            get
            {
                return value;
            }
        }

      
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Key", Key);
			info.AddValue("HasValue", HasValue);
		}

		public void SetValue(object valueToSet)
		{
			value = valueToSet as T;

			if (value != null)
				HasValue = true;
		}

    	
    	public object GetValue()
		{
			return Value;
		}

    }

	public interface IDataBusProperty
	{
		string Key { get; set; }
		object GetValue();
		void SetValue(object value);
		bool HasValue { get; set; }
	}
}