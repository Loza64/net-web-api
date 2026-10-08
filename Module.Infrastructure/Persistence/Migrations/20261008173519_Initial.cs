using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PERMISSIONS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    TITLE = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERMISSIONS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ROLES",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    ACTIVE = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DELETED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ROLE_PERMISSIONS",
                columns: table => new
                {
                    ROLE_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    PERMISSION_ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLE_PERMISSIONS", x => new { x.ROLE_ID, x.PERMISSION_ID });
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSIONS_PERMISSIONS_PERMISSION_ID",
                        column: x => x.PERMISSION_ID,
                        principalTable: "PERMISSIONS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSIONS_ROLES_ROLE_ID",
                        column: x => x.ROLE_ID,
                        principalTable: "ROLES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    USERNAME = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    PASSWORD = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    SURNAME = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    PHOTO = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    BLOCKED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ROLE_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DELETED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_USERS_ROLES_ROLE_ID",
                        column: x => x.ROLE_ID,
                        principalTable: "ROLES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "REFRESH_TOKEN",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TOKEN = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    FAMILY_ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    USED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    REVOKED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    USER_ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REFRESH_TOKEN", x => x.ID);
                    table.ForeignKey(
                        name: "FK_REFRESH_TOKEN_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PERMISSIONS_NAME",
                table: "PERMISSIONS",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_REFRESH_TOKEN_FAMILY_ID",
                table: "REFRESH_TOKEN",
                column: "FAMILY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_REFRESH_TOKEN_TOKEN",
                table: "REFRESH_TOKEN",
                column: "TOKEN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_REFRESH_TOKEN_USER_ID",
                table: "REFRESH_TOKEN",
                column: "USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSIONS_PERMISSION_ID",
                table: "ROLE_PERMISSIONS",
                column: "PERMISSION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_ROLES_NAME_DELETED_AT",
                table: "ROLES",
                columns: new[] { "NAME", "DELETED_AT" },
                unique: true,
                filter: "\"DELETED_AT\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_EMAIL_DELETED_AT",
                table: "USERS",
                columns: new[] { "EMAIL", "DELETED_AT" },
                unique: true,
                filter: "\"DELETED_AT\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_ROLE_ID",
                table: "USERS",
                column: "ROLE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_USERNAME_DELETED_AT",
                table: "USERS",
                columns: new[] { "USERNAME", "DELETED_AT" },
                unique: true,
                filter: "\"DELETED_AT\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "REFRESH_TOKEN");

            migrationBuilder.DropTable(
                name: "ROLE_PERMISSIONS");

            migrationBuilder.DropTable(
                name: "USERS");

            migrationBuilder.DropTable(
                name: "PERMISSIONS");

            migrationBuilder.DropTable(
                name: "ROLES");
        }
    }
}
