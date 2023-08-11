using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RBTB_WindowsClient_Frame.Domains
{
	public class WalletPoint
	{
		public WalletPoint( DateTime? dateTime, double value )
		{
			DateTimeD = dateTime;
			Value = value;
		}

		public DateTime? DateTimeD { get; set; }
		public string DateTime => DateTimeD?.ToString("f");
		public double Value { get; set; }
	}
}
