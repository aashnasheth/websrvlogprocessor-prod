using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ParserLib;
using Microsoft.Azure.Documents;
using System.Collections.Generic;

namespace parserfn
{
    // Cosmos DB Entity
    public class AccessedSitesLog
    {
        public string id { get; set; }
        public string PartitionKey { get; set; }
        public string Date { get; set; }
        public string Url { get; set; }
        public long MainModuleCount { get; set; }
        public long SubModuleCount { get; set; }
    }

    public static class JsonToDB
    {
        [FunctionName("JsonToDB")]
        public static void Run(
            [BlobTrigger("processedjson/{name}", Connection = "AzureWebJobsStorage")]Stream inputJsonBlob, 
            string name, 
            ILogger log, 
            [CosmosDB(databaseName: "accessdatadb", collectionName: "accessdatacollection", ConnectionStringSetting = "AzureWebJobsCosmos")]ICollector<AccessedSitesLog> cosmosOutput)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {inputJsonBlob.Length} Bytes");

            //Deserialize the Input Json Blob as AccessDataDetail Object
            var serializer = new JsonSerializer();
            AccessDataDetail result = new AccessDataDetail();
            using (StreamReader reader = new StreamReader(inputJsonBlob))
            {
                using (var jsonTextReader = new JsonTextReader(reader))
                {
                    result = serializer.Deserialize<AccessDataDetail>(jsonTextReader);
                }
            }

            //For each AccessData Object in the list - create a record in Cosmos DB
            foreach (AccessData accessedsite in result.AccessDetails)
            {
                log.LogInformation($"Test UploadTime:{accessedsite.UpLoadTime} \n ");
                AccessedSitesLog accessedSiteRecord = new AccessedSitesLog
                {
                    PartitionKey = result.DeviceId + ' ' + accessedsite.ModuleName + ' ' + result.AccessDate,
                    id = result.DeviceId,
                    Url = accessedsite.ModuleName,
                    Date = accessedsite.UpLoadTime.ToString("MM/dd/yyyy"),
                    MainModuleCount = accessedsite.MainModuleCount,
                    SubModuleCount = accessedsite.SubModuleCount
                };

                // upload accessedSiteRecord to cosmos db
                cosmosOutput.Add(accessedSiteRecord);
                log.LogInformation($"Added all to  Cosmos DB");
            }
        }
    }
}
