using AEPOS.DAO;
using AEPOS.Model;
using Npgsql;
using NpgsqlTypes;
using Sypram.Common;
using System.Data;
using System.Text.RegularExpressions;
using Telerik.Windows.Documents.Spreadsheet.Expressions.Functions;

namespace AEPOS.PriceCheck.DAO
{
    public class ITEMINFO_DAL
    {
        DBManager _DbMgr = null;

        public ITEMINFO_DAL(DBManager db)
        {
            _DbMgr = db;
        }

        public DataTable GetItemList(string UPC)
        {
            try
            {
                string sql = @$"SELECT * from ITM_ITEMINFO WHERE MAINUPC ILIKE '{UPC}'";

                DataTable dt = _DbMgr.GetDataTable(sql);
                if (dt == null || dt.Rows.Count == 0)
                {
                    return null;
                }

                return dt;
            }
            catch (Exception ex)
            {
                //AddError(ex.Message, "");
                return null;
            }
        }
        public DataTable GetItemListByName(string name)
        {
            try
            {
				//(CASE WHEN DONOTPROMPTMTX = true THEN I.MATRIXNAME
				//		   ELSE CONCAT(I.ITEMNAME, ' ', COALESCE(S.SIZENAME, ''), ' ', COALESCE(P.PACKNAME, ''))

				//	   END) AS ITEMNAME,
				//	SKU
				//string sql = @$"SELECT * from ITM_ITEMINFO WHERE ITEMNAME ILIKE '%{name}%' OR MAINUPC ILIKE '%{name}%'";
				string sql = @"SELECT *
                    FROM ITM_ITEMINFO I
                    LEFT JOIN ITM_ITEMSIZE S ON S.SIZEID = I.SIZEID
                    LEFT JOIN ITM_ITEMPACK P ON P.PACKID = I.PACKID
                    WHERE (I.NONACTIVE = 0 OR I.NONACTIVE IS NULL) 
                    AND I.ITEMLEVEL = (CASE WHEN I.DONOTPROMPTMTX = true THEN 0 ELSE I.ITEMLEVEL END)
                    AND I.KEYWORD ILIKE '%" + name.Replace("'", "\'\'") + @"%'";

				DataTable dt = _DbMgr.GetDataTable(sql);
                if (dt == null || dt.Rows.Count == 0)
                {
                    return null;
                }

                return dt;
            }
            catch (Exception ex)
            {
                //AddError(ex.Message, "");
                return null;
            }


        }

