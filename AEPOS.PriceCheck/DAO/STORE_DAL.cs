using AEPOS.DAO;
using AEPOS.Model;
using CopyRightResources.Std;
using Npgsql;
using Sypram.Common;
using System.Data;
using static CopyRightResources.Std.SypCryptoBridge;

namespace AEPOS.PriceCheck.DAO
{
    public class STORE_DAL : SypErrorBase
    {
        private Store_REC _STOREREC;

        //================================================================================

        public Store_REC StoreRecord
        {
            get => _STOREREC ?? (_STOREREC = new Store_REC());
            set => _STOREREC = value;
        }

        //================================================================================

        readonly DBManager _DbMgr;
        public STORE_DAL(DBManager db, bool bCreateRecObject)
        {
            _DbMgr = db ?? throw new Exception("Database object cannot be null");
            _STOREREC = bCreateRecObject ? new Store_REC() : null;
        }


        public bool LoadFirstStoreRecord(string StoreID = "", bool bZipDatabase = true)
        {
            ResetErrors();

            try
            {
                NpgsqlCommand cmd = null;
                if (string.IsNullOrEmpty(StoreID))
                {
                    cmd = new NpgsqlCommand
                    {
                        Connection = _DbMgr.DBConn,
                        CommandText = "SELECT STOREID FROM MST_STORE LIMIT 1"
                    };
                }
                else
                {
                    cmd = new NpgsqlCommand
                    {
                        Connection = _DbMgr.DBConn,
                        CommandText = "SELECT STOREID FROM MST_STORE WHERE STOREID = '" + StoreID + "' LIMIT 1"
                    };
                }

                Object objStore = cmd.ExecuteScalar();
                if (objStore == null || objStore == DBNull.Value)
                    return false;

                return LoadRecord(objStore.ToString());
            }
            catch (Exception ex)
            {
                //_log.Error(ex.Message + ":" + this + ":Load Store Record");
                AddError(ex.Message, "Load Store Record");
                return false;
            }
        }

