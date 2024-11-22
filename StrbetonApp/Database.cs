using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrbetonApp
{
    public class Database
    {
        public static SqliteConnection GetConnection()
        {
            string connectionString = "Data Source=strbeton.db;";
            return new SqliteConnection(connectionString);
        }
    }
}
