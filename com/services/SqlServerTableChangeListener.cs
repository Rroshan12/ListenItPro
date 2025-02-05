using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ListenItPro.com.services
{
    public class SqlServerTableChangeListener
    {
        private readonly string _connectionString;
        private SqlDependency? _dependency; // For a single table
        public event Action<object>? TableChanged; // Event for any table change

        public SqlServerTableChangeListener(IConfiguration configuration, string connectionName = "DefaultConnection")
        {
            _connectionString = configuration.GetConnectionString(connectionName)
                ?? throw new ArgumentNullException(nameof(connectionName), "Connection string not found.");
        }

        // Start listening for changes in a single table
        public void StartListeningForTable(string tableName, string columns = "*")
        {
            SqlDependency.Start(_connectionString);

            // Register dependency for the specific table
            RegisterDependency(tableName, columns);
        }

        // Stop listening for changes
        public void StopListening()
        {
            SqlDependency.Stop(_connectionString);
        }

        // Register dependency for a specific table
        private void RegisterDependency(string tableName, string columns)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var commandText = $"SELECT {columns} FROM dbo.{tableName}";
                using (var command = new SqlCommand(commandText, connection))
                {
                    _dependency = new SqlDependency(command);
                    _dependency.OnChange += (sender, e) => OnNotificationChange(sender, e, tableName);
                    command.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
        }

        // Event handler when a change is detected
        private void OnNotificationChange(object sender, SqlNotificationEventArgs e, string tableName)
        {
            if (e.Info == SqlNotificationInfo.Insert || e.Info == SqlNotificationInfo.Update || e.Info == SqlNotificationInfo.Delete)
            {
                var latestRecord = GetLatestRecord(tableName);
                if (latestRecord != null)
                {
                    // Notify all subscribers with the full record
                    TableChanged?.Invoke(latestRecord);
                }
            }

            // Re-register for future changes to continue listening
            RegisterDependency(tableName, "*");
        }

        // Fetch the latest record for a table
        private object? GetLatestRecord(string tableName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand($"SELECT TOP 1 * FROM dbo.{tableName} ORDER BY (SELECT NULL) DESC", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapToDynamicEntity(reader); // Map SQL data to dynamic object
                        }
                    }
                }
            }
            return null;
        }

        // Map SQL data to a dynamic object for all columns
        private object MapToDynamicEntity(SqlDataReader reader)
        {
            var expandoObject = new System.Dynamic.ExpandoObject();
            var dict = (IDictionary<string, object>)expandoObject;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            return expandoObject;
        }




        public T MapToClass<T>(IDictionary<string, object> dict) where T : new()
        {
            var obj = new T();
            var objType = typeof(T);

            foreach (var kvp in dict)
            {
                // Get the property in the class that matches the dictionary key
                var property = objType.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);

                // If the property exists, set its value
                if (property != null && kvp.Value != null && property.CanWrite)
                {
                    // Convert the value to the type of the property
                    var value = Convert.ChangeType(kvp.Value, property.PropertyType);
                    property.SetValue(obj, value);
                }
            }

            return obj;
        }
    }

}
