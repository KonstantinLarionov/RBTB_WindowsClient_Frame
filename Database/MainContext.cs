using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RBTB_WindowsClient_Frame.Domains.Entities;

namespace RBTB_WindowsClient_Frame.Database
{
	public class MainContext : DbContext
	{
		public MainContext() : base( "Strato" ) { }

		public DbSet<Option> Options { get; set; }
	}
}
