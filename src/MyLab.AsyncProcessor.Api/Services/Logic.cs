﻿using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MyLab.AsyncProcessor.Api.Tools;
using MyLab.AsyncProcessor.Sdk;
using MyLab.Mq;
using MyLab.Redis;
using MyLab.Redis.ObjectModel;

namespace MyLab.AsyncProcessor.Api.Services
{
    public class Logic
    {
        private readonly IMqPublisher _mqPublisher;
        private readonly IRedisService _redis;
        private readonly AsyncProcessorOptions _options;

        public Logic(
            IRedisService redis, 
            IMqPublisher mqPublisher,
            IOptions<AsyncProcessorOptions> options)
            : this(redis, options.Value)
        {
            _mqPublisher = mqPublisher;
        }

        public Logic(IRedisService redis, AsyncProcessorOptions options)
        {
            _redis = redis;
            _options = options;
        }

        public async Task<string> RegisterNewRequestAsync()
        {
            var newId = Guid.NewGuid().ToString("N");
            
            var statusKey = _redis.Db().Hash(CreateKeyName(newId, "status"));

            var initialStatus = new RequestStatus
            {
                Step = ProcessStep.Pending
            };

            await initialStatus.WriteToRedis(statusKey);
            await statusKey.ExpireAsync(_options.MaxIdleTime);

            return newId;
        }

        public void SendRequestToProcessor(string id, CreateRequest createRequest)
        {
            var msgPayload = new QueueRequestMessage
            {
                Id = id,
                Content = createRequest.Content
            };

            var msg =new OutgoingMqEnvelop<QueueRequestMessage>
            {
                Message = new MqMessage<QueueRequestMessage>(msgPayload),
                PublishTarget = new PublishTarget
                {
                    Exchange = _options.QueueExchange,
                    Routing = createRequest.Routing
                }
            };

            _mqPublisher.Publish(msg);
        }

        public async Task<RequestStatus> GetStatusAsync(string id)
        {
            var key = await GetStatusKey(id);

            return await RequestStatusTools.ReadFromRedis(key);
        }

        public async Task SetBizStepAsync(string id, string bizStep)
        {
            var key = await GetStatusKey(id);

            await RequestStatusTools.SaveBizStep(bizStep, key);
            await key.ExpireAsync(_options.MaxIdleTime);
        }

        public async Task<RequestResult> GetResultAsync(string id)
        {
            var statusKey = await GetStatusKey(id);
            var resultMime = await RequestStatusTools.ReadResultMimeType(statusKey);

            var resultKey = GetResultKey(id);
            var resultContent = await resultKey.GetAsync();

            return new RequestResult(resultMime, resultContent);
        }

        public async Task SetResultAsync(string id, byte[] content, string mimeType)
        {
            var statusKey = await GetStatusKey(id);

            await RequestStatusTools.SaveResultInfo(content.Length, mimeType, statusKey);

            var resultKey = GetResultKey(id);
            var strContent = ContentToString(content, mimeType);

            await resultKey.SetAsync(strContent);

            await statusKey.ExpireAsync(_options.MaxStoreTime);
            await resultKey.ExpireAsync(_options.MaxStoreTime);
        }

        string CreateKeyName(string id, string suffix) => _options.RedisKeyPrefix.TrimEnd(':') + ':' + id + ":" + suffix;

        string ContentToString(byte[] content, string mimeType)
        {
            switch (mimeType)
            {
                case "application/octet-stream":
                {
                    return Convert.ToBase64String(content);
                }
                case "text/plain":
                case "application/json":
                {
                    return Encoding.UTF8.GetString(content);
                }
                default:
                {
                    throw new UnsupportedMediaTypeException(mimeType);
                }
            }
        }

        async Task<HashRedisKey> GetStatusKey(string id)
        {
            var statusKeyName = CreateKeyName(id, "status");
            var statusKey = _redis.Db().Hash(statusKeyName);

            if (!await statusKey.ExistsAsync())
                throw new RequestNotFoundException(id);

            return statusKey;
        }

        StringRedisKey GetResultKey(string id)
        {
            var resultKeyName = CreateKeyName(id, "result");
            return _redis.Db().String(resultKeyName);
        }
    }
}
