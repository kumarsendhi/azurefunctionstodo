using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

namespace FunctionApp2
{
    public static class TodoApi
    {
        //private static List<Todo> items = new List<Todo>();

        //[FunctionName("CreateTodo")]
        //public static async Task<IActionResult> CreateTodo(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route ="todo")]HttpRequest req,
        //    [Table("todos",Connection = "AzureWebJobsStorage")] IAsyncCollector<TodoTableEntity> todoTable,
        //    TraceWriter log)
        //{
        //    log.Info("Creating a new todo list item");
        //    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //    var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

        //    var todo = new Todo() { TaskDescription = input.TaskDescription };
        //    //items.Add(todo);
        //    await todoTable.AddAsync(todo.ToTableEntity());
        //    return new OkObjectResult(todo);
        //}

        [FunctionName("CreateTodo")]
        public static async Task<IActionResult> CreateTodo(
      [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")]HttpRequest req,
      [Queue("todos", Connection = "AzureWebJobsStorage")] IAsyncCollector<Todo> todoQueue,
      [Table("todos", Connection = "AzureWebJobsStorage")] IAsyncCollector<TodoTableEntity> todoTable,
      TraceWriter log)
        {
            log.Info("Creating a new todo list item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new Todo() { TaskDescription = input.TaskDescription };
            //items.Add(todo);
            await todoTable.AddAsync(todo.ToTableEntity());
            await todoQueue.AddAsync(todo);
            return new OkObjectResult(todo);
        }

        [FunctionName("GetTodos")]
        public static async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")]HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            TraceWriter log)
        {
            log.Info("Getting todo list items");
            var query = new TableQuery<TodoTableEntity>();
            var segment = await todoTable.ExecuteQuerySegmentedAsync(query, null);
            return new OkObjectResult(segment.Select(Mappings.ToTodo));
            
            //return new OkObjectResult(items);
        }


        [FunctionName("GetTodoById")]
        public static async Task<IActionResult> GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")]HttpRequest req,
            [Table("todos","TODO","{id}", Connection = "AzureWebJobsStorage")] TodoTableEntity todo,
            TraceWriter log, string id)
        {
            log.Info("Get a particular item based on id");
            //var todo = items.FirstOrDefault(t => t.Id == id);
            if (todo == null)
            {
                return new NotFoundResult();
            }
            //return new OkObjectResult(todo);
            return new OkObjectResult(todo.ToTodo());
        }


        //[FunctionName("UpdateTodo")]
        //public static async Task<IActionResult> UpdateTodo(
        //  [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")]HttpRequest req,
        //  TraceWriter log, string id)
        //{
        //    log.Info("Update a particular item based on id");
        //    var todo = items.FirstOrDefault(t => t.Id == id);
        //    if (todo == null)
        //    {
        //        return new NotFoundResult();
        //    }

        //    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //    var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);

        //    todo.IsCompleted = updated.IsCompleted;
        //    if (!string.IsNullOrEmpty(updated.TaskDescription))
        //    {
        //        todo.TaskDescription = updated.TaskDescription;
        //    }
        //    return new OkObjectResult(todo);
        //}

        [FunctionName("UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")]HttpRequest req,
[Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
TraceWriter log, string id)
        {
            log.Info("Update a particular item based on id");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
            var findOperation = TableOperation.Retrieve<TodoTableEntity>("TODO", id);
            var findResult = await todoTable.ExecuteAsync(findOperation);
            if(findResult.Result == null)
            {
                return new NotFoundResult();
            }
            var existingRow = (TodoTableEntity)findResult.Result;
            existingRow.IsCompleted = updated.IsCompleted;

            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                existingRow.TaskDescription = updated.TaskDescription;
            }

            var replaceOperation = TableOperation.Replace(existingRow);
            await todoTable.ExecuteAsync(replaceOperation);
            return new OkObjectResult(existingRow.ToTodo());
        }


        //[FunctionName("DeleteTodo")]
        //public static async Task<IActionResult> DeleteTodo(
        // [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")]HttpRequest req,
        // TraceWriter log, string id)
        //{
        //    log.Info("Delete a particular item based on id");
        //    var todo = items.FirstOrDefault(t => t.Id == id);
        //    if (todo == null)
        //    {
        //        return new NotFoundResult();
        //    }

        //    items.Remove(todo);
        //    return new OkObjectResult(todo);
        //}

        [FunctionName("DeleteTodo")]
        public static async Task<IActionResult> DeleteTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")]HttpRequest req,
        [Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
        TraceWriter log, string id)
        {
            var deleteOperation = TableOperation.Delete(new TableEntity()
            {
                PartitionKey = "TODO",
                RowKey = id,
                ETag = "*"
            });
            try
            {
                var deleteResult = await todoTable.ExecuteAsync(deleteOperation);
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }
            return new OkResult();
        }


    }
}
