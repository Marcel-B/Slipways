using SQLite;
using System;
using System.IO;

namespace Slipways.Mobile.Infrastructure
{
    public class Constants
    {
        public const string DatabaseFilename = "slipways.db3";

        private const SQLiteOpenFlags flags =
            // open the database in read/write mode
            SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLiteOpenFlags.SharedCache;

        public static string DatabasePath
        {
            get
            {
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(basePath, DatabaseFilename);
            }
        }

        public static SQLiteOpenFlags Flags => flags;
    }
}
