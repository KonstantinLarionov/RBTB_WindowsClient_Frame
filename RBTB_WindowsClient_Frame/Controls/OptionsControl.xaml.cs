using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RBTB_WindowsClient_Frame.Database;
using RBTB_WindowsClient_Frame.Domains;
using RBTB_WindowsClient_Frame.Domains.Entities;
using RBTB_WindowsClient_Frame.Helpers;

namespace RBTB_WindowsClient_Frame.Controls;
/// <summary>
/// Логика взаимодействия для OptionsControl.xaml
/// </summary>
public partial class OptionsControl : UserControl
{
	private readonly MainContext _mainContext;
	public OptionsModel Model;
	public OptionsControl(MainContext mainContext)
	{
		InitializeComponent();
		Model = new OptionsModel();
		this.DataContext = Model;

		this._mainContext = mainContext;
		LoadingOptions(this);
	}

	private async void Button_Click( object sender, RoutedEventArgs e ) => await SavingOptions(this);

	
	private void LoadingOptions( DependencyObject obj )
	{
		var options = _mainContext.Options.ToList();

		var option = options.FirstOrDefault( x => x.NameType == Domains.Entities.NameType.ApiKey );
		Model.ApiKey = option?.GetByType().ToString();

		var option1 = options.FirstOrDefault( x => x.NameType == Domains.Entities.NameType.SecretKey );
		Model.SecretKey = option1?.GetByType().ToString();

		var option2 = options.FirstOrDefault( x => x.NameType == Domains.Entities.NameType.VolumeIn );
		Model.VolumeIn = option2?.GetByType().ToString();

		var option3 = options.FirstOrDefault( x => x.NameType == Domains.Entities.NameType.PipsOut );
		Model.PipsOut = option3?.GetByType().ToString();

		var option4 = options.FirstOrDefault( x => x.NameType == Domains.Entities.NameType.TelegramId );
		Model.TelegramId = option4?.GetByType().ToString();
	}
	public async Task SavingOptions( DependencyObject obj )
	{
		saver.Foreground = new SolidColorBrush( Colors.White );
		
		foreach ( var item in LogicalTreeHelper.GetChildren( obj ) )
		{
			if ( item is DependencyObject )
			{
				DependencyObject child = (DependencyObject)item;
				if ( child is Panel ) await SavingOptions( child );

				if ( child is TextBox )
				{
					var textBox = (TextBox)child;
					if ( ((string)textBox.Tag).Contains("option") )
					{
						var nameType = textBox.Name.ToEnum<Domains.Entities.NameType>();
						var db_item = _mainContext.Options
							.FirstOrDefault( x => x.NameType == nameType );
						
						if ( db_item == null )
						{
							db_item = new Option() { NameType = nameType, Name = textBox.Name };
							
							_mainContext.Options.Add( db_item );
							await _mainContext.SaveChangesAsync();
						}

						if ( ( (string)textBox.Tag ).Contains( "double" ) )
						{ db_item.ValueDouble = Convert.ToDouble( textBox.Text ); db_item.ValueType = OptionType.Double; }
						if ( ( (string)textBox.Tag ).Contains( "string" ) )
						{ db_item.ValueString =  textBox.Text ; db_item.ValueType = OptionType.String; }
						if ( ( (string)textBox.Tag ).Contains( "int" ) )
						{ db_item.ValueInt = Convert.ToInt32( textBox.Text ); db_item.ValueType = OptionType.Int; }
						if ( ( (string)textBox.Tag ).Contains( "date" ) )
						{ db_item.ValueDateTime = Convert.ToDateTime( textBox.Text ); db_item.ValueType = OptionType.DateTime; }

						await _mainContext.SaveChangesAsync();
					}
				}

				if ( child is PasswordBox )
				{
					var textBox = (PasswordBox)child;
					if ( ( (string)textBox.Tag ).Contains( "option" ) )
					{
						var nameType = textBox.Name.ToEnum<NameType>();
						var db_item = _mainContext.Options
							.FirstOrDefault( x => x.NameType == nameType );

						if ( db_item == null )
						{
							db_item = new Option() { NameType = nameType, Name = textBox.Name };

							_mainContext.Options.Add( db_item );
							await _mainContext.SaveChangesAsync();
						}

						if ( ( (string)textBox.Tag ).Contains( "double" ) )
						{ db_item.ValueDouble = Convert.ToDouble( textBox.Password ); db_item.ValueType = OptionType.Double; }
						if ( ( (string)textBox.Tag ).Contains( "string" ) )
						{ db_item.ValueString = textBox.Password; db_item.ValueType = OptionType.String; }
						if ( ( (string)textBox.Tag ).Contains( "int" ) )
						{ db_item.ValueInt = Convert.ToInt32( textBox.Password ); db_item.ValueType = OptionType.Int; }
						if ( ( (string)textBox.Tag ).Contains( "date" ) )
						{ db_item.ValueDateTime = Convert.ToDateTime( textBox.Password ); db_item.ValueType = OptionType.DateTime; }

						await _mainContext.SaveChangesAsync();
					}
				}
			}
		}

		saver.Foreground = new SolidColorBrush( Colors.Green );
	}

	private void Options_TextChanged( object sender, TextChangedEventArgs e )
	{
		saver.Foreground = new SolidColorBrush( Colors.White );
	}
	private void Options_TextChanged( object sender, RoutedEventArgs e )
	{
		saver.Foreground = new SolidColorBrush( Colors.White );
	}
}
