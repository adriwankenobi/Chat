using System;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApi.Controllers.Common
{
    public static class PartitionHelper
    {
        /// <summary>
        /// Constructs a reverse proxy URL for a given service.
        /// Example: http://localhost:19081/Chat/ChatData/
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static Uri GetProxyAddress(Uri serviceName)
        {
            return new Uri($"http://localhost:19081{serviceName.AbsolutePath}");
        }

        public static Uri GetProxyAddress(ServiceContext context)
        {
            Uri serviceName = ChatApi.GetChatDataServiceName(context);
            return GetProxyAddress(serviceName);
        }

        /// <summary>
        /// Creates a partition key from the given name.
        /// Uses the zero-based numeric position in the alphabet of the first letter of the name (0-25).
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static long GetPartitionKey(string name)
        {
            return Char.ToUpper(name.First()) - 'A';
        }

        public static string GetProxyUrl(Uri proxyAddress, string path, long partitionKey)
        {
            return $"{proxyAddress}{path}?PartitionKey={partitionKey}&PartitionKind=Int64Range";
        }

        public static string GetProxyUrl(ServiceContext context, string path, string partitionId)
        {
            Uri proxyAddress = GetProxyAddress(context);
            long partitionKey = GetPartitionKey(partitionId);
            return GetProxyUrl(proxyAddress, path, partitionKey);
        }

        public async static Task<ServicePartitionList> GetAllPartitions(ServiceContext context, FabricClient fabricClient)
        {
            Uri serviceName = ChatApi.GetChatDataServiceName(context);
            return await fabricClient.QueryManager.GetPartitionListAsync(serviceName);

        }
    }
}
