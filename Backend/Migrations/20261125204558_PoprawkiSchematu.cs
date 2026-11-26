using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class PoprawkiSchematu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionOptions");

            migrationBuilder.DropTable(
                name: "ControlQuestions");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses");

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_UserId_CourseId",
                table: "UserProgresses",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CourseId",
                table: "Feedbacks",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_LessonBlockId",
                table: "Feedbacks",
                column: "LessonBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId",
                table: "CourseEnrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_UserId_CourseId",
                table: "CourseEnrollments",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_Courses_CourseId",
                table: "CourseEnrollments",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_Users_UserId",
                table: "CourseEnrollments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Courses_CourseId",
                table: "Feedbacks",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_LessonBlocks_LessonBlockId",
                table: "Feedbacks",
                column: "LessonBlockId",
                principalTable: "LessonBlocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_Courses_CourseId",
                table: "CourseEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_Users_UserId",
                table: "CourseEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Courses_CourseId",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_LessonBlocks_LessonBlockId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_UserId_CourseId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_CourseId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_LessonBlockId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_CourseId",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_UserId_CourseId",
                table: "CourseEnrollments");

            migrationBuilder.CreateTable(
                name: "ControlQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlQuestions_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ControlQuestionId = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOptions_ControlQuestions_ControlQuestionId",
                        column: x => x.ControlQuestionId,
                        principalTable: "ControlQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlQuestions_LessonId",
                table: "ControlQuestions",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_ControlQuestionId",
                table: "QuestionOptions",
                column: "ControlQuestionId");
        }
    }
}
