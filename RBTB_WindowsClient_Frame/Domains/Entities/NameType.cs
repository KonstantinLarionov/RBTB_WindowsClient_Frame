using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RBTB_WindowsClient_Frame.Domains.Entities
{
	public enum NameType
	{
		[EnumMember( Value = "" )]
		None,
		[EnumMember(Value = "URL_ServiceStrategy" )]
		URL_ServiceStrategy,
		[EnumMember( Value = "URL_ServiceAccount" )]
		URL_ServiceAccount,
		[EnumMember( Value = "URL_Binance" )]
		URL_Binance,
        [EnumMember(Value = "URL_Bybit")]
        URL_Bybit,

        [EnumMember( Value = "ApiKey" )]
		ApiKey,
		[EnumMember( Value = "SecretKey" )]
		SecretKey,
		[EnumMember( Value = "TelegramId" )]
		TelegramId,
		[EnumMember( Value = "Symbol" )]
		Symbol,
		[EnumMember( Value = "PipsOut" )]
		PipsOut,
		[EnumMember( Value = "VolumeIn" )]
		VolumeIn
	}
}
