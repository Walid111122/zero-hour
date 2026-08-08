using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZeroHour.Server.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    auth_provider = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    age_declared = table.Column<short>(type: "smallint", nullable: true),
                    created_at = table.Column<long>(type: "bigint", nullable: false),
                    last_login_at = table.Column<long>(type: "bigint", nullable: true),
                    banned_until = table.Column<long>(type: "bigint", nullable: true),
                    ban_reason = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_states",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_id = table.Column<int>(type: "integer", nullable: false),
                    hq_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    power = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    tile_x = table.Column<int>(type: "integer", nullable: true),
                    tile_y = table.Column<int>(type: "integer", nullable: true),
                    alliance_id = table.Column<long>(type: "bigint", nullable: true),
                    vip_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    vip_points = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    shield_until = table.Column<long>(type: "bigint", nullable: true),
                    stamina = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    last_resolved_at = table.Column<long>(type: "bigint", nullable: false),
                    resources = table.Column<string>(type: "jsonb", nullable: false),
                    config_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_states", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_states_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ps_alliance",
                table: "player_states",
                column: "alliance_id");

            migrationBuilder.CreateIndex(
                name: "ix_ps_power",
                table: "player_states",
                columns: new[] { "state_id", "power" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ps_tile",
                table: "player_states",
                columns: new[] { "state_id", "tile_x", "tile_y" });

            migrationBuilder.CreateIndex(
                name: "ux_ps_player_state",
                table: "player_states",
                columns: new[] { "player_id", "state_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_players_device",
                table: "players",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ux_players_email",
                table: "players",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_states");

            migrationBuilder.DropTable(
                name: "players");
        }
    }
}
