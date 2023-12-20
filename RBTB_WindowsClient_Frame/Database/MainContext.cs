using System.Data.Entity;

using RBTB_WindowsClient_Frame.Domains.Entities;

namespace RBTB_WindowsClient_Frame.Database
{
    public class MainContext : DbContext
    {
        public MainContext() : base("Strato") { }

        public DbSet<Option> Options { get; set; }
    }
}
