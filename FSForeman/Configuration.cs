using System;
using System.IO;
using System.Data.SQLite;

namespace FSForeman {
    /// <summary>
    /// Uses a SQLite backend to allow configuration persistance.
    /// </summary>
    public class Configuration {
        /// <summary>
        /// Singleton <see cref="Configuration"/> object.
        /// </summary>
        public static Configuration Global;
        
        private string connString;
        private CachedResults<string> ignores;
        private CachedResults<string> roots;
        private OtherOpts otherOpts;

        /// <summary>
        /// Patterns that should be ignored by the <see cref="Watcher"/> and <see cref="FileCache"/>.
        /// </summary>
        public string[] Ignores { get { return GetSingleColumnCachedResults(ignores, "Ignores", "Pattern"); } }
        /// <summary>
        /// The base directories monitored by the <see cref="Watcher"/> and <see cref="FileCache"/>.
        /// </summary>
        public string[] Roots { get { return GetSingleColumnCachedResults(roots, "Roots", "Root"); } }

        /// <summary>
        /// Adds an ignore pattern.
        /// </summary>
        /// <param name="pattern">A regex pattern.</param>
        public void AddIgnore(string pattern) { AddToSingleColumn("Ignores", "Pattern", pattern, ignores); }
        /// <summary>
        /// Adds a root directory.
        /// </summary>
        /// <param name="root">A directory.</param>
        public void AddRoot(string root) { AddToSingleColumn("Roots", "Root", root, roots); }

        /// <summary>
        /// Removes an ignore pattern.
        /// </summary>
        /// <param name="pattern">A regex pattern.</param>
        public void RemoveIgnore(string pattern) { RemoveFromSingleColumn("Ignores", "Pattern", pattern, ignores); }
        /// <summary>
        /// Removes a root directory.
        /// </summary>
        /// <param name="root">A directory.</param>
        public void RemoveRoot(string root) { RemoveFromSingleColumn("Roots", "Root", root, roots); }

        /// <summary>
        /// The port the <see cref="WebHost"/> uses.
        /// </summary>
        public int Port {
            get { CheckUpdateOtherOptions(); return otherOpts.Port; }
            set { var newOpts = new OtherOpts(otherOpts); newOpts.Port = value; SetOtherOptions(newOpts); }
        }

        /// <summary>
        /// How long the program should sleep between performing cache updates.
        /// </summary>
        public int UpdateDelay {
            get { CheckUpdateOtherOptions(); return otherOpts.UpdateDelay; }
            set { var newOpts = new OtherOpts(otherOpts); newOpts.UpdateDelay = value; SetOtherOptions(newOpts); }
        }

        /// <summary>
        /// Creates a new <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="dbFile">Path of the configuration file.</param>
        /// <param name="setGlobal">If true, sets the <see cref="Global"/> instance.</param>
        public Configuration(string dbFile, bool setGlobal = false) {
            connString = $"Data Source={dbFile}";

            if (!File.Exists(dbFile)) {
                SQLiteConnection.CreateFile(dbFile);
                ResetConfig();
            }

            if (setGlobal)
                Global = this;
        }

        /// <summary>
        /// Populates the configuration database with default values.
        /// </summary>
        public void ResetConfig() {
            using (var conn = new SQLiteConnection(connString)) {
                conn.Open();
                using (var trans = conn.BeginTransaction()) {
                    // IGNORES
                    var sql = "CREATE TABLE Ignores (Pattern TEXT UNIQUE)";
                    var cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    sql = "INSERT INTO Ignores VALUES(@Pattern)";
                    cmd = new SQLiteCommand(sql, conn);
                    var param = new SQLiteParameter("Pattern");
                    cmd.Parameters.Add(param);
                    foreach (var s in DefaultIgnoreDirs) {
                        param.Value = s;
                        cmd.ExecuteNonQuery();
                    }

                    // ROOTS
                    sql = "CREATE TABLE Roots (Root TEXT UNIQUE)";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    sql = "INSERT INTO Roots VALUES(@Root)";
                    cmd = new SQLiteCommand(sql, conn);
                    param = new SQLiteParameter("Root");
                    cmd.Parameters.Add(param);
                    foreach (var s in DefaultRoots) {
                        param.Value = s;
                        cmd.ExecuteNonQuery();
                    }

                    // OTHER
                    otherOpts = new OtherOpts();
                    sql = "CREATE TABLE Other (Port INTEGER, UpdateDelay INTEGER)";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    sql = "INSERT INTO Other (Port, UpdateDelay) VALUES(@Port, @UpdateDelay)";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.Add(new SQLiteParameter("Port", otherOpts.Port));
                    cmd.Parameters.Add(new SQLiteParameter("UpdateDelay", otherOpts.UpdateDelay));
                    cmd.ExecuteNonQuery();

                    trans.Commit();
                }
            }
        }
        
