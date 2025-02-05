# ListenItPro - Table Tracker Library

**v** is a C# library designed to track changes (insert, update, delete) in a specified database table. It triggers events whenever changes occur, allowing you to respond to data modifications in real-time.

## Installation

To install the package, run the following command:

dotnet add package ListenItPro



## Usage

You can create a separate class to listen to specific tables based on their names. For example, to listen to changes in the JobQueue table, you can define a JobQueueSubscriber class like this:

csharp
Copy
Edit
using ListenItPro;
using System.Reflection;
using System.Text.Json;

namespace WebApplication1.services
{
    public class JobsQueue
    {
        public int Id { get; set; } // Maps to the Id column (Primary Key)
        public string JobType { get; set; } // Maps to the JobType column
        public string JobData { get; set; } // Maps to the JobData column
        public string Status { get; set; } // Maps to the Status column (with a default of 'Pending')
    }

    public class JobQueueSubscriber
    {
        private readonly SqlServerTableChangeListener _listener;

        public JobQueueSubscriber(SqlServerTableChangeListener listener)
        {
            _listener = listener;
            _listener.TableChanged += OnTableChanged;
        }

        // This will be called when the table change event occurs
        private void OnTableChanged(object tableData)
        {
            Console.WriteLine("Table data changed:");

            // Check if tableData is an ExpandoObject
            if (tableData is System.Dynamic.ExpandoObject expando)
            {
                var dict = (IDictionary<string, object>)expando;

                var jobque = _listener.MapToClass<JobsQueue>(dict);
                // You can handle the changed data here, like updating your application state
            }

            StartListening();
        }

        public void StartListening()
        {
            // Start listening to changes in the JobsQueue table
            _listener.StartListeningForTable("JobsQueue", "Id");
        }

        public void StopListening()
        {
            _listener.StopListening();
        }
    }
}
Key Points:
You can create a custom class (JobQueueSubscriber in this case) to listen for changes in a specific table (e.g., JobsQueue).
The class subscribes to changes and processes the events when data in the JobsQueue table is modified.
You can map the dynamic table data to a strongly-typed class (JobsQueue), enabling you to work with the data as objects.
License
This project is licensed under the MIT License - see the LICENSE.md file for details.


# Register service in Program.cs


builder.Services.AddSingleton<SqlServerTableChangeListener>(serviceProvider =>
    new SqlServerTableChangeListener(builder.Configuration)); // Register the table listener

builder.Services.AddSingleton<JobQueueSubscriber>(serviceProvider =>
    new JobQueueSubscriber(serviceProvider.GetRequiredService<SqlServerTableChangeListener>())); // Register the subscriber



var app = builder.Build();


var subscriber = app.Services.GetRequiredService<JobQueueSubscriber>();
subscriber.StartListening(); // Start listening for table changes





```csharp




Contributing
Feel free to contribute! Please refer to CONTRIBUTING.md for more information.

pgsql
Copy
Edit

This section explains how users can create separate subscribers for different tables (like the `JobQueue` table) and react to changes specific to those tables. It provides an example that demonstrates how to map the database changes to a C# class (`JobsQueue` in this case).

Let me know if you'd like further adjustments! 😊