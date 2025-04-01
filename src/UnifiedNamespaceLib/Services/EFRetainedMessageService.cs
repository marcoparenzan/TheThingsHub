
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using UnifiedNamespaceLib.Models;
using UnifiedNamespaceLib.Models.Database;

namespace UnifiedNamespaceLib.Services;

public class EFRetainedMessageService : IRetainedMessageService
{
    private UnifiedNamespaceContext dbContext;
    private Models.Database.ValueType[] valueTypes;
    private Models.Database.ValueType jsonValueType;
    private HybridCache hybridCache;

    public EFRetainedMessageService(IConfiguration config, IServiceProvider sp, ILogger<EFRetainedMessageService> logger, HybridCache hybridCache)
    {
        this.dbContext = sp.GetService<UnifiedNamespaceContext>();
        this.valueTypes = dbContext.ValueTypes.ToArray();
        this.jsonValueType = valueTypes.Single(xx => xx.Id == 1);
        this.hybridCache = hybridCache;
    }

    public async Task AddAsync(MqttRetainedMessageModel value)
    {
        var ts = DateTimeOffset.Now;
        var topic = dbContext.Topics.SingleOrDefault(xx => xx.Value == value.Topic);
        if (topic is null)
        {
            topic = new Topic
            {
                Value = value.Topic
            };
            dbContext.Topics.Add(topic);
        }
        var lastTopicValue = dbContext.TopicValues.SingleOrDefault(xx => xx.TopicId == topic.Id && xx.To == null);
        var newTopicValue = new TopicValue
        {
            Topic = topic,
            From = ts,
            ValueType = jsonValueType,
            Value = JsonSerializer.Serialize(value)
        };
        dbContext.TopicValues.Add(newTopicValue);
        if (lastTopicValue is not null) lastTopicValue.To = ts;

        try
        {
            await hybridCache.SetAsync(value.Topic, value);

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public async Task<MqttRetainedMessageModel[]> GetAsync(params string[] keys)
    {
        var items = new List<MqttRetainedMessageModel>();

        if (keys.Length == 0)
        {
            keys = dbContext.Topics.Select(xx => xx.Value).ToArray();
        }

        foreach (var key in keys)
        {
            var item = await hybridCache.GetOrCreateAsync(key, async ct =>
            {
                var xxx = dbContext.TopicValues.SingleOrDefault(xx => xx.Topic.Value == key && xx.To == null);
                var itemXX = JsonSerializer.Deserialize<MqttRetainedMessageModel>(xxx.Value);
                return itemXX;
            });
            items.Add(item);
        }

        return items.ToArray();
    }
}