		public DataTable GetItemByCriteria(string SerachVal, int VendorID, short SerachType, bool bWithSizePack)
		{
			string Col_PricePerUnit = ModelGlobals.Get_PricePerUnit();

			//ResetErrors();

			string Col_TotalQty = ModelGlobals.Get_TotalQty();

			try
			{
				string sql;
				int SKU = 0;

				if (SerachType == 0)
				{
					SKU = System.Convert.ToInt32(SerachVal);
				}

				if (bWithSizePack)
				{
					sql = @"SELECT I.SKU, COALESCE(I.NonActive,0) AS NonActive, I.MainUPC, IU.UPC,IV.ProductCode,ItemName,
                    ItemLevel, ControlSKU, ItemType, UNITTYPE,                            
                    COALESCE(I.MPackID, 0) AS MPackID,
                    COALESCE(I.NONSTOCKITEM,0) AS NONSTOCKITEM,
                    COALESCE(D.DEPNAME,'') AS DepName,
                    COALESCE(C.CATNAME, '') AS CatName, 
                    COALESCE(I.CaseCost,0) AS CaseCost, CASE WHEN I.BuyAsCase = 1 THEN 1 ELSE 0 END AS BuyAsCase,
                    MatrixSKU, MATRIXNAME, MatrixTitle1, MatrixTitle2, MatrixTitle3, MatrixValue1, MatrixValue2, MatrixValue3,
                    " + Col_PricePerUnit + @" AS PricePerUnit, COALESCE(I.SizeID,0) AS SizeID, COALESCE(SizeName,'') AS SizeName,
                    COALESCE(I.PackID,0) AS PackID, COALESCE(PackName,'') AS PackName, UnitsInPack,
                    Description, BuyDownAmt, " + Col_TotalQty + @" AS TotalQty, COALESCE(CaseUnits, 0) AS CaseUnits, 
                    I.CaseCost, I.LastCaseCost, I.LastUnitCost, I.CurrentCost, UPPER(KEYWORD) AS KEYWORD
                    FROM (ITM_ITEMINFO I 
                    INNER JOIN ITM_ITEMUPC IU ON I.SKU = IU.SKU 
                    LEFT JOIN ITM_ITEMSIZE IZ ON I.SizeID = IZ.SizeID 
                    LEFT JOIN ITM_ITEMPACK IP ON I.PackID = IP.PackID
                    LEFT JOIN MST_DEPARTMENT D ON I.DEPID = D.DEPID
                    LEFT JOIN MST_CATEGORY C ON I.CATID = C.CATID)";
				}
				else
				{
					sql = @"SELECT I.SKU, COALESCE(I.NonActive,0) AS NonActive, I.MainUPC, IU.UPC,IV.ProductCode ,ItemName,
                    ItemLevel, ControlSKU, ItemType, UNITTYPE,
                    MatrixSKU, MATRIXNAME, MatrixTitle1, MatrixTitle2, MatrixTitle3, MatrixValue1, MatrixValue2, MatrixValue3,
                    " + Col_PricePerUnit + @" AS PricePerUnit, Description, BuyDownAmt," + Col_TotalQty + @" AS TotalQty, COALESCE(CaseUnits, 0) AS CaseUnits, 
                    I.CaseCost, I.LastCaseCost, I.LastUnitCost, I.CurrentCost, UPPER(KEYWORD) AS KEYWORD
                    FROM ITM_ITEMINFO I INNER JOIN ITM_ITEMUPC IU ON I.SKU = IU.SKU";
				}

				if (SerachType == 1) //--- By Product Code
				{
					sql += " INNER JOIN ITM_ITEMVENDOR IV ON I.SKU = IV.SKU AND UPPER(IV.ProductCode) = '" + SerachVal.Trim().ToUpper() + "'";

					if (VendorID > 0)
						sql += " AND IV.VendorID = " + VendorID + "";

					sql += " WHERE IU.UPC IS NOT NULL AND IU.UPC <> ''";
				}
				else //-- SKU
				{
					if (VendorID > 0)  //Check This Case During Execution
					{
						sql += @" WHERE IU.UPC IS NOT NULL AND IU.UPC <> '' ";
						sql += " AND I.SKU IN (SELECT V.SKU FROM ITM_ITEMVENDOR V WHERE V.SKU = I.SKU AND V.VendorID = " + VendorID + " LIMIT 1)";
						if (SerachType == 0)
							sql += " AND I.SKU = " + SKU;
					}
					else
					{
						if (bWithSizePack)
						{
							sql = @"SELECT I.SKU, COALESCE(I.NonActive,0) AS NonActive, I.MainUPC, IU.UPC,
                            I.CaseCost," + Col_PricePerUnit + @" AS PricePerUnit,'' AS ProductCode,
                            ItemLevel, ControlSKU, ItemType, UNITTYPE,
                            COALESCE(I.MPackID,0) AS MPackID,
                            ItemName,COALESCE(I.NONSTOCKITEM,0) AS NONSTOCKITEM,I.ItemType,
                            MatrixSKU, MATRIXNAME, MatrixTitle1, MatrixTitle2, MatrixTitle3, MatrixValue1, MatrixValue2, MatrixValue3,
                            COALESCE(I.SizeID,0) AS SizeID, 
                            COALESCE(SizeName,'') AS SizeName,
                            COALESCE(I.PackID,0) AS PackID, 
                            COALESCE(PackName,'') AS PackName,
                            COALESCE(D.DEPNAME,'') AS DepName,
                            COALESCE(C.CATNAME, '') AS CatName, 
                            Description, BuyDownAmt," + Col_TotalQty + @" AS TotalQty, UnitsInPack,
                            COALESCE(I.CaseCost,0) AS CaseCost, CASE WHEN I.BuyAsCase = 1 THEN 1 ELSE 0 END AS BuyAsCase, COALESCE(CaseUnits, 0) AS CaseUnits, 
                            I.CaseCost, I.LastCaseCost, I.LastUnitCost, I.CurrentCost, UPPER(KEYWORD) AS KEYWORD 
                            FROM ITM_ITEMINFO I 
                            INNER JOIN ITM_ITEMUPC IU ON I.SKU = IU.SKU 
                            LEFT JOIN ITM_ITEMSIZE IZ ON I.SizeID = IZ.SizeID 
                            LEFT JOIN ITM_ITEMPACK IP ON I.PackID = IP.PackID
                            LEFT JOIN MST_DEPARTMENT D ON I.DEPID = D.DEPID
                            LEFT JOIN MST_CATEGORY C ON I.CATID = C.CATID";
						}
						else
						{
							sql = @"SELECT I.SKU, COALESCE(I.NonActive,0) AS NonActive, I.MainUPC, IU.UPC,
                            " + Col_PricePerUnit + @" AS PricePerUnit, '' AS ProductCode, ItemName, Description, 
                            ItemLevel, ControlSKU, ItemType, UNITTYPE,
                            MatrixSKU, MATRIXNAME, MatrixTitle1, MatrixTitle2, MatrixTitle3, MatrixValue1, MatrixValue2, MatrixValue3,
                            BuyDownAmt, " + Col_TotalQty + @" AS TotalQty, COALESCE(CaseUnits, 0) AS CaseUnits, 
                            I.CaseCost, I.LastCaseCost, I.LastUnitCost, I.CurrentCost, UPPER(KEYWORD) AS KEYWORD 
                            FROM ITM_ITEMINFO I 
                            INNER JOIN ITM_ITEMUPC IU ON I.SKU = IU.SKU";
						}

						sql += " WHERE IU.UPC IS NOT NULL AND IU.UPC <> '' ";
						if (SerachType == 0)
							sql += "  AND I.SKU = " + SKU;
					}
				}

				sql += " LIMIT 1";

				DataTable dt = _DbMgr.GetDataTable_NoTimeout(sql);
				if (dt == null)
				{
					//AddError(_DbMgr.GetErrorMessage(), "");
					return null;
				}

				dt.TableName = "Item";

				return dt;
			}
			catch (Exception ex)
			{
				//AddError("Error occured when retrieving Item Record: " + ex.Message, this.GetType() + ": GetItemByCriteria");
				return null;
			}
		}

