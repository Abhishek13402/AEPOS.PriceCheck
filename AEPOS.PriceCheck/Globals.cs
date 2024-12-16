using AEPOS.DAO;
using CopyRightResources.Std;
using Newtonsoft.Json;
using Npgsql;
using Sypram.Common;
using System.Data;
using System.Net;
using System.Net.Http.Headers;

namespace AEPOS.PriceCheck
{
    public class Globals
    {
        public static bool Isnet_Connected = true;
        public static bool IsServerConnected = true;
        public static Store_REC StoreREC = null;
        public static bool InitialLoad = true;
        private static string _Error;
        public static bool isScanRunning = false;
        public static string StrDateFormat = "";
        public static string GUID = "";
        public static string StoreName = "";
		public static string Store_ID = "";
		public static string ErrorMessage
        {
            get { return _Error; }
        }

        #region Connection Properties and Methods

        private static DBManager _ServerDB;
        // private static SQLiteManager _ConfigDB;
        static string Server_ConnStr;

        //------------------------------------------------------------------
        /// <summary>
        /// Main database connection
        /// </summary>
        public static DBManager DBServer
        {
            get
            {
                if (Globals.IsServerConnected == false)
                {

                    return _ServerDB;
                }
                else if (_ServerDB == null || _ServerDB.DBConn == null || _ServerDB.DBConn.State != ConnectionState.Open)
                {

                    ConnectToDatabase();
                }

                return _ServerDB;
            }
        }

        public static string GetServer_ConnStr
        {
            get { return Server_ConnStr; }
        }

        public static string SetServer_ConnStr
        {
            set { Server_ConnStr = value; }
        }




        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Load connection strings from congig file and make database connections
        /// </summary>
        /// <returns></returns>
        public static bool ConnectToDatabase(bool bDisplayMessage = true)
        {
            try
            {


                if (string.IsNullOrEmpty(Server_ConnStr))
                {
                    //if (LoadConnectionString(bDisplayMessage) == false)
                    return false;
                }

                #region Check Server Database string and connection

                if (_ServerDB == null)
                    _ServerDB = new DBManager();



                if (!_ServerDB.ConnectFromString(Server_ConnStr))
                {


                    if (bDisplayMessage)
                    {
                        ///Display Error
                        //Globals.UltraMessageBox(null, "Unable to connect to server database. " + _ServerDB.GetErrorMessage(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    return false;
                }
                else
                {
                    Globals.IsServerConnected = true;
                }

                #endregion


                return true;
            }
            catch (Exception ex)
            {
				Console.WriteLine(ex.Message);
				if (bDisplayMessage)
                {
                    ///Display Error
                    //Globals.UltraMessageBox(null, "Unable to connect to database.\n" + ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                return false;
            }
            finally
            {
            }
        }

        //-----------------------------------------------------------------------------------

        public static DBManager CloneConnection(String ConnectionString)
        {
            DBManager localDB = new DBManager();

            if (!localDB.ConnectFromString(ConnectionString))
            {
                return null;
            }

            return localDB;
        }

        #endregion

        public static async Task<bool> GetConnectionString(string STORE_GUID)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://crmapi9.sypramsoftware.com/api/LicenseAPI/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });

                string store_GUID_json = WebUtility.UrlEncode(STORE_GUID);
                string address_1 = "FindStore?_storeDBID=" + store_GUID_json;

                HttpResponseMessage response = await client.GetAsync(address_1);
                if (!response.IsSuccessStatusCode)
                {
                    _Error = "Invalid Store GUID.\nPlease enter valid Store GUID.";
                    return false;
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseContent))
                {
                    _Error = "Invalid Store GUID.\nPlease enter valid Store GUID.";
                    return false;
                }

                ReturnStatus RetSta = JsonConvert.DeserializeObject<ReturnStatus>(responseContent);
                if (RetSta == null)
                {
                    _Error = "Invalid URL.";
                    return false;
                }
                else if (RetSta.StatusVal == false)
                {
                    _Error = RetSta.StatusMsg;
                    return false;
                }

                BUSINESS objBusiness = JsonConvert.DeserializeObject<BUSINESS>(RetSta.Data.ToString());
                string resMessage = "";

                if (objBusiness == null)
                {
                    return false;
                }

                bool success = SypCryptoBridge.ExtractFileExt(objBusiness.STORE_ID, SypCryptoBridge.enKeyType.DBParameters, SypCryptoBridge.enSoftType.AdvEntPOS, out resMessage);
                if (success)
                {
                    Server_ConnStr = resMessage;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool CheckServerConnection(string ServerStr)
        {
            DBManager _ServerDB = new DBManager();

            try
            {
                _ServerDB = Globals.CloneConnection(ServerStr);
                if (_ServerDB == null || _ServerDB.DBConn == null || _ServerDB.DBConn.State != ConnectionState.Open)
                {
                    return false;
                }

                NpgsqlCommand command = new NpgsqlCommand("select 1", _ServerDB.DBConn);
                command.ExecuteScalar();

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (_ServerDB != null && _ServerDB.DBConn != null)
                {
                    _ServerDB.CloseConnection();
                    _ServerDB = null;
                }
            }
        }

    }

    public class BUSINESS
    {
        public int STORENUM { get; set; }

        public string STORE_DBID { get; set; }
        public string COMMON_GUID { get; set; }

        public int CLIENT_ID { get; set; }
        public int AGENT_ID { get; set; }
        public int PRODUCT_ID { get; set; }

        public string BUSINESS_NAME { get; set; }
        public string ADDRESS1 { get; set; }
        public string ADDRESS2 { get; set; }
        public string CITY { get; set; }
        public string STATE { get; set; }
        public string ZIPCODE { get; set; }
        public string COUNTRY { get; set; }

        public string STOREPHONE { get; set; }
        public string CONTACTNAME1 { get; set; }
        public string CONTACTPHONE1 { get; set; }
        public string CONTACTNAME2 { get; set; }
        public string CONTACTPHONE2 { get; set; }

        public string SUPPORTNUM { get; set; }
        public string HARDWAREPROF { get; set; }

        public DateTime INSTALLDATE { get; set; }
        public DateTime ACTIVATIONDATE { get; set; }
        public DateTime EXPIRYDATE { get; set; }

        public short ONLINECC { get; set; }
        public string CCPROCESSING { get; set; }

        public string COUNTRYCODE { get; set; }

        public string ResellerID { get; set; }
        public bool WhiteListThisTerminal { get; set; }
        public string TermID { get; set; }
        public string MachineName { get; set; }

        public bool HasLotto { get; set; }

        public string STORE_ID { get; set; }
        public bool IsTestConnection { get; set; }
        public bool IsLocalDB { get; set; }
        public string SERVER { get; set; }
        public string DBNAME { get; set; }
        public string DBUSER { get; set; }
        public string DBPASSWD { get; set; }

        public int StoreUser_ID { get; set; }
    }
}

