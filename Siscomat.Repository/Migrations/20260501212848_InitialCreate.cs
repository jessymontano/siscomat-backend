using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Siscomat.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gestores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    apellido1 = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    apellido2 = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    correo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    es_admin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gestores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participantes",
                columns: table => new
                {
                    folio = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    apellido1 = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    apellido2 = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participantes", x => x.folio);
                });

            migrationBuilder.CreateTable(
                name: "plantillas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plantillas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cursos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    participante_folio = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cursos", x => x.id);
                    table.ForeignKey(
                        name: "fk_cursos_participantes_participante_folio",
                        column: x => x.participante_folio,
                        principalTable: "participantes",
                        principalColumn: "folio");
                });

            migrationBuilder.CreateTable(
                name: "constancias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    folio_participante = table.Column<string>(type: "text", nullable: false),
                    curso_id = table.Column<int>(type: "integer", nullable: false),
                    plantilla_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_constancias", x => x.id);
                    table.ForeignKey(
                        name: "fk_constancias_cursos_curso_id",
                        column: x => x.curso_id,
                        principalTable: "cursos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_constancias_participantes_folio_participante",
                        column: x => x.folio_participante,
                        principalTable: "participantes",
                        principalColumn: "folio",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_constancias_plantillas_plantilla_id",
                        column: x => x.plantilla_id,
                        principalTable: "plantillas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_constancias_curso_id",
                table: "constancias",
                column: "curso_id");

            migrationBuilder.CreateIndex(
                name: "ix_constancias_folio_participante",
                table: "constancias",
                column: "folio_participante");

            migrationBuilder.CreateIndex(
                name: "ix_constancias_plantilla_id",
                table: "constancias",
                column: "plantilla_id");

            migrationBuilder.CreateIndex(
                name: "ix_cursos_participante_folio",
                table: "cursos",
                column: "participante_folio");

            migrationBuilder.CreateIndex(
                name: "ix_gestores_correo",
                table: "gestores",
                column: "correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "constancias");

            migrationBuilder.DropTable(
                name: "gestores");

            migrationBuilder.DropTable(
                name: "cursos");

            migrationBuilder.DropTable(
                name: "plantillas");

            migrationBuilder.DropTable(
                name: "participantes");
        }
    }
}
