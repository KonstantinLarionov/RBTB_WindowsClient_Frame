using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RBTB_WindowsClient_Frame.Domains
{
	public class OptionsModel : INotifyPropertyChanged
	{
		private string _log = string.Empty;
		public string Log
		{
			get { return _log; }
			set
			{
				if ( value != _log )
				{
					_log = value;
					OnPropertyChanged( "Log" );
				}
			}
		}

		private string _ApiKey;
		public string ApiKey
		{
			get { return _ApiKey; }
			set
			{
				if ( value != _ApiKey )
				{
					_ApiKey = value;
					OnPropertyChanged( "ApiKey" );
				}
			}
		}
		private string _SecretKey;
		public string SecretKey
		{
			get { return _SecretKey; }
			set
			{
				if ( value != _SecretKey )
				{
					_SecretKey = value;
					OnPropertyChanged( "SecretKey" );
				}
			}
		}
		public string _TelegramId;
		public string TelegramId
		{
			get { return _TelegramId; }
			set
			{
				if ( value != _TelegramId )
				{
					_TelegramId = value;
					OnPropertyChanged( "TelegramId" );
				}
			}
		}
		private string _VolumeIn;
		public string VolumeIn
		{
			get { return _VolumeIn; }
			set
			{
				if ( value != _VolumeIn )
				{
					_VolumeIn = value;
					OnPropertyChanged( "VolumeIn" );
				}
			}
		}
		private string _PipsOut;
		public string PipsOut
		{
			get { return _PipsOut; }
			set
			{
				if ( value != _PipsOut )
				{
					_PipsOut = value;
					OnPropertyChanged( "PipsOut" );
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged( string propertyName )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
		}

	}
}
