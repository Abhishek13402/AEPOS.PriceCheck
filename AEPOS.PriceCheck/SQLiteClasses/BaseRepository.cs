using AEPOS.PriceCheck.SQLiteClasses.Models;
using SQLite;
using Sypram.Common;


namespace AEPOS.PriceCheck.SQLiteClasses
{
    public class CacheDataRepo : SypErrorBase
    {
        public const SQLite.SQLiteOpenFlags Flags =
     // open the database in read/write mode
     SQLite.SQLiteOpenFlags.ReadWrite |
     // create the database if it doesn't exist
     SQLite.SQLiteOpenFlags.Create |
     // enable multi-threaded database access
     SQLite.SQLiteOpenFlags.SharedCache;

        private SQLiteConnection conn;
        private const string DbName = "PriceCheck.db";
        private static readonly SQLiteAsyncConnection _DBConn;
        private static readonly string DatabasePath = Path.Combine(FileSystem.AppDataDirectory, DbName);

        protected SQLiteAsyncConnection Database
        {
            get
            {
                //if(_DBConn)
                var database = _DBConn;
                return database;
            }
        }

        static CacheDataRepo()
        {
            try
            {
                if (_DBConn != null)
                    return;


                string appDataPath = FileSystem.AppDataDirectory;
                string databasePath = Path.Combine(appDataPath, DbName);
                Console.WriteLine($"Database Path: {DatabasePath}");

				Console.WriteLine($"Initializing CacheDataRepo with database path: {databasePath}");

				_DBConn = new SQLiteAsyncConnection(DatabasePath, Flags);


                #region create Databases

                SQLiteAsyncConnection sQLiteAsync = _DBConn;
                //sQLiteAsync.CreateTableAsync<StoreInfo>().Wait();
                sQLiteAsync.CreateTableAsync<StoreInfo>();
				Console.WriteLine("Database and tables initialized successfully.");

				//sQLiteAsync.CreateTableAsync<Txn_Info>().Wait();
				//sQLiteAsync.CreateTableAsync<EmployeeInfo>().Wait();
				//sQLiteAsync.CreateTableAsync<UserInfo>().Wait();

				#endregion
			}
            catch (Exception ex)
            {
                Console.WriteLine($"Error during static initialization: {ex.Message}");
				Console.WriteLine($"Stack Trace: {ex.StackTrace}");
				// Add your error handling or logging mechanism
				throw new InvalidOperationException("Failed to initialize CacheDataRepo.", ex);
            }
        }



        public async Task<List<T>> ExecuteSelectQueryAsync<T>(string query) where T : new()
        {
            try
            {

                var results = await Database.QueryAsync<T>(query);
                return results.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while executing query: {ex.Message}");
                return new List<T>();
            }
        }



        public async Task ExecuteNonQuery(string Query)
        {
            try
            {
                await Database.ExecuteAsync(Query);
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> SaveStoreInfoData()
        {
            ResetErrors();

            try
            {
                await Database.ExecuteAsync("DELETE FROM STOREINFO");

                string insertQuery = "INSERT INTO STOREINFO (StoreGUID,ConnectionString,StoreName,City,Location,STDateFormat,StoreID) VALUES (?,?,?,?,?,?,?) ";
                await Database.ExecuteAsync(insertQuery, Globals.StoreREC.STORE_GUID, Globals.GetServer_ConnStr, Globals.StoreREC.StoreName, Globals.StoreREC.City, Globals.StoreREC.LOCATION, Globals.StrDateFormat, Globals.StoreREC.StoreID);

                return true;
            }
            catch (Exception ex)
            {
                AddError("Error occured while saving STOREINFO data : " + ex.Message, this + ": SaveStoreInfoData");
                return false;
            }
        }

        public async Task<bool> RemoveStoreData()
        {
            ResetErrors();

            try
            {
                await Database.ExecuteAsync("DELETE FROM STOREINFO");


                return true;
            }
            catch (Exception ex)
            {
                AddError("Error occured while removing STOREINFO data : " + ex.Message, this + ": SaveStoreInfoData");
                return false;
            }
        }


        public async Task<StoreInfo> LoadStore()
        {
            ResetErrors();

            try
            {
                var results = await Database.QueryAsync<StoreInfo>($"SELECT * from StoreInfo");
                StoreInfo store = new StoreInfo();

                if (results.Count > 0)
                {
                    store.StoreGUID = results[0].StoreGUID;
                    store.StoreID = results[0].StoreID;
                    store.StoreName = results[0].StoreName;
                    store.City = results[0].City;
                    store.Location = results[0].Location;
                    store.STDateFormat = results[0].STDateFormat;
                }
                else
                {
                    Console.WriteLine("Store data not found in local");
                    return null;
                }

                return store;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while executing query: {ex.Message}");
                return null;
            }
        }

    }
}