		public DataTable GetItemUPCByItem(int SKU)
		{
			//ResetErrors();
			DataTable dt = _DbMgr.GetDataTable("SELECT UPC FROM ITM_ITEMUPC WHERE SKU = " + SKU + "");
			if (dt == null)
			{
				//AddError(_DbMgr.GetErrorMessage(), "");
				return null;
			}

			return dt;
		}

		public int SearchItemByUPC(string UPC, string UPC_EtoA, out string ModiFiedUPC, out int ControlSKU, out bool ShowDialog, out int MatrixSKU, out int ScanCodeID, bool ExtendedSearch = false, bool FixUPC = false, string NonPLUFlag = "2", int NonPLULength = 5, bool searchUPCinSKU = true)
		{
			ControlSKU = 0;
			ShowDialog = false;
			MatrixSKU = 0;
			ScanCodeID = 0;
			ModiFiedUPC = "";
			try
			{
				if (UPC.Trim().Length < 4)
				{
					ExtendedSearch = false;
					FixUPC = false;
				}

				UPC = UPC.ToUpper();
				string uPC = UPC;
				bool extendedSearch = ExtendedSearch;
				int nonPLULength = NonPLULength;
				int num = SearchUPCinDB(uPC, UPC_EtoA, extendedSearch, out ModiFiedUPC, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID, searchUPCinSKU, NonPLUFlag, nonPLULength);
				if (num == -1)
				{
					if (UPC.Trim().Length < 4)
					{
						return 0;
					}

					return -1;
				}

				if (num == 0 && ExtendedSearch && UPC.Substring(2, UPC.Length - 2).Contains("00000"))
				{
					if (!string.IsNullOrEmpty(UPC_EtoA))
					{
						UPC = UPC_EtoA;
					}

					string text = UPC.Replace("00000", "");
					if (text.Length >= 5)
					{
						num = SearchUPCinDB(text, "", ExtendedSearch: true, out ModiFiedUPC, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID, SearchUPCinSKU: true, NonPLUFlag, NonPLULength);
					}

					if (num == -1)
					{
						if (UPC.Trim().Length < 4)
						{
							return 0;
						}

						return -1;
					}
				}

				if (num != 0 && FixUPC && !string.IsNullOrEmpty(ModiFiedUPC))
				{
					NpgsqlCommand npgsqlCommand = new NpgsqlCommand("", _DbMgr.DBConn);
					npgsqlCommand.CommandText = "UPDATE ITM_ITEMUPC Set UPC = '" + UPC + "', SKUNOTE = 'DAO-SearchItemByUPC' WHERE SKU = " + num + " AND UPC LIKE '%" + ModiFiedUPC + "%'";
					NpgsqlCommand npgsqlCommand2 = npgsqlCommand;
					npgsqlCommand2.ExecuteNonQuery();
					npgsqlCommand2.CommandText = "UPDATE ITM_ITEMINFO Set MainUPC = '" + UPC + "' WHERE SKU = '" + num + "'";
					npgsqlCommand2.ExecuteNonQuery();
				}

				return num;
			}
			catch (Exception ex)
			{
				//AddError(ex.Message, this?.ToString() + " : SearchItemByUPC");
				return -1;
			}
		}

