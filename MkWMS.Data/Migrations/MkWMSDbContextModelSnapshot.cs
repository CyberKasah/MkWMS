using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MkWMS.Data.Context;

#nullable disable

namespace MkWMS.Data.Migrations
{
    [DbContext(typeof(MkWMSDbContext))]
    partial class MkWMSDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.7");
#pragma warning restore 612, 618
        }
    }
}
