using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RBTB_WindowsClient_Frame.Domains.Entities;

namespace RBTB_WindowsClient_Frame.Helpers
{
	public static class OptionsHelper
	{
		public static object GetByType( this Option option )
		{
			switch ( option.ValueType )
			{
				case OptionType.None:
					return null;
				case OptionType.Int:
					return option.ValueInt;
				case OptionType.String:
					return option.ValueString;
				case OptionType.Double:
					return option.ValueDouble;
				case OptionType.DateTime:
					return option.ValueDateTime;
				default: return null;
			}
		}
	}
}
