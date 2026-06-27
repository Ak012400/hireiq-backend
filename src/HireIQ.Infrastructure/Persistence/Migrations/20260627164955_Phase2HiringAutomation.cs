using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class Phase2HiringAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    recipient_name = table.Column<string>(type: "text", nullable: true),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    related_entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    subject_template = table.Column<string>(type: "text", nullable: false),
                    body_template = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.Id);
                });

            // [SKIPPED] interview_rooms — already exists in DB from earlier (pre-Clean-Architecture) deploy.
            // [SKIPPED] job_applications — same reason.

            migrationBuilder.CreateTable(
                name: "job_postings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    hirer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    employment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    work_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    experience_min_years = table.Column<int>(type: "integer", nullable: true),
                    experience_max_years = table.Column<int>(type: "integer", nullable: true),
                    salary_min = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    salary_max = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    salary_period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    requirements_json = table.Column<string>(type: "jsonb", nullable: false),
                    benefits_json = table.Column<string>(type: "jsonb", nullable: false),
                    skills_required_json = table.Column<string>(type: "jsonb", nullable: false),
                    skills_nice_to_have_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closes_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    linkedin_post_url = table.Column<string>(type: "text", nullable: true),
                    indeed_external_id = table.Column<string>(type: "text", nullable: true),
                    naukri_external_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_postings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_postings_users_hirer_id",
                        column: x => x.hirer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // [SKIPPED] mock_interview_sessions — already exists in DB.

            migrationBuilder.CreateTable(
                name: "candidate_journeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applicant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    stage_history_json = table.Column<string>(type: "jsonb", nullable: false),
                    last_transition_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_journeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidate_journeys_job_applications_job_application_id",
                        column: x => x.job_application_id,
                        principalTable: "job_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidate_journeys_job_postings_job_posting_id",
                        column: x => x.job_posting_id,
                        principalTable: "job_postings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidate_journeys_users_applicant_user_id",
                        column: x => x.applicant_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_board_syncs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    external_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_board_syncs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_board_syncs_job_postings_job_posting_id",
                        column: x => x.job_posting_id,
                        principalTable: "job_postings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hiring_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    offered_salary = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    offered_currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    joining_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    offer_details_json = table.Column<string>(type: "jsonb", nullable: true),
                    offer_letter_url = table.Column<string>(type: "text", nullable: true),
                    candidate_response = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    candidate_responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hiring_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hiring_decisions_candidate_journeys_candidate_journey_id",
                        column: x => x.candidate_journey_id,
                        principalTable: "candidate_journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hiring_decisions_users_decided_by",
                        column: x => x.decided_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hr_interviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hirer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    meet_link = table.Column<string>(type: "text", nullable: true),
                    hirer_notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr_interviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hr_interviews_candidate_journeys_candidate_journey_id",
                        column: x => x.candidate_journey_id,
                        principalTable: "candidate_journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hr_interviews_users_hirer_id",
                        column: x => x.hirer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "interview_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    video_recording_url = table.Column<string>(type: "text", nullable: true),
                    audio_recording_url = table.Column<string>(type: "text", nullable: true),
                    transcript_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interview_sessions_candidate_journeys_candidate_journey_id",
                        column: x => x.candidate_journey_id,
                        principalTable: "candidate_journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_interview_sessions_interview_rooms_interview_room_id",
                        column: x => x.interview_room_id,
                        principalTable: "interview_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    turn_index = table.Column<int>(type: "integer", nullable: false),
                    related_question_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observation_json = table.Column<string>(type: "jsonb", nullable: false),
                    score_technical = table.Column<float>(type: "real", nullable: true),
                    score_communication = table.Column<float>(type: "real", nullable: true),
                    score_confidence = table.Column<float>(type: "real", nullable: true),
                    score_attention = table.Column<float>(type: "real", nullable: true),
                    score_emotion = table.Column<float>(type: "real", nullable: true),
                    raw_response = table.Column<string>(type: "text", nullable: true),
                    latency_ms = table.Column<int>(type: "integer", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_observations_interview_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "interview_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_final_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technical_score = table.Column<float>(type: "real", nullable: false),
                    behavioral_score = table.Column<float>(type: "real", nullable: false),
                    communication_score = table.Column<float>(type: "real", nullable: false),
                    attention_score = table.Column<float>(type: "real", nullable: false),
                    confidence_score = table.Column<float>(type: "real", nullable: false),
                    overall_score = table.Column<float>(type: "real", nullable: false),
                    recommendation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    aggregated_reasoning = table.Column<string>(type: "text", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_final_scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interview_final_scores_interview_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "interview_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_order = table.Column<int>(type: "integer", nullable: false),
                    question_text = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interview_questions_interview_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "interview_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_transcripts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment_index = table.Column<int>(type: "integer", nullable: false),
                    speaker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    start_ms = table.Column<long>(type: "bigint", nullable: false),
                    end_ms = table.Column<long>(type: "bigint", nullable: false),
                    confidence = table.Column<float>(type: "real", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_transcripts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interview_transcripts_interview_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "interview_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transcript_segment_ids_json = table.Column<string>(type: "jsonb", nullable: false),
                    answer_text = table.Column<string>(type: "text", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interview_answers_interview_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "interview_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_interview_answers_interview_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "interview_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_observations_session_id_agent_turn_index",
                table: "ai_observations",
                columns: new[] { "session_id", "agent", "turn_index" });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_journeys_applicant_user_id",
                table: "candidate_journeys",
                column: "applicant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_journeys_job_application_id",
                table: "candidate_journeys",
                column: "job_application_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidate_journeys_job_posting_id_current_stage",
                table: "candidate_journeys",
                columns: new[] { "job_posting_id", "current_stage" });

            migrationBuilder.CreateIndex(
                name: "IX_email_notifications_status",
                table: "email_notifications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_category",
                table: "email_templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_name",
                table: "email_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hiring_decisions_candidate_journey_id",
                table: "hiring_decisions",
                column: "candidate_journey_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hiring_decisions_decided_by",
                table: "hiring_decisions",
                column: "decided_by");

            migrationBuilder.CreateIndex(
                name: "IX_hr_interviews_candidate_journey_id",
                table: "hr_interviews",
                column: "candidate_journey_id");

            migrationBuilder.CreateIndex(
                name: "IX_hr_interviews_hirer_id",
                table: "hr_interviews",
                column: "hirer_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_answers_question_id",
                table: "interview_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_answers_session_id",
                table: "interview_answers",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_final_scores_session_id",
                table: "interview_final_scores",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interview_questions_session_id_question_order",
                table: "interview_questions",
                columns: new[] { "session_id", "question_order" });

            // [SKIPPED] IX_interview_rooms_* — already exist.

            migrationBuilder.CreateIndex(
                name: "IX_interview_sessions_candidate_journey_id",
                table: "interview_sessions",
                column: "candidate_journey_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_sessions_interview_room_id",
                table: "interview_sessions",
                column: "interview_room_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_transcripts_session_id_segment_index",
                table: "interview_transcripts",
                columns: new[] { "session_id", "segment_index" });

            // [SKIPPED] IX_job_applications_* — already exist.

            migrationBuilder.CreateIndex(
                name: "IX_job_board_syncs_job_posting_id_board",
                table: "job_board_syncs",
                columns: new[] { "job_posting_id", "board" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_hirer_id",
                table: "job_postings",
                column: "hirer_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_status_published_at",
                table: "job_postings",
                columns: new[] { "status", "published_at" });

            // [SKIPPED] IX_mock_interview_sessions_user_id_status — already exists.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_observations");

            migrationBuilder.DropTable(
                name: "email_notifications");

            migrationBuilder.DropTable(
                name: "email_templates");

            migrationBuilder.DropTable(
                name: "hiring_decisions");

            migrationBuilder.DropTable(
                name: "hr_interviews");

            migrationBuilder.DropTable(
                name: "interview_answers");

            migrationBuilder.DropTable(
                name: "interview_final_scores");

            migrationBuilder.DropTable(
                name: "interview_transcripts");

            migrationBuilder.DropTable(
                name: "job_board_syncs");

            // [SKIPPED] mock_interview_sessions — preserved (legacy table).

            migrationBuilder.DropTable(
                name: "interview_questions");

            migrationBuilder.DropTable(
                name: "interview_sessions");

            migrationBuilder.DropTable(
                name: "candidate_journeys");

            // [SKIPPED] interview_rooms — preserved (legacy table).

            // [SKIPPED] job_applications — preserved (legacy table).

            migrationBuilder.DropTable(
                name: "job_postings");
        }
    }
}
