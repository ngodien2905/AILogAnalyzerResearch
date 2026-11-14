using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILogAnalyzerResearch.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialResearchSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coding_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_code = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coding_tasks_id", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    course = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    research_group = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants_id", x => x.id);
                    table.CheckConstraint("ck_participant_code_not_blank", "participant_code <> ''");
                });

            migrationBuilder.CreateTable(
                name: "research_outcomes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    scale_maximum = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_research_outcomes_id", x => x.id);
                    table.CheckConstraint("ck_research_outcome_scale", "scale_maximum IS NULL OR scale_maximum > 0");
                    table.ForeignKey(
                        name: "fk_research_outcomes_participants_participant_id",
                        column: x => x.participant_id,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coding_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_assignments_id", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_assignments_coding_tasks_coding_task_id",
                        column: x => x.coding_task_id,
                        principalTable: "coding_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_task_assignments_participants_participant_id",
                        column: x => x.participant_id,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_interactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    plan = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    prompt = table.Column<string>(type: "text", nullable: false),
                    response = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false),
                    validation_note = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_interactions_id", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_interactions_task_assignments_task_assignment_id",
                        column: x => x.task_assignment_id,
                        principalTable: "task_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "source_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_reference = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    content_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_final = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_submissions_id", x => x.id);
                    table.ForeignKey(
                        name: "fk_source_submissions_task_assignments_task_assignment_id",
                        column: x => x.task_assignment_id,
                        principalTable: "task_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_classifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_interaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    review_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewer_note = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_classifications_id", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_classifications_ai_interactions_ai_interaction_id",
                        column: x => x.ai_interaction_id,
                        principalTable: "ai_interactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "coding_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correctness = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    code_quality = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    solution_efficiency = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    testing_error_handling = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ability_to_explain = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coding_assessments_id", x => x.id);
                    table.CheckConstraint("ck_assessment_code_quality", "code_quality >= 0 AND code_quality <= 2");
                    table.CheckConstraint("ck_assessment_correctness", "correctness >= 0 AND correctness <= 4");
                    table.CheckConstraint("ck_assessment_explanation", "ability_to_explain >= 0 AND ability_to_explain <= 2");
                    table.CheckConstraint("ck_assessment_solution_efficiency", "solution_efficiency >= 0 AND solution_efficiency <= 1");
                    table.CheckConstraint("ck_assessment_testing", "testing_error_handling >= 0 AND testing_error_handling <= 1");
                    table.ForeignKey(
                        name: "fk_coding_assessments_source_submissions_source_submission_id",
                        column: x => x.source_submission_id,
                        principalTable: "source_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_classifications_ai_interaction_id",
                table: "ai_classifications",
                column: "ai_interaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_interactions_task_assignment_id",
                table: "ai_interactions",
                column: "task_assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_coding_assessments_source_submission_id",
                table: "coding_assessments",
                column: "source_submission_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_coding_tasks_task_code",
                table: "coding_tasks",
                column: "task_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participants_participant_code",
                table: "participants",
                column: "participant_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_research_outcomes_participant_id_metric_name",
                table: "research_outcomes",
                columns: new[] { "participant_id", "metric_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_source_submissions_task_assignment_id",
                table: "source_submissions",
                column: "task_assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_assignments_coding_task_id",
                table: "task_assignments",
                column: "coding_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_assignments_participant_id_coding_task_id",
                table: "task_assignments",
                columns: new[] { "participant_id", "coding_task_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_classifications");

            migrationBuilder.DropTable(
                name: "coding_assessments");

            migrationBuilder.DropTable(
                name: "research_outcomes");

            migrationBuilder.DropTable(
                name: "ai_interactions");

            migrationBuilder.DropTable(
                name: "source_submissions");

            migrationBuilder.DropTable(
                name: "task_assignments");

            migrationBuilder.DropTable(
                name: "coding_tasks");

            migrationBuilder.DropTable(
                name: "participants");
        }
    }
}
