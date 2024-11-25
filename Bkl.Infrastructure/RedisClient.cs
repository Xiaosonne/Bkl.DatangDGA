using StackExchange.Redis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bkl.Infrastructure
{
	using System;
	using System.Collections.Generic;



	public class RedisClient : IRedisClient, IDisposable
	{
		public ConnectionMultiplexer connection;
		public IDatabase database;
		private bool disposed;
		public RedisClient(ConnectionMultiplexer conn, long db = -1)
		{
			connection = conn;
			database = conn.GetDatabase();
		}
		#region List
		public IEnumerable<string> GetList(string listId)
		{

			var len = (int)database.ListLength(listId);
			foreach (var i in Enumerable.Range(0, len))
			{
				yield return (string)database.ListGetByIndex(listId, i);
			}
		}

		public string DequeueItemFromList(string listId)
		{
			string str = database.ListLeftPop(listId);
			if (str.Empty())
				return default(String);
			return str;
		}

		public void EnqueueItemOnList(string listId, string Item, int timeout = 0)
		{
			database.ListRightPush(listId, Item);
			if (timeout > 0)
				database.KeyExpire(listId, DateTime.UtcNow.AddSeconds(timeout));
		}
		#endregion

		#region Hash 
		public RedisValue Get(string key)
		{
			return database.StringGet(key);
		}
		public bool Set(string key, RedisValue t, int timeout = 0)
		{
			if (timeout > 0)
				return database.StringSet(key, t, TimeSpan.FromSeconds(timeout));
			return database.StringSet(key, t);
		}
		#endregion

		#region HashSet 
		public List<string> GetKeysFromHash(string hashId, string pattern = null)
		{
			return database.HashKeys(hashId).Select(p => (string)p).ToList();
		}

		public RedisValue GetValueFromHash(string hashId, string key)
		{
			return database.HashGet(hashId, (RedisValue)key);
		}

		public Dictionary<string, RedisValue> GetValuesFromHash(string hashId)
		{
			var entries = database.HashGetAll(hashId);
			return entries.ToDictionary(p => p.Name.ToString(), p => p.Value);
		}
		public Dictionary<string, RedisValue> GetValuesFromHash(string hashId, string keypattern)
		{
			if (keypattern != null)
			{
				var entries = database.HashScan(hashId, keypattern);
				return entries.ToDictionary(p => p.Name.ToString(),
				   p => p.Value);
			}
			else
			{
				var entries = database.HashGetAll(hashId);
				return entries.ToDictionary(p => p.Name.ToString(), p => p.Value);
			}
		}
		public Dictionary<string, RedisValue> GetValuesFromHash(string hashId, string[] keys)
		{
			Dictionary<string, RedisValue> dic = new Dictionary<string, RedisValue>();
			foreach (var item in keys)
			{
				var val = database.HashGet(hashId, item.ToJson());
				dic.Add(item, val);
			}
			return dic;
		}

		public bool RemoveEntryFromHash(string hashId, string key)
		{
			return database.HashDelete(hashId, key);
		}

		public void SetEntryInHash(string hashId, string key, RedisValue value)
		{
			database.HashSet(hashId, key, value);
		}

		public bool SetEntryInHashIfNotExists(string hashId, string key, RedisValue value)
		{
			if (!database.HashExists(hashId, key))
			{
				var ret = database.HashSet(hashId, key, value); ;
				return ret;
			}
			return false;
		}

		public void SetRangeInHash(string hashId, IEnumerable<KeyValuePair<string, RedisValue>> keyValuePairs)
		{
			database.HashSet(hashId, keyValuePairs.Select(p => new HashEntry(p.Key, p.Value)).ToArray());
		}
		#endregion

		#region Set
		public List<string> GetAllItemsFromSet(string v)
		{
			var vals = database.SetMembers(v);
			return vals.Select(p => (string)p).ToList();
		}

		public void RemoveItemFromSet(string v1, string v2)
		{
			database.SetRemove(v1, v2);
		}

		public void RemoveItemsFromSet(string v, List<string> devices)
		{
			database.SetRemove(v, devices.Select(p => (RedisValue)p).ToArray());
		}

		public void AddItemToSet(string v1, string v2)
		{
			database.SetAdd(v1, v2);
		}
		#endregion

		#region SortedSet
		public List<string> GetAllItemsFromSortedSet(string v)
		{
			return database.SortedSetRangeByScore(v).Select(p => (string)p).ToList();
		}
		public void AddRangeToSortedSet(string sortedSetId, List<string> item, double score)
		{
			database.SortedSetAdd(sortedSetId, item.Select(p => new SortedSetEntry(p, score)).ToArray());
		}
		public void AddItemToSortedSet(string sortedSetId, string item, double score)
		{
			database.SortedSetAdd(sortedSetId, item, score);
		}
		public double? GetItemScoreInSortedSet(string sortedSetId, string item)
		{
			return database.SortedSetScore(sortedSetId, item);
		}

		public List<string> GetRangeFromSortedSetByHighestScore(string sortedSetId, double start, double stop)
		{
			return database.SortedSetRangeByScore(sortedSetId, start, stop)
				.Select(p => (string)p)
				.ToList();
		}

		public void RemoveRangeFromSortedSetByScore(string sortedSetId, double start, double stop)
		{
			database.SortedSetRemoveRangeByScore(sortedSetId, start, stop);
		}

		public void RemoveItemsFromSortedSet(string sortedSetId, List<string> items)
		{
			database.SortedSetRemove(sortedSetId, items.Select(p => (RedisValue)p).ToArray());
		}

		public void RemoveItemFromSortedSet(string sortedSetId, string item)
		{
			database.SortedSetRemove(sortedSetId, item);
		}
		#endregion

		#region Key TTL
		public TimeSpan? GetTimeToLive(string redisKey)
		{
			return database.KeyTimeToLive(redisKey);
		}

		#endregion

		public bool Remove(string key)
		{
			return database.KeyDelete(key);
		}

		public void Expire(string key, int timeout = 0)
		{
			if (timeout > 0)
				database.KeyExpire(key, DateTime.UtcNow.AddSeconds(timeout));
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			database = null;

		}

		public bool SetIfNotExists(string key, RedisValue t, int timeout = 0)
		{
			if (timeout > 0)
				return database.StringSet(key, t , TimeSpan.FromSeconds(timeout), When.NotExists);
			return database.StringSet(key, t, null, When.NotExists);

		}

		//public void ChangeDatabase(long db, Action<IRedisClient> invokeMethod, [CallerMemberName] string mem1 = "")
		//{
		//    using (var redis = new RedisClient(connection, db))
		//    {
		//        (invokeMethod, redis);
		//    }
		//}

		//public T ChangeDatabase<T>(long db, Func<IRedisClient, T> invokeMethod, [CallerMemberName] string mem1 = "")
		//{
		//    using (var redis = new RedisClient(connection, db))
		//    {
		//        return (invokeMethod, redis);
		//    }
		//}

		public long IncrementValueInHash(string v1, string v2, int v3)
		{
			return database.HashIncrement(v1, v2, v3);
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			return base.ToString();
		}
		IServer _server;
		public List<string> Keys(string pattern)
		{
            if(_server==null){
                _server=connection.GetServer(connection.GetEndPoints()[0]);
            }
			return _server.Keys(pattern: pattern).Select(s => (string)s).ToList();
		}
	}
}
