using AEPOS.DAO;
using AEPOS.PriceCheck.DAO;
using AEPOS.PriceCheck.PopUpPages;
using AEPOS.PriceCheck.SQLiteClasses.Models;
using System.Data;

namespace AEPOS.PriceCheck.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
		this.Loaded += LoginPageLoaded;
		entryGUID.Focus();
	}

	bool isCapsLock_Enable = false;
	bool isShift_Pressed = false;
	string cutText;
	private int _cursorPosition = 0;
	private bool isLoginKeyboardVisible = false;
	NetworkAccess accessType = Connectivity.Current.NetworkAccess;
	public async void CheckInternetAndExit()
	{
		if (Connectivity.NetworkAccess != NetworkAccess.Internet)
		{
			bool status = await DisplayAlert("Internet is Required!!", "Please Check your Internet", "Exit", "Cancle");
			if (status)
			{
				CloseApp();
			}
			return;
		}
	}
	private void CloseApp()
	{
#if ANDROID
    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#elif IOS
		// iOS does not allow programmatically closing the app.
		System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
#else
		System.Environment.Exit(0);
#endif
	}

	private async void LoginPageLoaded(object sender, EventArgs e)
	{
		if (Globals.InitialLoad == true)
		{
			try
			{

				loadingPage.IsVisible = true;
				mainLoginPage.IsVisible = false;

				List<StoreInfo> lststore = await App.cacheDataRepo.ExecuteSelectQueryAsync<StoreInfo>("SELECT * FROM STOREINFO");

				if (lststore.Count > 0 && !string.IsNullOrEmpty(lststore[0].ConnectionString))
				{
					Globals.SetServer_ConnStr = lststore[0].ConnectionString;

					Globals.GUID = lststore[0].StoreGUID;
					Globals.StoreName = lststore[0].StoreName;
					Globals.Store_ID = lststore[0].StoreID;
					if (Globals.ConnectToDatabase(true) == false)
					{
						Globals.IsServerConnected = false;
						CheckInternetAndExit();
						loadingPage.IsVisible = false;
						mainLoginPage.IsVisible = true;
					}
					else
					{
						await Navigation.PushAsync(new HomePage());
						loadingPage.IsVisible = false;
						mainLoginPage.IsVisible = true;
					}
				}
				else
				{
					//GuIdPopUp.IsOpen = true;
					loadingPage.IsVisible = false;
					mainLoginPage.IsVisible = true;

					return;
				}
			}
			catch (Exception ex)
			{
				loadingPage.IsVisible = false;
				mainLoginPage.IsVisible = true;

				await DisplayAlert("Error!", $"{ex.Message}", "OK");
			}
		}
	}

	private async Task<bool> LoadingData()
	{
		try
		{
			//loginLoadingPopup.Show(); // Show the loading popup
			if (Globals.IsServerConnected == true)
			{
				#region Load Store Record

				STORE_DAL St_dal = new STORE_DAL(Globals.DBServer, true);
				if (!St_dal.LoadFirstStoreRecord())
				{
					Globals.StoreREC = null;
					await DisplayAlert("Error!", "Error in Loading store Data: " + St_dal.GetErrorMsg(), "OK");
					return false;
				}

				Globals.StoreREC = St_dal.StoreRecord;

				DataTable dts = St_dal.GetStoreDateFormat(Globals.Store_ID);
				string dateformat = string.Empty;

				if (dts == null)
				{
					dateformat = "dd/MM/yyyy hh:mm:ss tt";
				}
				else
				{
					string str = dts.Rows[0]["SETUPVALUESTR"].ToString();
					dateformat = $"{str} hh:mm:ss tt";
				}

				Globals.StrDateFormat = dateformat;
				await App.cacheDataRepo.SaveStoreInfoData();
				#endregion
			}
			else
			{
				//*** Load from Local
				StoreInfo store = await App.cacheDataRepo.LoadStore();
				if (store == null)
				{
					//alertWithOneBtn.Show("Error !", "Store data not found in local cache.");
					return false;
				}
				Globals.StoreREC = new Store_REC();
				Globals.StoreREC.StoreID = store.StoreID;
				Globals.StoreREC.STORE_GUID = store.StoreGUID;
				Globals.StoreREC.StoreName = store.StoreName;
				Globals.StoreREC.City = store.City;
				Globals.StoreREC.LOCATION = store.Location;
				Globals.StrDateFormat = store.STDateFormat;
			}

			#region Load Login User For Remember Me

			//List<UserInfo> users = await App.cacheDataRepo.ExecuteSelectQueryAsync<UserInfo>("SELECT * FROM USERINFO");

			//if (users != null && users.Count > 0 && users[0].IsRememberChk)
			//{
			//    entryUser.Text = users[0].USERID;
			//    entryPass.Text = users[0].PASSWORD;
			//    ckhrememberme.IsChecked = true;
			//}

			#endregion

			//SetLoginScreen();
			return true;
		}
		catch (Exception ex)
		{
			//alertWithOneBtn.Show("Error !", $"{ex.Message}");
			return false;
		}
	}

	private async void ValidateStoreCode(object sender, EventArgs e)
	{
		try
		{
			loadingpopup.Show();
			Login_KeyboardLayout.IsVisible = false;
			if (string.IsNullOrEmpty(entryGUID.Text))
			{
				await DisplayAlert("Alert !", "Enter GU-ID", "ok");
				return;
			}
			string guId = entryGUID.Text.Trim();

			if (await Globals.GetConnectionString(guId) == false)
			{
				await DisplayAlert("Failed !", "Invalid GUID.", "ok");
				//loginLoadingPopup.Hide(); // Hide loading popup if the connection fails
				return;
			}
			entryGUID.Text = "";

			if (await LoadingData() == false)
			{
				Application.Current.Quit();
				//loginLoadingPopup.Hide(); // Hide loading popup if an error occurs
				return;
			}
			await Navigation.PushAsync(new HomePage());
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error !", $"{ex.Message}", "ok");
			return;
		}
		finally
		{
			loadingpopup.Hide();
		}
	}

	private void Login_keyboardbtn_clicked(object sender, EventArgs e)
	{
		try
		{
			isLoginKeyboardVisible = !isLoginKeyboardVisible;
			Login_KeyboardLayout.IsVisible = isLoginKeyboardVisible;
			//entryGUID.Text = string.Empty;
		}
		catch (Exception ex)
		{
			DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}

	#region keyborad methods
	private void BtnCloseKeyboard(object sender, EventArgs e)
	{
		try
		{
			isLoginKeyboardVisible = !isLoginKeyboardVisible;
			Login_KeyboardLayout.IsVisible = isLoginKeyboardVisible;
			entryGUID.Focus();
		}
		catch (Exception ex)
		{
			DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
	private void Backspace_Tap(object sender, EventArgs e)
	{
		try
		{
			if (entryGUID.Text != null)
			{
				if (entryGUID.Text.Length > 0)
				{
					int cursorPosition = entryGUID.CursorPosition;
					if (cursorPosition > 0)
					{
						string originalText = entryGUID.Text ?? string.Empty;
						string newText = originalText.Remove(cursorPosition - 1, 1);
						entryGUID.Text = newText;
						entryGUID.CursorPosition = cursorPosition - 1;
					}
					entryGUID.Focus();
				}
			}
		}
		catch (Exception ex)
		{
			DisplayAlert("Alert !", $"{ex.Message}", "ok");
			return;
		}
	}
	private void CapsLock_Tap(object sender, EventArgs e)
	{
		try
		{
			if (isCapsLock_Enable == true)
			{
				btnCaps.BorderColor = Colors.Black;
				btnCaps.BorderWidth = 0;

				isCapsLock_Enable = false;
				LetA.Text = "a";
				LetB.Text = "b";
				LetC.Text = "c";
				LetD.Text = "d";
				LetE.Text = "e";
				LetF.Text = "f";
				LetG.Text = "g";
				LetH.Text = "h";
				LetI.Text = "i";
				LetJ.Text = "j";
				LetK.Text = "k";
				LetL.Text = "l";
				LetM.Text = "m";
				LetN.Text = "n";
				LetO.Text = "o";
				LetP.Text = "p";
				LetQ.Text = "q";
				LetR.Text = "r";
				LetS.Text = "s";
				LetT.Text = "t";
				LetU.Text = "u";
				LetV.Text = "v";
				LetW.Text = "w";
				LetX.Text = "x";
				LetY.Text = "y";
				LetZ.Text = "z";
			}
			else
			{
				btnCaps.BorderColor = Colors.Black;
				btnCaps.BorderWidth = 2;
				isCapsLock_Enable = true;
				LetA.Text = "A";
				LetB.Text = "B";
				LetC.Text = "C";
				LetD.Text = "D";
				LetE.Text = "E";
				LetF.Text = "F";
				LetG.Text = "G";
				LetH.Text = "H";
				LetI.Text = "I";
				LetJ.Text = "J";
				LetK.Text = "K";
				LetL.Text = "L";
				LetM.Text = "M";
				LetN.Text = "N";
				LetO.Text = "O";
				LetP.Text = "P";
				LetQ.Text = "Q";
				LetR.Text = "R";
				LetS.Text = "S";
				LetT.Text = "T";
				LetU.Text = "U";
				LetV.Text = "V";
				LetW.Text = "W";
				LetX.Text = "X";
				LetY.Text = "Y";
				LetZ.Text = "Z";
			}
		}
		catch (Exception ex)
		{
			DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
	private void OnKeyClicked(object sender, EventArgs e)
	{
		try
		{
			if (sender is Button button)
			{
				string KeyText;
				if (isCapsLock_Enable == true)
				{
					KeyText = button.Text.ToUpper();
				}
				else
				{
					KeyText = button.Text.ToLower();
				}

				entryGUID.Focus();
				InsertTextAtCursor(KeyText);


			}
		}
		catch (Exception ex)
		{
			DisplayAlert("Error !", $"{ex.Message}", "ok");
		}
	}
	private void InsertTextAtCursor(string text)
	{
		try
		{
			int cursorPosition = entryGUID.CursorPosition;
			string currentText = entryGUID.Text;

			if (currentText == null)
			{
				currentText = string.Empty;
			}

			string newText = currentText.Insert(cursorPosition, text);
			entryGUID.Text = newText;
			entryGUID.CursorPosition = cursorPosition + text.Length;
		}
		catch (Exception ex)
		{
			DisplayAlert("Alert !", $"{ex.Message}", "ok");
			return;
		}
	}
	private void OnKeyClear(object sender, EventArgs e)
	{
		try
		{
			entryGUID.Text = "";
		}
		catch (Exception ex)
		{
			DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
	private void OnKeyCut(object sender, EventArgs e)
	{
		try
		{
			cutText = entryGUID.Text;
			entryGUID.Text = "";
		}
		catch (Exception ex)
		{
			DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
	private void OnKeyPaste(object sender, EventArgs e)
	{
		try
		{
			InsertTextAtCursor(cutText);
		}
		catch (Exception ex)
		{
			DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
	private void OnKeyShift(object sender, EventArgs e)
	{
		try
		{
			if (isShift_Pressed == true)
			{

				btnShift.BorderColor = Colors.Black;
				btnShift.BorderWidth = 0;
				isShift_Pressed = false;
				num1.Text = "1";
				num2.Text = "2";
				num3.Text = "3";
				num4.Text = "4";
				num5.Text = "5";
				num6.Text = "6";
				num7.Text = "7";
				num8.Text = "8";
				num9.Text = "9";
				num0.Text = "0";
			}
			else
			{

				btnShift.BorderColor = Colors.Black;
				btnShift.BorderWidth = 2;
				isShift_Pressed = true;
				num1.Text = "!";
				num2.Text = "@";
				num3.Text = "#";
				num4.Text = "$";
				num5.Text = "%";
				num6.Text = "^";
				num7.Text = "&";
				num8.Text = "*";
				num9.Text = "-";
				num0.Text = "/";
			}
		}
		catch (Exception ex)
		{
			DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
	private void InputChange(object sender, EventArgs e)
	{
		try
		{
			DisplayAlert("", "", "Ok");
		}
		catch (Exception ex)
		{
			DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
	private void Login_Enter_Button_Clicked(object sender, EventArgs e)
	{
		try
		{
			ValidateStoreCode(sender, e);
			//Item firstitem = Items.FirstOrDefault();
			//if (firstitem != null)
			//{
			//	btnSearch_Clicked(firstitem.MAINUPC);
			//}
			//else
			//{
			//	RefreshButton_Clicked(sender, e);
			//	DisplayAlert("Alert!!", "Item Not Found", "OK");
			//}
		}
		catch (Exception ex)
		{
			DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
	#endregion
}