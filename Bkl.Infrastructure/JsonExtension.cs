using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Bkl.Infrastructure
{
    public static class JsonExtension
    {
        public static T JsonToObj<T>(this string json)
        {
            try
            {
                T t = JsonSerializer.Deserialize<T>(json);
                return t;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return default(T);
            }
        }
        public static string ToJson(this object obj, JsonSerializerOptions option = null)
        {
            return JsonSerializer.Serialize(obj, option);
        }
        public static dynamic DynamicJson(this string obj)
        {
            return JsonSerializer.Deserialize<dynamic>(obj);
        }

        public static string Base64Encode(this string str)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }
        public static string Base64Decode(this string str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }
    }

}