		protected int SearchUPCinDB(NpgsqlCommand cmd, string UPC, bool ExtendedSearch, out int ControlSKU, out bool ShowDialog, out int MatrixSKU, out int ScanCodeID)
		{
			ControlSKU = 0;
			ShowDialog = false;
			MatrixSKU = 0;
			ScanCodeID = 0;
			NpgsqlDataReader npgsqlDataReader = null;
			try
			{
				cmd.Parameters[0].Value = UPC;
				using (npgsqlDataReader = cmd.ExecuteReader())
				{
					if (npgsqlDataReader.Read())
					{
						ControlSKU = (int)npgsqlDataReader.GetValue(1);
						ShowDialog = (int)npgsqlDataReader.GetValue(2) == 1;
						MatrixSKU = (int)npgsqlDataReader.GetValue(3);
						ScanCodeID = (int)npgsqlDataReader.GetValue(4);
						return (int)npgsqlDataReader.GetValue(0);
					}
				}

				if (ExtendedSearch && UPC.Length >= 4 && !UPC.StartsWith("%") && !UPC.EndsWith("%") && !cmd.CommandText.ToUpper().Contains("WHERE UPPER(UPC) LIKE"))
				{
					string commandText = cmd.CommandText;
					try
					{
						cmd.CommandText = cmd.CommandText.Replace("WHERE UPPER(UPC)", "WHERE LENGTH(UPC) > 4 AND UPPER(SUBSTRING(UPC,1, LENGTH(UPC) - 1))");
						cmd.Parameters[0].Value = UPC;
						using (npgsqlDataReader = cmd.ExecuteReader())
						{
							if (npgsqlDataReader.Read())
							{
								ControlSKU = (int)npgsqlDataReader.GetValue(1);
								ShowDialog = (int)npgsqlDataReader.GetValue(2) == 1;
								MatrixSKU = (int)npgsqlDataReader.GetValue(3);
								ScanCodeID = (int)npgsqlDataReader.GetValue(4);
								return (int)npgsqlDataReader.GetValue(0);
							}
						}
					}
					catch
					{
					}
					finally
					{
						cmd.CommandText = commandText;
					}
				}

				return -1;
			}
			catch (Exception ex)
			{
				return -1;
			}
			finally
			{
				if (npgsqlDataReader != null)
				{
					npgsqlDataReader.Close();
					npgsqlDataReader.Dispose();
				}
			}
		}

