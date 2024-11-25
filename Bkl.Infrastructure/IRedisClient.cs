using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bkl.Infrastructure
{
	public interface IRedisClient
	{

		List<string> Keys(string pattern);
		string DequeueItemFromList(string listId);
		void EnqueueItemOnList(string listId, string Item, int timeout = 0);
		IEnumerable<string> GetList(string listId);

		RedisValue Get(string key);
		bool Set(string key, RedisValue t, int timeout = 0);
		bool SetIfNotExists(string key, RedisValue t, int timeout = 0);

		List<string> GetKeysFromHash(string hashId, string pattern = null);
		RedisValue GetValueFromHash(string hashId, string key);
		Dictionary<string, RedisValue> GetValuesFromHash(string hashId);
		Dictionary<string, RedisValue> GetValuesFromHash(string hashId, string keypattern);
		Dictionary<string, RedisValue> GetValuesFromHash(string hashId, string[] keys);
		void SetEntryInHash(string hashId, string key, RedisValue value);
		bool SetEntryInHashIfNotExists(string hashId, string key, RedisValue value);
		void SetRangeInHash(string hashId, IEnumerable<KeyValuePair<string, RedisValue>> keyValuePairs);
		bool RemoveEntryFromHash(string hashId, string key);

		List<string> GetAllItemsFromSet(string setId);
		void RemoveItemFromSet(string setId, string item);
		void RemoveItemsFromSet(string setId, List<string> items);
		void AddItemToSet(string setId, string item);

		void AddRangeToSortedSet(string sortedSetId, List<string> item, double score);
		void AddItemToSortedSet(string sortedSetId, string item, double score);
		List<string> GetAllItemsFromSortedSet(string v);
		double? GetItemScoreInSortedSet(string sortedSetId, string item);
		List<string> GetRangeFromSortedSetByHighestScore(string sortedSetId, double start, double stop);
		void RemoveRangeFromSortedSetByScore(string sortedSetId, double start, double stop);
		void RemoveItemsFromSortedSet(string sortedId, List<string> devices);
		long IncrementValueInHash(string v1, string v2, int v3);
		void RemoveItemFromSortedSet(string sortedId, string item);

		TimeSpan? GetTimeToLive(string redisKey);
		bool Remove(string redisKey);
		void Expire(string redisKey, int timeoutSeconds = 0);
		void Dispose();

		//void ChangeDatabase(long db, Action<IRedisClient> invokeMethod, [CallerMemberName] string mem1 = "");
		//T ChangeDatabase<T>(long db, Func<IRedisClient, T> invokeMethod, [CallerMemberName] string mem1 = "");
	}
}