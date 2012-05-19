using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WMA
{
    //
    // Currently trying to figure out groups in XAML
    // http://stackoverflow.com/questions/639809/how-do-i-group-items-in-a-wpf-listview
    //

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IBlizzardProgress
    {
        private Database _database;
        private bool _ignoreEvent;

        private void UpdateAuctionsListView()
        {
            auctionsListView.ItemsSource = from auction in _database.Auctions select auction;
        }

        public MainWindow()
        {
            InitializeComponent();

            string dbName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\wma.mdf";
            _database = Database.Open(dbName);

            BlizzardAPI.UpdateRealms(_database);

            realmComboBox.ItemsSource = from realm in _database.Realms select realm;

            if (_database.Configurations.First().SelectedRealm != null)
            {
                _ignoreEvent = true;
                realmComboBox.SelectedValue = _database.Configurations.First().SelectedRealm;
                _ignoreEvent = false;
                UpdateAuctionsListView();
            }
        }

        private void realmComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreEvent)
                return;

            realmComboBox.IsEnabled = false;
            statusProgress.Visibility = System.Windows.Visibility.Visible;
            statusProgress.Value = 0;

            string selectedRealm = (string)realmComboBox.SelectedValue;

            var task = Task.Factory.StartNew(() =>
                {
                    BlizzardAPI.UpdateAuctions(_database, selectedRealm, this);

                    this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            realmComboBox.IsEnabled = true;
                            statusProgress.Visibility = System.Windows.Visibility.Hidden;
                            UpdateAuctionsListView();
                        }));
                });
        }

        public void UpdateProgress(int value, int max)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                statusProgress.Value = value;
                statusProgress.Maximum = max;
                statusProgress.ToolTip = value + " / " + max;
            }));
        }

        private GridViewColumnHeader _CurSortCol = null;
        private SortAdorner _CurAdorner = null;

        private void SortClick(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = sender as GridViewColumnHeader;
            String field = column.Tag as String;

            if (_CurSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(_CurSortCol).Remove(_CurAdorner);
                auctionsListView.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (_CurSortCol == column && _CurAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            _CurSortCol = column;
            _CurAdorner = new SortAdorner(_CurSortCol, newDir);
            AdornerLayer.GetAdornerLayer(_CurSortCol).Add(_CurAdorner);
            auctionsListView.Items.SortDescriptions.Add(
                new SortDescription(field, newDir));
        }

    }
}
