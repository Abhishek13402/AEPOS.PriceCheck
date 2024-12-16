using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using System.Threading.Tasks;

namespace AEPOS.PriceCheck
{
	[Activity(Theme = "@style/Maui.SplashTheme",
		MainLauncher = true,
		LaunchMode = LaunchMode.SingleTop,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
	public class MainActivity : MauiAppCompatActivity
	{
		public override void OnBackPressed()
		{
			// Do nothing or implement custom behavior here
			// base.OnBackPressed(); // Uncomment this line if you want the default behavior
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Set the status bar color
			Window.SetStatusBarColor(Android.Graphics.Color.Transparent); // Example color

																					// Enable full-screen mode
			if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
			{
				Window?.SetDecorFitsSystemWindows(false);
				Window?.InsetsController?.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
			}
			else
			{
#pragma warning disable CS0618
				Window.DecorView.SystemUiVisibility =
					(StatusBarVisibility)(SystemUiFlags.Fullscreen |
										  SystemUiFlags.HideNavigation |
										  SystemUiFlags.ImmersiveSticky);
#pragma warning restore CS0618
			}

			// Start hiding the system bars automatically
			HideSystemBars();
		}

		// Method to hide system bars automatically after a timeout of no interaction
		private async void HideSystemBars()
		{
			while (true)
			{
				await Task.Delay(3000); // Delay for 3 seconds (change the delay as needed)

				// Hide system bars after the timeout
				if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
				{
					Window?.InsetsController?.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
				}
				else
				{
#pragma warning disable CS0618
					Window.DecorView.SystemUiVisibility =
						(StatusBarVisibility)(SystemUiFlags.Fullscreen |
											  SystemUiFlags.HideNavigation |
											  SystemUiFlags.ImmersiveSticky);
#pragma warning restore CS0618
				}
			}
		}
	}
}
