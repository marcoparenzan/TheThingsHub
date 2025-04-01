using UnifiedNamespaceLib.Models;

namespace UnifiedNamespaceLib.Services;
 
public interface IRetainedMessageService: IService
{
    Task AddAsync(MqttRetainedMessageModel value);
    Task<MqttRetainedMessageModel[]> GetAsync(params string[] keys);
}
