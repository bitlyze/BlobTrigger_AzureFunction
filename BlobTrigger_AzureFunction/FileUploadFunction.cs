using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using BlobTrigger_AzureFunction.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;


namespace BlobTrigger_AzureFunction
{
    public class FileUploadFunction
    {
        #region Property
        private readonly AppDbContext appDbContext;
        #endregion

        #region Constructor
        public FileUploadFunction(AppDbContext appDbContext)
        {
            this.appDbContext = appDbContext;
        }
        #endregion

        [FunctionName("ProcessCsvOnUpload")]
        public static async Task Run(
        [BlobTrigger("%INPUT_CONTAINER%/{name}", Connection = "AzureWebJobsStorage")] Stream myBlob,
        string name,
        ILogger log)
        {
            string outputContainer = Environment.GetEnvironmentVariable("OUTPUT_CONTAINER");
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            log.LogInformation($"Processing CSV file: {name}");

            // Read & process the CSV file
            using (var reader = new StreamReader(myBlob))
            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (line != null)
                    {
                        await writer.WriteLineAsync(line.ToUpper()); // Convert to uppercase as an example
                    }
                }
                await writer.FlushAsync();
                memoryStream.Position = 0;

                // Upload processed file to output blob container
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(outputContainer);
                BlobClient blobClient = containerClient.GetBlobClient(name);
                await blobClient.UploadAsync(memoryStream, overwrite: true);
            }

            log.LogInformation($"Processed CSV file saved to {outputContainer}/{name}");
        }
    }
}