        public static bool ExtractStoreEmpData(string strText, out string resMessage)
        {
            resMessage = "";
            //_log.Debug("Load Store Record : ExtractFileExt = " + strText);
            bool isSuccess = SypCryptoBridge.ExtractFileExt(strText, (enKeyType)2, (enSoftType)0, out resMessage);
            if (!isSuccess)
            {
                //_log.Debug("Load Store Record : ExtractFileExt = " + resMessage);
                resMessage = SypEncryption.Decrypt(strText, out isSuccess);
                if (!isSuccess)
                {
                    //_log.Debug("Load Store Record : Decrypt = " + resMessage);
                }
            }

            return isSuccess;
        }
        public bool LoadRecord(string StoreID, bool bZipDatabase = true)
        {
            ResetErrors();

            try
            {
                //_log.Debug("Load Store Record : START");

                DataTable dtStore = _DbMgr.GetDataTable(@"SELECT S.*, Z.ZONENUM 
                                        FROM MST_STORE S
                                        INNER JOIN MST_ZONE Z ON S.ZONEID = Z.ZONEID
                                        WHERE S.StoreID = '" + StoreID + @"'");

                if (_STOREREC == null)
                    _STOREREC = new Store_REC();

                bool RetVal;
                if (dtStore == null || dtStore.Rows.Count <= 0)
                {
                    //_log.Debug("Load Store Record : No store has been added yet. Please register store in POS");

                    _STOREREC.StoreID = "0";
                    AddError("No store has been added yet. Please register store in POS.", "Load Store Record");
                    RetVal = false;
                }
                else
                {
                    DataRow fdr = dtStore.Rows[0];

                    _STOREREC.StoreID = fdr["StoreID"].ToString();

                    if (!Convert.IsDBNull(fdr["StoreNum"]))
                    {
                        _STOREREC.StoreNum = Convert.ToInt32(fdr["StoreNum"].ToString());
                        ModelGlobals.STORENUM = _STOREREC.StoreNum;
                    }

                    _STOREREC.ZoneID = Convert.ToInt32(fdr["ZONEID"].ToString());
                    ModelGlobals.ZONEID = _STOREREC.ZoneID;

                    _STOREREC.ZoneNum = Convert.ToInt32(fdr["ZONENUM"].ToString());
                    ModelGlobals.ZONENUM = _STOREREC.ZoneNum;

                    string resMessage;
                    bool success;

                    //_log.Debug(this + ":Load Store Record : StoreName = " + fdr["StoreName"]);
                    success = SypCryptoBridge.ExtractFileExt(fdr["StoreName"].ToString(), SypCryptoBridge.enKeyType.StoreEmpData, SypCryptoBridge.enSoftType.AdvEntPOS, out resMessage);
                    if (!success)
                    {
                        //_log.Debug(this + ":Load Store Record : ExtractFileExt = " + resMessage);

                        resMessage = SypEncryption.Decrypt(fdr["StoreName"].ToString(), out success);
                        if (success)
                        {
                            if (bZipDatabase)
                                DBUpgrader.CompressDatabase(_DbMgr);
                        }
                        else
                        {
                            //_log.Debug(this + ":Load Store Record : Decrypt = " + resMessage);
                            AddError(resMessage, "Validating Store Name");
                            return false;
                        }
                    }
                    _STOREREC.StoreName = resMessage;

                    //_log.Debug(this + ":Load Store Record : City = " + fdr["City"]);
                    if (!Convert.IsDBNull(fdr["City"]))
                    {
                        success = ExtractStoreEmpData(fdr["City"].ToString(), out resMessage);
                        if (!success)
                        {
                            //_log.Debug(this + ":Load Store Record : ExtractStoreEmpData = " + resMessage);
                            AddError(resMessage, "Validating City");
                            return false;
                        }
                        _STOREREC.City = resMessage;
                    }

                    //_log.Debug(this + ":Load Store Record : ADDRESS1 = " + fdr["ADDRESS1"]);
                    if (!Convert.IsDBNull(fdr["ADDRESS1"]))
                    {
                        success = ExtractStoreEmpData(fdr["Address1"].ToString(), out resMessage);
                        if (!success)
                        {
                            //_log.Debug(this + ":Load Store Record : ExtractStoreEmpData = " + resMessage);
                            AddError(resMessage, "Validating Address1");
                            return false;
                        }
                        _STOREREC.Address1 = resMessage;
                    }

                    //_log.Debug(this + ":Load Store Record : ADDRESS2 = " + fdr["Address2"]);
                    if (!Convert.IsDBNull(fdr["ADDRESS2"]))
                    {
                        success = ExtractStoreEmpData(fdr["ADDRESS2"].ToString(), out resMessage);
                        if (!success)
                        {
                            //_log.Debug(this + ":Load Store Record : ExtractStoreEmpData = " + resMessage);
                            AddError(resMessage, "Validating Address2");
                            return false;
                        }
                        _STOREREC.Address2 = resMessage;
                    }

                    //_log.Debug(this + ":Load Store Record : STATE = " + fdr["STATE"]);
                    if (!Convert.IsDBNull(fdr["STATE"]))
                    {
                        success = ExtractStoreEmpData(fdr["State"].ToString(), out resMessage);
                        if (!success)
                        {
                            //_log.Debug(this + ":Load Store Record : ExtractStoreEmpData = " + resMessage);
                            AddError(resMessage, "Validating State");
                            return false;
                        }
                        _STOREREC.State = resMessage;
                    }

                    //_log.Debug(this + ":Load Store Record : ZIP = " + fdr["ZIP"]);
                    if (!Convert.IsDBNull(fdr["ZIP"]))
                    {
                        success = ExtractStoreEmpData(fdr["Zip"].ToString(), out resMessage);
                        if (!success)
                        {
                            //_log.Debug(this + ":Load Store Record : ExtractStoreEmpData = " + resMessage);
                            AddError(resMessage, "Validating Zip");
                            return false;
                        }
                        _STOREREC.Zip = resMessage;
                    }
                    if (string.IsNullOrEmpty(_STOREREC.Zip))
                        _STOREREC.Zip = "00000";

                    if (!Convert.IsDBNull(fdr["Country"]))
                        _STOREREC.Country = fdr["Country"].ToString();

                    //_log.Debug(this + ":Load Store Record : PHONE = " + fdr["PHONE"]);
                    if (!Convert.IsDBNull(fdr["PHONE"]))
                    {
                        success = ExtractStoreEmpData(fdr["Phone"].ToString(), out resMessage);
                        if (!success)
                        {
                            //_log.Debug(this + ":Load Store Record : ExtractStoreEmpData = " + resMessage);
                            AddError(resMessage, "Validating Phone");
                            return false;
                        }
                        _STOREREC.Phone = resMessage;
                    }

                    if (!Convert.IsDBNull(fdr["ReceiptXML"]))
                        _STOREREC.ReceiptXML = (byte[])fdr["ReceiptXML"];

                    if (!Convert.IsDBNull(fdr["PhoneExt"]))
                        _STOREREC.PhoneExt = fdr["PhoneExt"].ToString();

                    if (!Convert.IsDBNull(fdr["Fax"]))
                        _STOREREC.Fax = fdr["Fax"].ToString();

                    if (!Convert.IsDBNull(fdr["GSTNumber"]))
                        _STOREREC.GSTNumber = fdr["GSTNumber"].ToString();

                    if (!Convert.IsDBNull(fdr["CSTNumber"]))
                        _STOREREC.CSTNumber = fdr["CSTNumber"].ToString();

                    if (!Convert.IsDBNull(fdr["REGNumber"]))
                        _STOREREC.REGNumber = fdr["REGNumber"].ToString();

                    if (!Convert.IsDBNull(fdr["PANNumber"]))
                        _STOREREC.PANNumber = fdr["PANNumber"].ToString();

                    if (!Convert.IsDBNull(fdr["VATRegNum"]))
                        _STOREREC.VATRegNum = fdr["VATRegNum"].ToString();

                    if (!Convert.IsDBNull(fdr["VATNumber"]))
                        _STOREREC.VATNumber = fdr["VATNumber"].ToString();

                    if (!Convert.IsDBNull(fdr["LBLMGRKEY"]))
                        _STOREREC.LBLMGRKEY = fdr["LBLMGRKEY"].ToString();

                    if (!Convert.IsDBNull(fdr["RWDMGRKEY"]))
                        _STOREREC.RWDMGRKEY = fdr["RWDMGRKEY"].ToString();

                    if (!Convert.IsDBNull(fdr["CRDMGRKEY"]))
                        _STOREREC.CRDMGRKEY = fdr["CRDMGRKEY"].ToString();

                    if (!Convert.IsDBNull(fdr["SCRCMRKEY"]))
                        _STOREREC.SCRCMRKEY = fdr["SCRCMRKEY"].ToString();

                    if (!Convert.IsDBNull(fdr["PRGMGRKEY"]))
                        _STOREREC.PRGMGRKEY = fdr["PRGMGRKEY"].ToString();

                    if (!Convert.IsDBNull(fdr["INVMGRKEY"]))
                        _STOREREC.INVMGRKEY = fdr["INVMGRKEY"].ToString();

                    if (!Convert.IsDBNull(fdr["QBBINFKEY"]))
                        _STOREREC.QBBINFKEY = fdr["QBBINFKEY"].ToString();

                    if (!Convert.IsDBNull(fdr["PRGMGRKEY"]))
                        _STOREREC.PRGMGRKEY = fdr["PRGMGRKEY"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_1"]))
                        _STOREREC.HELP_1 = fdr["HELP_1"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_2"]))
                        _STOREREC.HELP_2 = fdr["HELP_2"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_3"]))
                        _STOREREC.HELP_3 = fdr["HELP_3"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_4"]))
                        _STOREREC.HELP_4 = fdr["HELP_4"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_5"]))
                        _STOREREC.HELP_5 = fdr["HELP_5"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_6"]))
                        _STOREREC.HELP_6 = fdr["HELP_6"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_7"]))
                        _STOREREC.HELP_7 = fdr["HELP_7"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_8"]))
                        _STOREREC.HELP_8 = fdr["HELP_8"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_9"]))
                        _STOREREC.HELP_9 = fdr["HELP_9"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_10"]))
                        _STOREREC.HELP_10 = fdr["HELP_10"].ToString();

                    if (!Convert.IsDBNull(fdr["HELP_11"]))
                        _STOREREC.HELP_11 = fdr["HELP_11"].ToString();

                    if (!Convert.IsDBNull(fdr["ISGSTTAXATION"]))
                        _STOREREC.IsGSTTaxation = Convert.ToInt16(fdr["ISGSTTAXATION"].ToString()) == 1;

                    if (!Convert.IsDBNull(fdr["LA_GUID"]))
                        _STOREREC.LA_GUID = Convert.ToInt16(fdr["LA_GUID"].ToString()) == 1;

                    if (!Convert.IsDBNull(fdr["GUID"]))
                        _STOREREC.GUID = fdr["GUID"].ToString();

                    if (!Convert.IsDBNull(fdr["STORE_GUID"]))
                        _STOREREC.STORE_GUID = fdr["STORE_GUID"].ToString();
                    if (!Convert.IsDBNull(fdr["LOCATION"]))
                        _STOREREC.LOCATION = fdr["LOCATION"].ToString();

                    if (!Convert.IsDBNull(fdr["IsHQStore"]))
                        _STOREREC.IsHQStore = Convert.ToBoolean(fdr["IsHQStore"].ToString());

                    if (!Convert.IsDBNull(fdr["DATABKLIC"]))
                    {
                        success = ExtractStoreEmpData(fdr["DATABKLIC"].ToString(), out resMessage);
                        if (!success)
                        {
                            AddError(resMessage, "Validating License Key");
                            return false;
                        }
                        _STOREREC.DATABKLIC = resMessage;
                    }

                    if (!Convert.IsDBNull(fdr["LA_GUID_1"]) && !string.IsNullOrEmpty(fdr["LA_GUID_1"].ToString()))
                    {
                        success = ExtractStoreEmpData(fdr["LA_GUID_1"].ToString(), out resMessage);
                        if (!success)
                        {
                            AddError(resMessage, "Validating CountryCode");
                            return false;
                        }
                        _STOREREC.CountryCode = resMessage;
                    }

                    if (!Convert.IsDBNull(fdr["AREACODE"]) && !string.IsNullOrEmpty(fdr["AREACODE"].ToString()))
                    {
                        success = ExtractStoreEmpData(fdr["AREACODE"].ToString(), out resMessage);
                        if (!success)
                        {
                            AddError(resMessage, "Validating AreaCode");
                            return false;
                        }
                        _STOREREC.AREACODE = resMessage;
                    }

                    if (!Convert.IsDBNull(fdr["GC_ZONE_ID"]))
                        _STOREREC.GC_ZONE_ID = Convert.ToInt32(fdr["GC_ZONE_ID"].ToString());

                    if (!Convert.IsDBNull(fdr["LP_ZONE_ID"]))
                        _STOREREC.LP_ZONE_ID = Convert.ToInt32(fdr["LP_ZONE_ID"].ToString());

                    if (!Convert.IsDBNull(fdr["GC_BIZ_ID"]))
                        _STOREREC.GC_BIZ_ID = Convert.ToInt32(fdr["GC_BIZ_ID"].ToString());

                    if (!Convert.IsDBNull(fdr["LocalUTCOffset"]))
                        _STOREREC.LocalUTCOffset = Convert.ToInt32(fdr["LocalUTCOffset"].ToString());

                    if (!Convert.IsDBNull(fdr["CLIENTID"]))
                        _STOREREC.CRM_CLIENTID = Convert.ToInt32(fdr["CLIENTID"].ToString());

                    try
                    {
                        if (!Convert.IsDBNull(fdr["DIVISIONGRP_ID"]))
                            _STOREREC.DivisionGrp_ID = Convert.ToInt32(fdr["DIVISIONGRP_ID"].ToString());
                    }
                    catch { }

                    RetVal = true;
                }

                #region SET NAME & ADDRESS DESCRIPTION

                _STOREREC.Address_Desc = _STOREREC.City;
                if (!string.IsNullOrEmpty(_STOREREC.State))
                    _STOREREC.Address_Desc = _STOREREC.Address_Desc + ", " + _STOREREC.State;
                if (!string.IsNullOrEmpty(_STOREREC.Zip))
                    _STOREREC.Address_Desc = _STOREREC.Address_Desc + " - " + _STOREREC.Zip;

                if (dtStore.Rows.Count <= 1)
                    _STOREREC.StoreName_Desc = _STOREREC.StoreName;
                else if (!string.IsNullOrEmpty(_STOREREC.LOCATION) && _STOREREC.LOCATION.ToUpper().Trim() != _STOREREC.City.ToUpper().Trim())
                    _STOREREC.Address_Desc = _STOREREC.Address_Desc + " => " + _STOREREC.LOCATION + "";
                else
                    _STOREREC.StoreName_Desc = _STOREREC.StoreName + " ( " + _STOREREC.Address_Desc + " )";

                #endregion

                DAOGlobals.DivisionGrp_ID = -1;
                DAOGlobals.lstDivision = new int[0];
                if (_STOREREC.DivisionGrp_ID > 0)
                {
                    DAOGlobals.DivisionGrp_ID = _STOREREC.DivisionGrp_ID;

                    DataTable dtDep = _DbMgr.GetDataTable("SELECT DepId FROM MST_DIVISION WHERE DIVISIONGRP_ID = " + _STOREREC.DivisionGrp_ID);
                    if (dtDep != null && dtDep.Rows.Count > 0)
                    {
                        for (int i = 0; i < dtDep.Rows.Count; i++)
                        {
                            Array.Resize(ref DAOGlobals.lstDivision, DAOGlobals.lstDivision.Length + 1);
                            DAOGlobals.lstDivision[DAOGlobals.lstDivision.Length - 1] = Convert.ToInt32(dtDep.Rows[i]["DepId"].ToString());
                        }
                    }
                }

                DAOGlobals.LocalUtcOffset = _STOREREC.LocalUTCOffset;

                #region MULTI-STORE SETTINGS

                #region Get Default Facility

                if (_DbMgr.ExecuteCountQuery("SELECT COUNT(*) FROM STK_FACILITY WHERE STOREID = '" + StoreID + "'") == 0)
                    _DbMgr.ExecuteNonQuery("INSERT INTO STK_FACILITY(FACILITYNAME, ISPREDEFINED, STOREID) VALUES ('In-Store', 1, '" + _STOREREC.StoreID + @"')");

                string strFAC = _DbMgr.ExecuteScalar("SELECT FACILITYID FROM STK_FACILITY WHERE STOREID = '" + StoreID + "' AND ISPREDEFINED = 1 LIMIT 1");

                if (strFAC != null && !string.IsNullOrEmpty(strFAC.ToString()))
                    _STOREREC.DefaultFacilityID = Convert.ToInt32(strFAC.ToString());
                else
                    _STOREREC.DefaultFacilityID = 1;

                #endregion

                #endregion

                return RetVal;
            }
            catch (Exception ex)
            {
                string _Error = ex.Message;
                //_log.Error(ex.Message + ":" + this + ":Load Store Record");
                AddError(ex.Message, "Load Store Record");
                return false;
            }
            finally
            {
                //_log.Debug("Load Store Record : END");
            }
        }

        public DataTable GetStoreDateFormat(string STOREID)
        {
            string sql = $"SELECT SETUPVALUESTR FROM MST_SETUPVALUES   WHERE STOREID='{STOREID}' AND SETUPVARIABLE = 'DATEFORMAT'";

            DataTable dt = _DbMgr.GetDataTable(sql);
            if (dt == null || dt.Rows.Count == 0)
            {
                return null;
            }

            return dt;
        }

    }
}
