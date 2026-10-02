using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class SwapLaunchPairGenders : Migration
    {
        // The production server, and the two owners whose first Plynlings were chosen at launch.
        private const long ProdGuildId = 878305033995825164;
        private const long CoprinOwner = 345917214966415362;
        private const long GirolleOwner = 324768221372743681;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only, scoped to the production server and to those two living rows. Gender is
            // 0 = Male, 1 = Female; species 12 = Coprin, 5 = Dore (shown as Girolle). The current
            // gender and species are in the WHERE, so a row that is not the launch Plynling any
            // more (abandoned, another species, already swapped) is left alone.
            // Nothing else stores a gender: compatibility comes from the ids, the art is not
            // gendered, and journal moments are worded at display, so the lines follow by themselves.
            migrationBuilder.Sql(
                $"UPDATE Plynlings SET Gender = 1 WHERE GuildId = {ProdGuildId} AND OwnerId = {CoprinOwner} " +
                "AND DiedAt IS NULL AND Species = 12 AND Gender = 0;");
            migrationBuilder.Sql(
                $"UPDATE Plynlings SET Gender = 0 WHERE GuildId = {ProdGuildId} AND OwnerId = {GirolleOwner} " +
                "AND DiedAt IS NULL AND Species = 5 AND Gender = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: a swapped gender cannot be told apart from one chosen afterwards.
        }
    }
}
