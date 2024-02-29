using System;
using System.Text.Json;
using System.Threading;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

Console.WriteLine("Hello to the QueueProcessor!");

var queueClient = new QueueClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"), "pics-to-delete");

queueClient.CreateIfNotExists();

while (true)
{
    QueueMessage message = queueClient.ReceiveMessage();

    if (message != null)
    {
        Console.WriteLine($"Message received {message.Body}");

        var task = JsonSerializer.Deserialize<Task>(message.Body);
        //Create a Blob service client
        var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"));

        DeleteAlterEgoImage(blobClient, task);
        DeleteHeroImage(blobClient, task);
        //Delete message from the queue
        queueClient.DeleteMessage(message.MessageId, message.PopReceipt);

    }
    else
    {
        Console.WriteLine($"Let's wait 5 seconds");
        Thread.Sleep(5000);
    }
}

void DeleteAlterEgoImage(BlobServiceClient blobClient, Task task) 
{
    //Get container client
    BlobContainerClient container = blobClient.GetBlobContainerClient("alteregos");

    //Get blob with old name
    var fileName = $"{task.alterEgoName.Replace(' ', '-').ToLower()}.png";
    Console.WriteLine($"Looking for {fileName}");
    var blob = container.GetBlobClient(fileName);

    if (blob.Exists())
    {
        Console.WriteLine("Found it!");

        //Delete the old blob
        blob.DeleteIfExists();
    }
    else
    {
        Console.WriteLine($"There is no image to delete.");
        Console.WriteLine($"Dismiss task.");
    }

}

void DeleteHeroImage(BlobServiceClient blobClient, Task task)
{
    //Get container client
    BlobContainerClient container = blobClient.GetBlobContainerClient("heroes");

    List<string> imageExtensions = ["jpg", "jpeg"];

    foreach (var extension in imageExtensions)
    {
        var fileName = $"{task.heroName.Replace(' ', '-').ToLower()}.{extension}";
        Console.WriteLine($"Looking for {fileName}");
        var blob = container.GetBlobClient(fileName);

        if (blob.Exists())
        {
            Console.WriteLine("Found it!");
            //Delete the old blob
            blob.DeleteIfExists();
            return; 
        }
    }
    Console.WriteLine($"There is no image to delete.");
    Console.WriteLine($"Dismiss task.");
}

class Task
{
    public string alterEgoName { get; set; }
    public string heroName { get; set; }
}