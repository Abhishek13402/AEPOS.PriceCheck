



using AEPOS.PriceCheck.SQLiteClasses;

namespace AEPOS.PriceCheck
{
    public partial class App : Application
    {
        public static CacheDataRepo cacheDataRepo { get; private set; }
        public App(CacheDataRepo repo)
        {
            InitializeComponent();
            cacheDataRepo = repo;
            MainPage = new NavigationPage(new Pages.LoginPage());
            //MainPage = new NavigationPage(new Pages.test());
            Current.UserAppTheme = AppTheme.Light;
        }
    }
}