		public int SearchUPCinDB(string UPC, string UPC_EtoA, bool ExtendedSearch, out string ModiFiedUPC, out int ControlSKU, out bool ShowDialog, out int MatrixSKU, out int ScanCodeID, bool SearchUPCinSKU = true, string NonPLUFlag = "2", int NonPLULength = 5)
		{
			ModiFiedUPC = "";
			ControlSKU = 0;
			ShowDialog = false;
			MatrixSKU = 0;
			ScanCodeID = 0;
			if (string.IsNullOrEmpty(UPC))
			{
				//AddError("UPC must not be blank", "");
				return -1;
			}

			NpgsqlCommand npgsqlCommand = null;
			try
			{
				UPC = UPC.ToUpper();
				npgsqlCommand = new NpgsqlCommand("", _DbMgr.DBConn);
				if (SearchUPCinSKU && int.TryParse(UPC, out var result) && UPC.Length <= 6)
				{
					npgsqlCommand.CommandText = "SELECT SKU, CASE WHEN PACKID IS NOT NULL THEN COALESCE(CONTROLSKU, 0) ELSE 0 END, \r\n                                            COALESCE(SHOWDIALOG,0), COALESCE(MATRIXSKU,0) \r\n                                            FROM ITM_ITEMINFO WHERE SKU = " + result + " ";
					using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
					if (npgsqlDataReader.Read())
					{
						ControlSKU = (int)npgsqlDataReader.GetValue(1);
						ShowDialog = (int)npgsqlDataReader.GetValue(2) == 1;
						MatrixSKU = (int)npgsqlDataReader.GetValue(3);
						return (int)npgsqlDataReader.GetValue(0);
					}
				}

				string commandText = (npgsqlCommand.CommandText = "SELECT IP.SKU, CASE WHEN I.PACKID IS NOT NULL THEN COALESCE(I.CONTROLSKU, 0) ELSE 0 END, \r\n                                   COALESCE(I.SHOWDIALOG,0), COALESCE(I.MATRIXSKU,0), \r\n                                   COALESCE(IP.ITEMGROUPID,0) \r\n                                   FROM ITM_ITEMUPC IP \r\n                                   INNER JOIN ITM_ITEMINFO I ON IP.SKU = I.SKU \r\n                                   WHERE UPPER(UPC) = @UPC");
				npgsqlCommand.Parameters.Add("@UPC", NpgsqlDbType.Varchar, 100);
				npgsqlCommand.Prepare();
				int num = SearchUPCinDB(npgsqlCommand, UPC, ExtendedSearch, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
				if (num > 0)
				{
					return num;
				}

				if (!string.IsNullOrEmpty(UPC_EtoA))
				{
					num = SearchUPCinDB(npgsqlCommand, UPC_EtoA, ExtendedSearch, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
					if (num > 0)
					{
						return num;
					}

					UPC = UPC_EtoA;
				}

				string text2 = "";
				if (UPC.Length >= 10 && NonPLUFlag.Length >= 1 && UPC.IsNumeric() && UPC.Substring(0, NonPLUFlag.Length) == NonPLUFlag && UPC.Length > NonPLUFlag.Length + NonPLULength)
				{
					text2 = UPC;
					string uPC = text2.Substring(0, NonPLUFlag.Length + NonPLULength);
					npgsqlCommand.CommandText = "SELECT IP.SKU, CASE WHEN I.PACKID IS NOT NULL THEN COALESCE(I.CONTROLSKU, 0) ELSE 0 END, \r\n                                            COALESCE(I.SHOWDIALOG,0), COALESCE(I.MATRIXSKU,0),\r\n                                            COALESCE(IP.ITEMGROUPID,0) \r\n                                            FROM ITM_ITEMUPC IP \r\n                                            INNER JOIN ITM_ITEMINFO I ON IP.SKU = I.SKU \r\n                                            WHERE UPPER(UPC) LIKE @UPC || '%' AND ISNONPLU = 1";
					num = SearchUPCinDB(npgsqlCommand, uPC, ExtendedSearch, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
					if (num > 0)
					{
						ModiFiedUPC = "";
						return num;
					}

					npgsqlCommand.CommandText = commandText;
				}

				if (!ExtendedSearch)
				{
					//AddError("UPC not found", GetType()?.ToString() + " : SearchUPCinDB");
					return 0;
				}

				bool flag = false;
				char c = ' ';
				if (UPC.IsNumeric())
				{
					if (UPC.StartsWith("0"))
					{
						text2 = UPC.Substring(1);
						num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
						if (num > 0)
						{
							ModiFiedUPC = text2;
							return num;
						}
					}
				}
				else
				{
					if (!new Regex("^[a-zA-Z]").IsMatch(UPC[0].ToString()) || !UPC.Substring(1).IsNumeric() || UPC.Length < 6)
					{
						//AddError("UPC not found", GetType()?.ToString() + " : SearchUPCinDB");
						return 0;
					}

					flag = true;
					c = UPC[0];
					UPC = UPC.Substring(1);
				}

				if (!flag)
				{
					text2 = "0" + UPC;
					npgsqlCommand.Parameters["@UPC"].Value = text2;
					num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
					if (num > 0)
					{
						ModiFiedUPC = text2;
						return num;
					}
				}
				else
				{
					text2 = c + "0" + UPC;
					num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
					if (num > 0)
					{
						ModiFiedUPC = text2;
						return num;
					}
				}

				if (!flag)
				{
					text2 = UPC.Substring(0, UPC.Length - 1);
					num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
					if (num > 0)
					{
						ModiFiedUPC = text2;
						return num;
					}
				}
				else
				{
					text2 = c + UPC.Substring(0, UPC.Length - 1);
					num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
					if (num > 0)
					{
						ModiFiedUPC = text2;
						return num;
					}
				}

				if (!flag)
				{
					text2 = "0" + UPC.Substring(0, UPC.Length - 1);
					num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
					if (num > 0)
					{
						ModiFiedUPC = text2;
						return num;
					}
				}
				else
				{
					text2 = c + "0" + UPC.Substring(0, UPC.Length - 1);
					num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
					if (num > 0)
					{
						ModiFiedUPC = text2;
						return num;
					}
				}

				if (UPC.Length >= 6)
				{
					if (UPC.StartsWith("0"))
					{
						if (!flag)
						{
							text2 = UPC.Substring(1);
							num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
							if (num > 0)
							{
								ModiFiedUPC = text2;
								return num;
							}
						}
						else
						{
							text2 = c + UPC.Substring(1);
							num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
							if (num > 0)
							{
								ModiFiedUPC = text2;
								return num;
							}
						}

						if (!flag)
						{
							text2 = UPC.Substring(1, UPC.Length - 2);
							num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
							if (num > 0)
							{
								ModiFiedUPC = text2;
								return num;
							}
						}
						else
						{
							text2 = c + UPC.Substring(1, UPC.Length - 2);
							num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
							if (num > 0)
							{
								ModiFiedUPC = text2;
								return num;
							}
						}
					}

					if (!flag)
					{
						text2 = UPC.Substring(1, UPC.Length - 2);
						num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
						if (num > 0)
						{
							ModiFiedUPC = text2;
							return num;
						}
					}
					else
					{
						text2 = c + UPC.Substring(1, UPC.Length - 2);
						num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
						if (num > 0)
						{
							ModiFiedUPC = text2;
							return num;
						}
					}

					npgsqlCommand.CommandText = "SELECT IP.SKU, CASE WHEN I.PACKID IS NOT NULL THEN COALESCE(I.CONTROLSKU, 0) ELSE 0 END, COALESCE(I.SHOWDIALOG,0), COALESCE(I.MATRIXSKU,0), COALESCE(IP.ITEMGROUPID,0) \r\n                                        FROM ITM_ITEMUPC IP \r\n                                        INNER JOIN ITM_ITEMINFO I ON IP.SKU = I.SKU \r\n                                        WHERE UPPER(UPC) LIKE '%' || @UPC || '%'";
					num = SearchUPCinDB(npgsqlCommand, UPC, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
					if (num > 0)
					{
						ModiFiedUPC = "";
						return num;
					}

					if (UPC.StartsWith("0"))
					{
						text2 = UPC.Substring(1, UPC.Length - 1);
						num = SearchUPCinDB(npgsqlCommand, text2, ExtendedSearch: true, out ControlSKU, out ShowDialog, out MatrixSKU, out ScanCodeID);
						if (num > 0)
						{
							ModiFiedUPC = "";
							return num;
						}
					}
				}

				return 0;
			}
			catch (Exception ex)
			{
				//AddError(ex.Message, GetType()?.ToString() + " : SearchUPCinDB");
				return -1;
			}
			finally
			{
				npgsqlCommand.Dispose();
			}
		}

		public bool FixUPC(int SKU, string newUPC, string oldUPC)
		{
			try
			{
				if (string.IsNullOrEmpty(newUPC) || string.IsNullOrEmpty(oldUPC))
				{
					return false;
				}

				NpgsqlCommand npgsqlCommand = new NpgsqlCommand("UPDATE ITM_ITEMUPC Set UPC = '" + newUPC + "', SKUNOTE = 'DAO-FixUPC' WHERE SKU = " + SKU + " AND UPC LIKE '%" + oldUPC + "%'", _DbMgr.DBConn);
				npgsqlCommand.ExecuteNonQuery();
				npgsqlCommand = new NpgsqlCommand("UPDATE ITM_ITEMINFO Set MAINUPC = '" + newUPC + "', UPDATEDATETIME = (now() at time zone 'utc') + interval '" + DAOGlobals.LocalUtcOffset + " minutes' WHERE SKU = '" + SKU + "'", _DbMgr.DBConn);
				npgsqlCommand.ExecuteNonQuery();
				return true;
			}
			catch (Exception ex)
			{
				//AddError(ex.Message, GetType()?.ToString() + " : FixUPCByItem");
				return false;
			}
		}

	}
}
