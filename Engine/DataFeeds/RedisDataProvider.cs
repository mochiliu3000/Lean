using QuantConnect.Interfaces;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Redis;
using Ionic.Crc;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    class RedisDataProvider : IDataProvider
    {
        // TODO: put this in config file
        // TODO: this may not in folder "usa"
        private string _dataFolder = "../../../Data/equity/usa";
        private string _connString = "localhost:6379";
        private IRedisClientsManager _redisManager;

        public RedisDataProvider()
        {
            _redisManager = new RedisManagerPool(_connString);
        }

        /// <summary>
        /// Retrieves data from Redis to be used in an algorithm
        /// </summary>
        /// <param name="key">A string to fetch the value from Redis, it looks like "Timestamp:Name:Resolution:Type", 
        /// for instance "20131007:spy:minute:trade". The corresponding Csv is "20131004_spy_minute_trade.csv". 
        /// The corresponding Zipfile is "../../../Data/equity\\usa\\minute\\spy\\20131004_trade.zip"</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public Stream Fetch(string key)
        {
            try
            {
                using (var redis = _redisManager.GetClient())
                {
                    var redisCsvObj = redis.As<CsvObj>();
                    if (!redisCsvObj.ContainsKey(key))
                    {
                        // Key does not exist in Redis, find it locally
                        Log.Trace("RedisDataProvider.Fetch(): The specified Key was not found in Redis: {0}", key);
                        // Generate zipfile name from redis key
                        var fileName = GenerateFileName(key);
                        if (!File.Exists(fileName))
                        {
                            Log.Error("RedisDataProvider.Fetch(): The specified file was not found Locally: {0}", key);
                            return null;
                        }

                        // Iterate local file and store its content into Redis
                        var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                        return fileStream;
                    }
                    else
                    {
                        Log.Trace("RedisDataProvider.Fetch(): Found the specified Key-Value in Redis: {0}", key);
                        var content = redisCsvObj.GetValue(key).Content;
                        return new MemoryStream(Encoding.UTF8.GetBytes(content));
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error("Error connecting to Redis:" + exception);
                return null;
            }
        }

        /// <summary>
        /// Store filestream content into Redis
        /// </summary>
        /// <param name="key">A string to index the content in Redis, it looks like "Timestamp:Name:Resolution:Type"</param>
        /// <param name="zipFileStream">A Stream that contains the csv content within a zipfile to store</param>
        /// <returns>void</returns>
        public void Store(string key, Stream zipFileStream)
        {
            using (var redis = _redisManager.GetClient())
            {
                var redisCsvObj = redis.As<CsvObj>();
                using (var sr = new StreamReader(zipFileStream))
                {
                    string content = null;
                    string s = null;
                    while ((s = sr.ReadLine()) != null)
                    {
                        content += s + "\n";
                    }

                    var csvObj = new CsvObj()
                    {
                        Content = content,
                        CreateDate = DateTime.UtcNow
                    };
                    redisCsvObj.SetValue(key, csvObj);
                }
            }
        }


        private string GenerateFileName(string key)
        {
            // 20131004:spy:minute:trade => ../../../Data/equity\\usa\\minute\\spy\\20131004_trade.zip
            // TODO: the zipfile name is different for different type
            string[] keys = key.Split(':');
            if (keys.Length != 4)
            {
                Log.Error("GenerateFileName(): The redis key is invalid: {0}", key);
                return null;
            }
            else
            {
                return _dataFolder + Path.DirectorySeparatorChar + keys[2] + Path.DirectorySeparatorChar 
                    + keys[1] + Path.DirectorySeparatorChar + keys[0] + "_" + keys[3] + ".zip";
            }
        }
    }

    

    class CsvObj
    {
        public string Content { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
