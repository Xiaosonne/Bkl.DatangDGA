using Bkl.Models.LocalContext;
using Bkl.Models.Std;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    public partial class BklLocalDbContext : DbContext
    {
        public BklLocalDbContext()
        {
        }

        public BklLocalDbContext(DbContextOptions<BklLocalDbContext> options)
            : base(options)
        {
        }
        public virtual DbSet<BklLocalYoloDataSet> BklLocalYoloDataSet { get; set; }
        public virtual DbSet<BklLocalYoloPath> BklLocalYoloPath { get; set; }
    }
}
