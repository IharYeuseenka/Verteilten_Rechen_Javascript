using DistributeHashing.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace DistributeHashing
{
    public class DataService
    {
        #region Singleton

        private static DataService _this;

        public static DataService Init()
        {
            if (_this == default(DataService))
            {
                _this = new DataService();
            }

            return _this;
        }
        #endregion
        private readonly SqlConnection _connection; // переменная, в которой будет храниться соединение

        private readonly object _dataLocker = new object();

        private string _loadDataCommand; // сюда будет загружаться команда для загрузки данных для передачи клиенту

        //строка обновления обработанных записей
        private const string UPDATE_PATTERN = "UPDATE [Entities] SET [IsLocked] = 0, [Hash] = '{0}' WHERE [Text] = '{1}';\r\n";


        private DataService()
        {
            var connectionBulder = new SqlConnectionStringBuilder(GetConntetiionString());
            _connection = new SqlConnection(connectionBulder.ConnectionString);
            
            //new StreamReader(HttpContext.Current.Server.MapPath("~") + "Scripts\\LoadTaskData.sql").ReadToEnd();
        }

        private string GetConntetiionString()
        {
            //строку подключения нужно изменить в зависимости от вашей базы данных и используемого сервера
            return "workstation id=DistHashDB.mssql.somee.com;packet size=4096;user id=disthashing_SQLLogin_1;pwd=g7u9272oi6;data source=DistHashDB.mssql.somee.com;persist security info=False;initial catalog=DistHashDB;MultipleActiveResultSets=True;";
        }

        public IEnumerable<EntityModel> GetEntities() // метод загрузки данных клиенту для обработки
        {
            var table = new DataTable(); // таблица, в которую будут загружаться полученные из базы данные
            _connection.Open();

            lock (_dataLocker)//обеспечение потокобезопасности
            {
                try
                {
                    //формирование запроса
                    _loadDataCommand = String.Format("UPDATE [Entities]" +
                                "SET [IsLocked] = 0" +
                                "WHERE [IsLocked] = 1" +
                                "AND DATEDIFF(MINUTE,GETDATE(),[LockTime]) > 10;" +

                                "CREATE TABLE  [Loader {0}](" +
                                "     [ID]        INTEGER" +
                                "    ,[Text]      NVARCHAR(255)" +
                                "    ,[Hash]      NVARCHAR(32)" +
                                "    ,[IsLocked]  BIT" +
                                "    ,[LockTime]  DATETIME" +
                                ");" +

                                "INSERT INTO [Loader {0}]" +
                                "SELECT TOP 100 * " +
                                "FROM [Entities] " +
                                "WHERE [IsLocked] = 0" +
                                "AND [Hash] IS NULL;" +

                                "UPDATE [Entities]" +
                                "SET  [IsLocked] = 1" +
                                "    ,[LockTime] = GETDATE()" +
                                "WHERE [ID] IN (SELECT [ID] FROM [Loader {0}]);" +

                                "SELECT [Text] FROM [Loader {0}];" +

                                "DROP TABLE  [Loader {0}]; ",
                                Guid.NewGuid().ToString());
                    // выполнение запроcа и загрузка полученных данных в переменную table
                    table.Load(new SqlCommand(_loadDataCommand, _connection).ExecuteReader());
                    if (table.Rows.Count < 1)
                    {
                        return null;
                    }

                    var result = table
                                    .Rows
                                    .Cast<DataRow>()
                                    .AsParallel()
                                    .Select(row => new EntityModel { Text = (string)row["Text"] })
                                    .ToList();

                    return result;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    _connection.Close();
                }
            }
        }

        public bool SaveEntities(IEnumerable<EntityModel> entities)// метод записи обработанных клиентом данных в базу
        {
            _connection.Open();

            lock (_dataLocker)
            {
                try
                {
                    var command = new StringBuilder();
                    foreach (var entity in entities)
                    {
                        // формирование запроса на основе описанной выше константы UPDATE_PATTERN
                        command.AppendFormat(UPDATE_PATTERN, entity.Hash, entity.Text);
                    }

                    new SqlCommand(command.ToString(), _connection).ExecuteNonQuery();
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    _connection.Close();
                }
            }
        }
    }

}