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

    /// <summary>
    /// class take table connection string table name and columns
    /// register service for tabe
    /// listen for changes in table
    /// update the client project 
    /// @param connectionString table name and columns configuration
    /// </summary>
    public class SqlServerTableChangeListener
    {
        private readonly string _connectionString;
        private SqlDependency? _dependency; 
        public event Action<object>? TableChanged; 

        public SqlServerTableChangeListener(IConfiguration configuration, string connectionName = "DefaultConnection")
        {
            _connectionString = configuration.GetConnectionString(connectionName)
                ?? throw new ArgumentNullException(nameof(connectionName), "Connection string not found.");
        }

        public void StartListeningForTable(string tableName, string columns = "*")
        {
            SqlDependency.Start(_connectionString);
            RegisterDependency(tableName, columns);
        }

        public void StopListening()
        {
            SqlDependency.Stop(_connectionString);
        }

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

        private void OnNotificationChange(object sender, SqlNotificationEventArgs e, string tableName)
        {
            if (e.Info == SqlNotificationInfo.Insert || e.Info == SqlNotificationInfo.Update || e.Info == SqlNotificationInfo.Delete)
            {
                var latestRecord = GetLatestRecord(tableName);
                if (latestRecord != null)
                {
                    TableChanged?.Invoke(latestRecord);
                }
            }
            RegisterDependency(tableName, "*");
        }


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
                            return MapToDynamicEntity(reader);
                        }
                    }
                }
            }
            return null;
        }

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
                var property = objType.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
                if (property != null && kvp.Value != null && property.CanWrite)
                {
                    var value = Convert.ChangeType(kvp.Value, property.PropertyType);
                    property.SetValue(obj, value);
                }
            }
            return obj;
        }
    }

}
