using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionApp2
{
    public class Todo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("n");
        public DateTime dateTime { get; set; } = DateTime.UtcNow;
        public string TaskDescription { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class TodoCreateModel
    {
        public string TaskDescription { get; set; }
    }

    public class TodoUpdateModel
    {
        public string TaskDescription { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class TodoTableEntity : TableEntity
    {
        public DateTime dateTime { get; set; } 
        public string TaskDescription { get; set; }
        public bool IsCompleted { get; set; }
    }

    public static class Mappings
    {
        public static TodoTableEntity ToTableEntity(this Todo todo)
        {
            return new TodoTableEntity()
            {
                PartitionKey = "TODO",
                RowKey = todo.Id,
                dateTime = todo.dateTime,
                IsCompleted = todo.IsCompleted,
                TaskDescription = todo.TaskDescription
            };
        }

        public static Todo ToTodo(this TodoTableEntity todo)
        {
            return new Todo()
            {
                Id = todo.RowKey,
                dateTime = todo.dateTime,
                IsCompleted = todo.IsCompleted,
                TaskDescription = todo.TaskDescription
            };
        }
    }
}
