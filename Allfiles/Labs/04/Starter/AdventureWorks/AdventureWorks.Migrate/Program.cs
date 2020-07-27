using AdventureWorks.Context;
using AdventureWorks.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdventureWorks.Migrate
{
    public class Program
    {
        private const string sqlDBConnectionString = "Server=tcp:polysqlsrvrtribi01.database.windows.net,1433;Initial Catalog=AdventureWorks;Persist Security Info=False;User ID=testuser;Password=Testadmin123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        private const string cosmosDBConnectionString = "AccountEndpoint=https://polycosmostribi01.documents.azure.com:443/;AccountKey=A9Yf7zJoGMvffTSG95SXdK7KRQr5Wldfhwqrc0sFfycZgN0JAHrWqV9oyWLAe989UwlYDSgv8va4xeHackFFeg==;";
        
        public static async Task Main(string[] args)
        {
            await Console.Out.WriteLineAsync("Start Migration");

            using AdventureWorksSqlContext context = new AdventureWorksSqlContext(sqlDBConnectionString);

            List<Model> items = await context.Models
                .Include(m => m.Products)
                .ToListAsync<Model>();

            await Console.Out.WriteLineAsync($"Total Azure SQL DB Records: {items.Count}");

            //Cosmos database
            using CosmosClient client = new CosmosClient(cosmosDBConnectionString);

            Database database = await client.CreateDatabaseIfNotExistsAsync("Retail");

            Container container = await database.CreateContainerIfNotExistsAsync("Online",
                partitionKeyPath: $"/{nameof(Model.Category)}",
                throughput: 1000
            );

            int count = 0;
            foreach (var item in items)
            {
                ItemResponse<Model> document = await container.UpsertItemAsync<Model>(item);
                await Console.Out.WriteLineAsync($"Upserted document #{++count:000} [Activity Id: {document.ActivityId}]");
            }

            await Console.Out.WriteLineAsync($"Total Azure Cosmos DB Documents: {count}");
        }
    }
}
