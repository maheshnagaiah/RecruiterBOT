using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PublishBot.Models
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;

    public static class DocumentDBRepository<T> where T : class
    {
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["database"];
        private static readonly string CollectionId = ConfigurationManager.AppSettings["collection"];
        private static DocumentClient client;

        public static async Task<T> GetItemAsync(string id)
        {
            try
            {
                Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = -1 })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task<Document> CreateItemAsync(T item)
        {
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
        }

        public static async Task<Document> UpdateItemAsync(string id, T item)
        {
            return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
        }

        public static async Task DeleteItemAsync(string id)
        {
            await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
        }

        public static void Initialize()
        {
            client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["endpoint"]), ConfigurationManager.AppSettings["authKey"]);
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    client.CreateDatabaseAsync(new Database { Id = DatabaseId }).Wait();
                }
                else
                {
                    throw;
                }
            }
        }


        public static async Task<Microsoft.Azure.Documents.Client.StoredProcedureResponse<T2>> ExecuteSP<T2>(string spSelfLink, params dynamic[] parameters)
        {

            return await client.ExecuteStoredProcedureAsync<T2>(spSelfLink, parameters);

        }

        public static async Task<Microsoft.Azure.Documents.Client.StoredProcedureResponse<T2>> ExecuteSP<T2>(string spSelfLink)
        {

            return await client.ExecuteStoredProcedureAsync<T2>(spSelfLink);

        }

        public static async Task<Microsoft.Azure.Documents.Client.StoredProcedureResponse<T2>> ExecuteSPByName<T2>(string spName, params dynamic[] parameters)
        {
            DocumentCollection collection = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            StoredProcedure storedProcedure = client.CreateStoredProcedureQuery(collection.StoredProceduresLink).Where(c => c.Id == spName).AsEnumerable().FirstOrDefault();

            return await client.ExecuteStoredProcedureAsync<T2>(storedProcedure.SelfLink, parameters);

        }

        public static async Task<Microsoft.Azure.Documents.Client.StoredProcedureResponse<T2>> ExecuteSPByName<T2>(string spName)
        {
            DocumentCollection collection = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            StoredProcedure storedProcedure = client.CreateStoredProcedureQuery(collection.StoredProceduresLink).Where(c => c.Id == spName).AsEnumerable().FirstOrDefault();

            return await client.ExecuteStoredProcedureAsync<T2>(storedProcedure.SelfLink);

        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection { Id = CollectionId },
                        new RequestOptions { OfferThroughput = 1000 }).Wait();
                }
                else
                {
                    throw;
                }
            }
        }
    }
}