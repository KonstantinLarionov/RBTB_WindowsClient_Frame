using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using RBTB_WindowsClient_Frame.Domains;
using RBTB_WindowsClient_Frame.Integrations.MyNamespace;

namespace RBTB_WindowsClient_Frame.Controls
{
	/// <summary>
	/// Логика взаимодействия для WalletControl.xaml
	/// </summary>
	public partial class WalletControl : UserControl
	{
		private readonly AccountClient _accountClient;
		private readonly Guid userId;

		public WalletControl(Guid userId, AccountClient client)
		{
			InitializeComponent();
			this.userId = userId;

			dateStart.SelectedDate = DateTime.Now.AddDays(-1);
			dateEnd.SelectedDate = DateTime.Now;
			_accountClient = client;
		}

		private async void Button_Click( object sender, RoutedEventArgs e )
		{
			DateTime? dateTo = dateEnd.SelectedDate.HasValue ? dateEnd.SelectedDate.Value.AddDays(1) : null;
			try
			{
                var result = await _accountClient.WalletsAsync(userId, currency.Text, dateStart.SelectedDate, dateTo, "Bybit");
                if (result != null && result.Data != null && result.Data.Count != 0)
                {
                    var points = result.Data
                        .Select(x => new WalletPoint(x.DateOfRecording, x.Balance))
                        .OrderBy(x => x.DateTimeD)
                        .ToList();

                    dg_wallet.ItemsSource = points;
                    BuildChartBalance(points);
                    var percent_points = new List<WalletPoint>() { new WalletPoint(points[0].DateTimeD, 0) };
                    percent_points.Add(new WalletPoint(DateTime.Now, ((points[points.Count - 1].Value / points[0].Value) - 1) * 100));
                    BuildChartPercent(percent_points);
                }
                else { MessageBox.Show("Нет данных за выбранный период"); }
            }
            catch (Exception ex)
			{

				MessageBox.Show($"Ошибка получения данных: {ex.Message}");
			}
			
		}

		private void BuildChartBalance( List<WalletPoint> points )
		{
			var sc = new SeriesCollection();
			var ls = new LineSeries();

			ls.LabelPoint = new Func<ChartPoint, string>( ( x ) => points[x.Key].DateTime + "\r\n " + points[x.Key].Value );
			var cv = new ChartValues<double>() { };
			cv.AddRange( points.Select( x => Convert.ToDouble( x.Value ) ).ToArray() );
			ls.Values = cv;
			ls.Title = "Баланс";
			sc.Add( ls );

			lc_wallet.Series = sc;
		}
		private void BuildChartPercent( List<WalletPoint> points )
		{
			var sc = new SeriesCollection();
			var ls = new LineSeries();

			ls.LabelPoint = new Func<ChartPoint, string>( ( x ) => points[x.Key].DateTime + "\r\n " + points[x.Key].Value );
			var cv = new ChartValues<double>() { };
			cv.AddRange( points.Select( x => Convert.ToDouble( x.Value ) ).ToArray() );
			ls.Values = cv;
			ls.Title = "Процент";
			sc.Add( ls );

			lc_percent.Series = sc;
		}
	}
}
