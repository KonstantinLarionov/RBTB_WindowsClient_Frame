using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Org.BouncyCastle.Crypto.Tls;

namespace RBTB_WindowsClient_Frame.Domains.Entities
{
	public class Option
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; }
		public NameType NameType { get; set; }
		public OptionType ValueType { get; set; }
		public string ValueString { get; set; }
		public double ValueDouble { get; set; }
		public int ValueInt { get; set; }
		public DateTime? ValueDateTime { get; set; }
	}
}
