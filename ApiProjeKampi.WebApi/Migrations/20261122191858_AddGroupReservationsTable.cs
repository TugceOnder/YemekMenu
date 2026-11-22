using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiProjeKampi.WebApi.Migrations
{
    public partial class AddGroupReservationsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                    name: "GroupReservations",
                    columns: table => new
                    {
                        GroupReservationId = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),

                        GroupTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        ResponsibleCustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        PersonCount = table.Column<int>(type: "int", nullable: true),
                        ReservationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                        Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        ReservationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        LastProcessDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_GroupReservations", x => x.GroupReservationId);
                    });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
      name: "GroupReservations");
        }
    }
}
