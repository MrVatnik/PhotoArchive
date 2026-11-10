using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoArchive.Migrations
{
    /// <inheritdoc />
    public partial class RestrictReferenceDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cameras_Formats_FormatId",
                table: "Cameras");

            migrationBuilder.DropForeignKey(
                name: "FK_Films_Cameras_CameraId",
                table: "Films");

            migrationBuilder.DropForeignKey(
                name: "FK_Films_Recipes_RecipeId",
                table: "Films");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Developers_DeveloperId",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_FilmTypes_FilmTypeId",
                table: "Recipes");

            migrationBuilder.AddForeignKey(
                name: "FK_Cameras_Formats_FormatId",
                table: "Cameras",
                column: "FormatId",
                principalTable: "Formats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Films_Cameras_CameraId",
                table: "Films",
                column: "CameraId",
                principalTable: "Cameras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Films_Recipes_RecipeId",
                table: "Films",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Developers_DeveloperId",
                table: "Recipes",
                column: "DeveloperId",
                principalTable: "Developers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_FilmTypes_FilmTypeId",
                table: "Recipes",
                column: "FilmTypeId",
                principalTable: "FilmTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cameras_Formats_FormatId",
                table: "Cameras");

            migrationBuilder.DropForeignKey(
                name: "FK_Films_Cameras_CameraId",
                table: "Films");

            migrationBuilder.DropForeignKey(
                name: "FK_Films_Recipes_RecipeId",
                table: "Films");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Developers_DeveloperId",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_FilmTypes_FilmTypeId",
                table: "Recipes");

            migrationBuilder.AddForeignKey(
                name: "FK_Cameras_Formats_FormatId",
                table: "Cameras",
                column: "FormatId",
                principalTable: "Formats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Films_Cameras_CameraId",
                table: "Films",
                column: "CameraId",
                principalTable: "Cameras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Films_Recipes_RecipeId",
                table: "Films",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Developers_DeveloperId",
                table: "Recipes",
                column: "DeveloperId",
                principalTable: "Developers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_FilmTypes_FilmTypeId",
                table: "Recipes",
                column: "FilmTypeId",
                principalTable: "FilmTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
