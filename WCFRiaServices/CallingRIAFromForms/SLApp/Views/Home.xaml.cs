namespace SLApp
{
    using System.Windows.Controls;
    using System.Windows.Navigation;
    using SLApp.Web;
    using SLApp.Web.Models;
    using System;

    /// <summary>
    /// Home page for the application.
    /// </summary>
    public partial class Home : Page
    {
        /// <summary>
        /// Creates a new <see cref="Home"/> instance.
        /// </summary>
        public Home()
        {
            InitializeComponent();

            this.Title = ApplicationStrings.HomePageTitle;
        }

        /// <summary>
        /// Executes when the user navigates to this page.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void contactDomainDataSource_LoadedData(object sender, System.Windows.Controls.LoadedDataEventArgs e)
        {

            if (e.HasError)
            {
                System.Windows.MessageBox.Show(e.Error.ToString(), "Load Error", System.Windows.MessageBoxButton.OK);
                e.MarkErrorAsHandled();
            }
        }

        private void button1_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            new ContactsDomainContext();
            this.contactDomainDataSource.SubmitChanges();
        }
    }
}