        private void CheckUpdateOtherOptions() {
            // Since nothing else SHOULD be in the database, we can be sure that optherOpts
            // is always up-to-date.
            if (otherOpts == null) {
                using (var conn = new SQLiteConnection(connString)) {
                    conn.Open();
                    using (var trans = conn.BeginTransaction()) {
                        var sql = "SELECT Port, UpdateDelay FROM Other";
                        var cmd = new SQLiteCommand(sql, conn);
                        var reader = cmd.ExecuteReader();
                        reader.Read();
                        otherOpts.Port = (int)reader["Port"];
                        otherOpts.UpdateDelay = (int)reader["UpdateDelay"];

                        trans.Commit();
                    }
                }
            }
        }

        private void SetOtherOptions(OtherOpts newOpts) {
            if (newOpts.Port == otherOpts.Port && newOpts.UpdateDelay == otherOpts.UpdateDelay)
                return;
            otherOpts = newOpts;
            using (var conn = new SQLiteConnection(connString)) {
                conn.Open();
                using (var trans = conn.BeginTransaction()) {
                    var sql = "UPDATE Other SET Port=@Port, UpdateDelay=@UpdateDelay";
                    var cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.Add(new SQLiteParameter("Port", otherOpts.Port));
                    cmd.Parameters.Add(new SQLiteParameter("UpdateDelay", otherOpts.UpdateDelay));
                    cmd.ExecuteNonQuery();

                    trans.Commit();
                }
            }
        }

        /// <summary>
        /// Adds a single value to a table with a single column.
        /// </summary>
        private void AddToSingleColumn<T>(string table, string column, T value, CachedResults<T> cache) {
            using (var conn = new SQLiteConnection(connString)) {
                conn.Open();
                using (var trans = conn.BeginTransaction()) {
                    var sql = $"INSERT INTO {table} ({column}) VALUES(@value)";
                    var cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.Add(new SQLiteParameter("value", value));
                    cmd.ExecuteNonQuery();

                    trans.Commit();
                }
            }
            if (cache != null)
                cache.Fresh = false;
            Logger.LogLine($"Added {column} {value} to {table}");
        }

        /// <summary>
        /// Removes a single value from a table with a single column.
        /// </summary>
        private void RemoveFromSingleColumn<T>(string table, string column, T value, CachedResults<T> cache) {
            using (var conn = new SQLiteConnection(connString)) {
                conn.Open();
                using (var trans = conn.BeginTransaction()) {
                    var sql = $"DELETE FROM {table} WHERE {column} = @value";
                    var cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.Add(new SQLiteParameter("value", value));
                    cmd.ExecuteNonQuery();

                    trans.Commit();
                }
            }
            if (cache != null)
                cache.Fresh = false;
            Logger.LogLine($"Removed {column} {value} from {table}");
        }

        /// <summary>
        /// Returns a single colum from a table.
        /// </summary>
        private CachedResults<T> GetFromSingleColumn<T>(string table, string column) {
            CachedResults<T> results;
            using (var conn = new SQLiteConnection(connString)) {
                conn.Open();
                using (var trans = conn.BeginTransaction()) {
                    var sql = $"SELECT COUNT(*) FROM {table}";
                    var cmd = new SQLiteCommand(sql, conn);
                    var count = (long)cmd.ExecuteScalar();
                    results = new CachedResults<T>(count);
                    if (count == 0)
                        return results;
                    sql = $"SELECT {column} FROM {table}";
                    cmd = new SQLiteCommand(sql, conn);
                    var reader = cmd.ExecuteReader();
                    for (var i = 0; reader.Read(); i++)
                        results.Results[i] = (T)reader[column];

                    trans.Commit();
                }
            }
            return results;
        }

        /// <summary>
        /// Determines if a cache is valid, and either refreshes the cache or returns the cached results.
        /// </summary>
        private T[] GetSingleColumnCachedResults<T>(CachedResults<T> cached, string table, string column) {
            if (cached == null || !cached.Fresh)
                cached = GetFromSingleColumn<T>(table, column);
            return cached.Results;
        }

        /// <summary>
        /// A wrapper around an array, which contains a bool deciding if the information is current or not.
        /// </summary>
        private class CachedResults<T> {
            public T[] Results;
            public bool Fresh;
            
            public CachedResults(long size) {
                Results = new T[size];
                Fresh = true;  // Pretty good assumption
            }
        }

        // Default configuration info
        private string[] DefaultIgnoreDirs = {
           // @"*\System Volume Information"
           @"\\System Volume Information"
        };

        private string[] DefaultRoots = {
            @"D:\"
        };

        [Serializable]
        private class OtherOpts {
            public int Port = 7654;
            public int UpdateDelay = 60000; // 1 minute

            public OtherOpts() { } // required because of the copy constructor

            // Copy constructor, because IClonable stinks
            public OtherOpts(OtherOpts copyFrom) {
                Port = copyFrom.Port;
                UpdateDelay = copyFrom.UpdateDelay;
            }
        }
    }
}
