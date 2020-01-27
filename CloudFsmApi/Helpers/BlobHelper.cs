#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using CloudFsmApi.Config;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CloudFsmApi.Helpers
{
    public class BlobHelper
    {
        private readonly StorageConfig _config;

        public BlobHelper(StorageConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Retrieve data from Azure Blob Storage
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<string> ReadFromBlobAsync(string fileName)
        {
            CloudBlockBlob cloudBlockBlob = await GetBlockBlobReference(fileName);
            return await cloudBlockBlob.DownloadTextAsync().ConfigureAwait(false); ;
        }

        public async Task WriteToBlobAsync(string fileName, string newConfig)
        {
            CloudBlockBlob cloudBlockBlob = await GetBlockBlobReference(fileName);

            byte[] byteArray = Encoding.UTF8.GetBytes(newConfig);
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                await cloudBlockBlob.UploadFromStreamAsync(stream).ConfigureAwait(false);
            }
        }

        private async Task<CloudBlockBlob> GetBlockBlobReference(string fileName)
        {
            if (CloudStorageAccount.TryParse(_config.StorageCnxnString, out CloudStorageAccount storageAccount))
            {
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer cloudBlobContainer =
                    cloudBlobClient.GetContainerReference("raven");
                await cloudBlobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);

                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);

                return cloudBlockBlob;
            }
            else
            {
                throw new Exception("Invalid connection string");
            }
        }
    }
}
