using Microsoft.EntityFrameworkCore;
using HireIQ.Domain.Entities;

namespace HireIQ.Infrastructure.Persistence
{

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Resume> Resumes { get; set; } = null!;
        public DbSet<JobDescription> JobDescriptions { get; set; } = null!;
        public DbSet<ScreeningResult> ScreeningResults { get; set; } = null!;
        public DbSet<Conversation> Conversations { get; set; } = null!;
        public DbSet<GeneratedResume> GeneratedResumes { get; set; } = null!;
        public DbSet<CustomResumeField> CustomResumeFields { get; set; }= null!;
        public DbSet<Template> Templates { get; set; }
        public DbSet<InterviewRoom> InterviewRooms { get; set; } = null!;
        public DbSet<MockInterviewSession> MockInterviewSessions { get; set; } = null!;
        public DbSet<JobApplication> JobApplications { get; set; } = null!;

        // ── Phase-2 hiring-automation tables ──
        public DbSet<JobPosting> JobPostings { get; set; } = null!;
        public DbSet<JobBoardSync> JobBoardSyncs { get; set; } = null!;
        public DbSet<CandidateJourney> CandidateJourneys { get; set; } = null!;
        public DbSet<EmailNotification> EmailNotifications { get; set; } = null!;
        public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;
        public DbSet<InterviewSession> InterviewSessions { get; set; } = null!;
        public DbSet<InterviewTranscript> InterviewTranscripts { get; set; } = null!;
        public DbSet<InterviewQuestion> InterviewQuestions { get; set; } = null!;
        public DbSet<InterviewAnswer> InterviewAnswers { get; set; } = null!;
        public DbSet<AiObservation> AiObservations { get; set; } = null!;
        public DbSet<InterviewFinalScore> InterviewFinalScores { get; set; } = null!;
        public DbSet<HrInterview> HrInterviews { get; set; } = null!;
        public DbSet<HiringDecision> HiringDecisions { get; set; } = null!;
        public DbSet<ConsentRecord> ConsentRecords { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigurePhase2Entities(modelBuilder);

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<JobDescription>().ToTable("job_descriptions");
            modelBuilder.Entity<ScreeningResult>().ToTable("screening_results");
            modelBuilder.Entity<Conversation>().ToTable("conversations");
            modelBuilder.Entity<Resume>().ToTable("resumes");
            modelBuilder.Entity<GeneratedResume>().ToTable("generated_resumes");
            // User
           modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<User>().Property(u => u.Id).HasColumnName("id");
            modelBuilder.Entity<User>().Property(u => u.Name).HasColumnName("name");
            modelBuilder.Entity<User>().Property(u => u.Email).HasColumnName("email");
            modelBuilder.Entity<User>().Property(u => u.PasswordHash).HasColumnName("password_hash");
            modelBuilder.Entity<User>().Property(u => u.Role).HasColumnName("role");
            modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasColumnName("created_at");

            modelBuilder.Entity<JobDescription>().ToTable("job_descriptions");
            modelBuilder.Entity<JobDescription>().Property(j => j.Id).HasColumnName("id");
            modelBuilder.Entity<JobDescription>().Property(j => j.UserId).HasColumnName("user_id");
            modelBuilder.Entity<JobDescription>().Property(j => j.Title).HasColumnName("title");
            modelBuilder.Entity<JobDescription>().Property(j => j.Content).HasColumnName("content");
            modelBuilder.Entity<JobDescription>().Property(j => j.CreatedAt).HasColumnName("created_at");

            modelBuilder.Entity<ScreeningResult>().ToTable("screening_results");
            modelBuilder.Entity<ScreeningResult>().Property(s => s.Id).HasColumnName("id");
            modelBuilder.Entity<ScreeningResult>().Property(s => s.ResumeId).HasColumnName("resume_id");
            modelBuilder.Entity<ScreeningResult>().Property(s => s.JdId).HasColumnName("jd_id");
            modelBuilder.Entity<ScreeningResult>().Property(s => s.MinilmScore).HasColumnName("minilm_score");
            modelBuilder.Entity<ScreeningResult>().Property(s => s.HireiqAnalysis).HasColumnName("hireiq_analysis");
            modelBuilder.Entity<ScreeningResult>().Property(s => s.Shortlisted).HasColumnName("shortlisted");
            modelBuilder.Entity<ScreeningResult>().Property(s => s.CandidateStatus).HasColumnName("candidate_status").HasDefaultValue("Screened");
            modelBuilder.Entity<ScreeningResult>().Property(s => s.CreatedAt).HasColumnName("created_at");
                        modelBuilder.Entity<ScreeningResult>()
                .HasOne(s => s.JobDescription)
                .WithMany()
                .HasForeignKey(s => s.JdId)
                .HasPrincipalKey(j => j.Id);

            modelBuilder.Entity<Conversation>().ToTable("conversations");
            modelBuilder.Entity<Conversation>().Property(c => c.Id).HasColumnName("id");
            modelBuilder.Entity<Conversation>().Property(c => c.UserId).HasColumnName("user_id");
            modelBuilder.Entity<Conversation>().Property(c => c.Role).HasColumnName("role");
            modelBuilder.Entity<Conversation>().Property(c => c.Content).HasColumnName("content");
            modelBuilder.Entity<Conversation>().Property(c => c.CreatedAt).HasColumnName("created_at");

            modelBuilder.Entity<Resume>().ToTable("resumes");
            modelBuilder.Entity<Resume>().Property(r => r.Id).HasColumnName("id");
            modelBuilder.Entity<Resume>().Property(r => r.UserId).HasColumnName("user_id");
            modelBuilder.Entity<Resume>().Property(r => r.CandidateName).HasColumnName("candidate_name");
            modelBuilder.Entity<Resume>().Property(r => r.Content).HasColumnName("content");
            modelBuilder.Entity<Resume>().Property(r => r.FileUrl).HasColumnName("file_url");
            modelBuilder.Entity<Resume>().Property(r => r.CreatedAt).HasColumnName("created_at");

            modelBuilder.Entity<GeneratedResume>().ToTable("generated_resumes");
            modelBuilder.Entity<GeneratedResume>().Property(g => g.Id).HasColumnName("id");
            modelBuilder.Entity<GeneratedResume>().Property(g => g.UserId).HasColumnName("user_id");
            modelBuilder.Entity<GeneratedResume>().Property(g => g.HtmlContent).HasColumnName("html_content");
            modelBuilder.Entity<GeneratedResume>().Property(g => g.PdfUrl).HasColumnName("pdf_url");
            modelBuilder.Entity<GeneratedResume>().Property(g => g.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<GeneratedResume>()
                .HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomResumeField>(e =>
            {
                e.ToTable("custom_resume_fields");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.FieldName).HasColumnName("field_name");
                e.Property(x => x.FieldValue).HasColumnName("field_value");
                e.Property(x => x.FieldType).HasColumnName("field_type");
                e.Property(x => x.Order).HasColumnName("order");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.User).WithMany()
                 .HasForeignKey(x => x.UserId);
            });
            // InterviewRoom
            modelBuilder.Entity<InterviewRoom>(e =>
            {
                e.ToTable("interview_rooms");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.RoomCode).HasColumnName("room_code");
                e.Property(x => x.RoomPassword).HasColumnName("room_password");
                e.Property(x => x.HirerId).HasColumnName("hirer_id");
                e.Property(x => x.JobId).HasColumnName("job_id");
                e.Property(x => x.CandidateEmail).HasColumnName("candidate_email");
                e.Property(x => x.CandidateUserId).HasColumnName("candidate_user_id");
                e.Property(x => x.CandidateName).HasColumnName("candidate_name");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.ScheduledAt).HasColumnName("scheduled_at");
                e.Property(x => x.StartedAt).HasColumnName("started_at");
                e.Property(x => x.EndedAt).HasColumnName("ended_at");
                e.Property(x => x.PresetQuestions).HasColumnName("preset_questions").HasColumnType("jsonb");
                e.Property(x => x.AiReport).HasColumnName("ai_report");
                e.Property(x => x.TechnicalScore).HasColumnName("technical_score");
                e.Property(x => x.BehavioralScore).HasColumnName("behavioral_score");
                e.Property(x => x.AttentionScore).HasColumnName("attention_score");
                e.Property(x => x.ConfidenceScore).HasColumnName("confidence_score");
                e.Property(x => x.EmotionScore).HasColumnName("emotion_score");
                e.Property(x => x.CommunicationScore).HasColumnName("communication_score");
                e.Property(x => x.FinalDecision).HasColumnName("final_decision");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.Hirer).WithMany().HasForeignKey(x => x.HirerId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.SetNull);
            });

            // MockInterviewSession
            modelBuilder.Entity<MockInterviewSession>(e =>
            {
                e.ToTable("mock_interview_sessions");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.JobTitle).HasColumnName("job_title");
                e.Property(x => x.JobDescription).HasColumnName("job_description");
                e.Property(x => x.Questions).HasColumnName("questions_json").HasColumnType("jsonb");
                e.Property(x => x.Answers).HasColumnName("answers_json").HasColumnType("jsonb");
                e.Property(x => x.AiEvaluation).HasColumnName("ai_evaluation");
                e.Property(x => x.TechnicalScore).HasColumnName("technical_score");
                e.Property(x => x.CommunicationScore).HasColumnName("communication_score");
                e.Property(x => x.ConfidenceScore).HasColumnName("confidence_score");
                e.Property(x => x.OverallScore).HasColumnName("overall_score");
                e.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");
                e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("InProgress"); // ✅ session persistence
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.UserId, x.Status }); // fast active-session lookup
            });

            // JobApplication
            modelBuilder.Entity<JobApplication>(e =>
            {
                e.ToTable("job_applications");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.JobId).HasColumnName("job_id");
                e.Property(x => x.ApplicantUserId).HasColumnName("applicant_user_id");
                e.Property(x => x.ResumeId).HasColumnName("resume_id");
                e.Property(x => x.CoverLetter).HasColumnName("cover_letter");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.AppliedAt).HasColumnName("applied_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
                e.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Applicant).WithMany().HasForeignKey(x => x.ApplicantUserId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.JobId, x.ApplicantUserId }).IsUnique();
            });

            modelBuilder.Entity<Template>(entity =>
            {
                entity.ToTable("templates"); // [cite: 59]
                entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(50);
                entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(50);
                entity.Property(e => e.PreviewImageUrl).HasColumnName("preview_image_url");
                entity.Property(e => e.BaseStructureJson).HasColumnName("base_structure_json").HasColumnType("jsonb");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsPremium).HasColumnName("is_premium").HasDefaultValue(false);
            });

        }

        // ────────────────────────────────────────────────────────────────
        // Phase-2 hiring automation mappings (job postings, candidate
        // journey state machine, AI interview swarm, HR + hiring decisions).
        // Kept in a separate method so the legacy OnModelCreating body stays
        // unchanged and diff-friendly.
        // ────────────────────────────────────────────────────────────────
        private static void ConfigurePhase2Entities(ModelBuilder mb)
        {
            // === JobPosting ===
            mb.Entity<JobPosting>(e =>
            {
                e.ToTable("job_postings");
                e.HasKey(x => x.Id);
                e.Property(x => x.HirerId).HasColumnName("hirer_id");
                e.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
                e.Property(x => x.Company).HasColumnName("company").HasMaxLength(200);
                e.Property(x => x.Location).HasColumnName("location").HasMaxLength(200);
                e.Property(x => x.EmploymentType).HasColumnName("employment_type").HasConversion<string>().HasMaxLength(30);
                e.Property(x => x.WorkMode).HasColumnName("work_mode").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.ExperienceMinYears).HasColumnName("experience_min_years");
                e.Property(x => x.ExperienceMaxYears).HasColumnName("experience_max_years");
                e.Property(x => x.SalaryMin).HasColumnName("salary_min").HasPrecision(14, 2);
                e.Property(x => x.SalaryMax).HasColumnName("salary_max").HasPrecision(14, 2);
                e.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(8);
                e.Property(x => x.SalaryPeriod).HasColumnName("salary_period").HasMaxLength(20);
                e.Property(x => x.Description).HasColumnName("description");
                e.Property(x => x.RequirementsJson).HasColumnName("requirements_json").HasColumnType("jsonb");
                e.Property(x => x.BenefitsJson).HasColumnName("benefits_json").HasColumnType("jsonb");
                e.Property(x => x.SkillsRequiredJson).HasColumnName("skills_required_json").HasColumnType("jsonb");
                e.Property(x => x.SkillsNiceToHaveJson).HasColumnName("skills_nice_to_have_json").HasColumnType("jsonb");
                e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.PublishedAt).HasColumnName("published_at");
                e.Property(x => x.ClosesAt).HasColumnName("closes_at");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
                e.Property(x => x.LinkedInPostUrl).HasColumnName("linkedin_post_url");
                e.Property(x => x.IndeedExternalId).HasColumnName("indeed_external_id");
                e.Property(x => x.NaukriExternalId).HasColumnName("naukri_external_id");
                e.HasOne(x => x.Hirer).WithMany().HasForeignKey(x => x.HirerId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.Status, x.PublishedAt });
            });

            // === JobBoardSync ===
            mb.Entity<JobBoardSync>(e =>
            {
                e.ToTable("job_board_syncs");
                e.HasKey(x => x.Id);
                e.Property(x => x.JobPostingId).HasColumnName("job_posting_id");
                e.Property(x => x.Board).HasColumnName("board").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.ExternalId).HasColumnName("external_id");
                e.Property(x => x.ExternalUrl).HasColumnName("external_url");
                e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.SyncedAt).HasColumnName("synced_at");
                e.Property(x => x.ErrorMessage).HasColumnName("error_message");
                e.Property(x => x.RetryCount).HasColumnName("retry_count");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.JobPosting).WithMany().HasForeignKey(x => x.JobPostingId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.JobPostingId, x.Board }).IsUnique();
            });

            // === CandidateJourney ===
            mb.Entity<CandidateJourney>(e =>
            {
                e.ToTable("candidate_journeys");
                e.HasKey(x => x.Id);
                e.Property(x => x.JobApplicationId).HasColumnName("job_application_id");
                e.Property(x => x.ApplicantUserId).HasColumnName("applicant_user_id");
                e.Property(x => x.JobPostingId).HasColumnName("job_posting_id");
                e.Property(x => x.CurrentStage).HasColumnName("current_stage").HasConversion<string>().HasMaxLength(40);
                e.Property(x => x.StageHistoryJson).HasColumnName("stage_history_json").HasColumnType("jsonb");
                e.Property(x => x.LastTransitionAt).HasColumnName("last_transition_at");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.JobApplication).WithMany().HasForeignKey(x => x.JobApplicationId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Applicant).WithMany().HasForeignKey(x => x.ApplicantUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.JobPosting).WithMany().HasForeignKey(x => x.JobPostingId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.JobPostingId, x.CurrentStage });
                e.HasIndex(x => x.JobApplicationId).IsUnique();
            });

            // === EmailNotification ===
            mb.Entity<EmailNotification>(e =>
            {
                e.ToTable("email_notifications");
                e.HasKey(x => x.Id);
                e.Property(x => x.RecipientEmail).HasColumnName("recipient_email").HasMaxLength(320);
                e.Property(x => x.RecipientName).HasColumnName("recipient_name");
                e.Property(x => x.TemplateId).HasColumnName("template_id");
                e.Property(x => x.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(40);
                e.Property(x => x.Subject).HasColumnName("subject");
                e.Property(x => x.BodyHtml).HasColumnName("body_html");
                e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.QueuedAt).HasColumnName("queued_at");
                e.Property(x => x.SentAt).HasColumnName("sent_at");
                e.Property(x => x.ErrorMessage).HasColumnName("error_message");
                e.Property(x => x.RetryCount).HasColumnName("retry_count");
                e.Property(x => x.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(50);
                e.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
                e.HasIndex(x => x.Status);
            });

            // === EmailTemplate ===
            mb.Entity<EmailTemplate>(e =>
            {
                e.ToTable("email_templates");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
                e.Property(x => x.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(40);
                e.Property(x => x.SubjectTemplate).HasColumnName("subject_template");
                e.Property(x => x.BodyTemplate).HasColumnName("body_template");
                e.Property(x => x.IsActive).HasColumnName("is_active");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
                e.HasIndex(x => x.Name).IsUnique();
                e.HasIndex(x => x.Category);
            });

            // === InterviewSession ===
            mb.Entity<InterviewSession>(e =>
            {
                e.ToTable("interview_sessions");
                e.HasKey(x => x.Id);
                e.Property(x => x.InterviewRoomId).HasColumnName("interview_room_id");
                e.Property(x => x.CandidateJourneyId).HasColumnName("candidate_journey_id");
                e.Property(x => x.StartedAt).HasColumnName("started_at");
                e.Property(x => x.EndedAt).HasColumnName("ended_at");
                e.Property(x => x.TotalDurationSeconds).HasColumnName("total_duration_seconds");
                e.Property(x => x.VideoRecordingUrl).HasColumnName("video_recording_url");
                e.Property(x => x.AudioRecordingUrl).HasColumnName("audio_recording_url");
                e.Property(x => x.TranscriptUrl).HasColumnName("transcript_url");
                e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.InterviewRoom).WithMany().HasForeignKey(x => x.InterviewRoomId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.CandidateJourney).WithMany().HasForeignKey(x => x.CandidateJourneyId).OnDelete(DeleteBehavior.Cascade);
            });

            // === InterviewTranscript ===
            mb.Entity<InterviewTranscript>(e =>
            {
                e.ToTable("interview_transcripts");
                e.HasKey(x => x.Id);
                e.Property(x => x.SessionId).HasColumnName("session_id");
                e.Property(x => x.SegmentIndex).HasColumnName("segment_index");
                e.Property(x => x.Speaker).HasColumnName("speaker").HasMaxLength(20);
                e.Property(x => x.Text).HasColumnName("text");
                e.Property(x => x.StartMs).HasColumnName("start_ms");
                e.Property(x => x.EndMs).HasColumnName("end_ms");
                e.Property(x => x.Confidence).HasColumnName("confidence");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.SessionId, x.SegmentIndex });
            });

            // === InterviewQuestion ===
            mb.Entity<InterviewQuestion>(e =>
            {
                e.ToTable("interview_questions");
                e.HasKey(x => x.Id);
                e.Property(x => x.SessionId).HasColumnName("session_id");
                e.Property(x => x.QuestionOrder).HasColumnName("question_order");
                e.Property(x => x.QuestionText).HasColumnName("question_text");
                e.Property(x => x.Source).HasColumnName("source").HasMaxLength(20);
                e.Property(x => x.AskedAt).HasColumnName("asked_at");
                e.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.SessionId, x.QuestionOrder });
            });

            // === InterviewAnswer ===
            mb.Entity<InterviewAnswer>(e =>
            {
                e.ToTable("interview_answers");
                e.HasKey(x => x.Id);
                e.Property(x => x.SessionId).HasColumnName("session_id");
                e.Property(x => x.QuestionId).HasColumnName("question_id");
                e.Property(x => x.TranscriptSegmentIdsJson).HasColumnName("transcript_segment_ids_json").HasColumnType("jsonb");
                e.Property(x => x.AnswerText).HasColumnName("answer_text");
                e.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");
                e.Property(x => x.AnsweredAt).HasColumnName("answered_at");
                e.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            });

            // === AiObservation ===
            mb.Entity<AiObservation>(e =>
            {
                e.ToTable("ai_observations");
                e.HasKey(x => x.Id);
                e.Property(x => x.SessionId).HasColumnName("session_id");
                e.Property(x => x.Agent).HasColumnName("agent").HasConversion<string>().HasMaxLength(30);
                e.Property(x => x.TurnIndex).HasColumnName("turn_index");
                e.Property(x => x.RelatedQuestionId).HasColumnName("related_question_id");
                e.Property(x => x.ObservationJson).HasColumnName("observation_json").HasColumnType("jsonb");
                e.Property(x => x.ScoreTechnical).HasColumnName("score_technical");
                e.Property(x => x.ScoreCommunication).HasColumnName("score_communication");
                e.Property(x => x.ScoreConfidence).HasColumnName("score_confidence");
                e.Property(x => x.ScoreAttention).HasColumnName("score_attention");
                e.Property(x => x.ScoreEmotion).HasColumnName("score_emotion");
                e.Property(x => x.RawResponse).HasColumnName("raw_response");
                e.Property(x => x.LatencyMs).HasColumnName("latency_ms");
                e.Property(x => x.RecordedAt).HasColumnName("recorded_at");
                e.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.SessionId, x.Agent, x.TurnIndex });
            });

            // === InterviewFinalScore ===
            mb.Entity<InterviewFinalScore>(e =>
            {
                e.ToTable("interview_final_scores");
                e.HasKey(x => x.Id);
                e.Property(x => x.SessionId).HasColumnName("session_id");
                e.Property(x => x.TechnicalScore).HasColumnName("technical_score");
                e.Property(x => x.BehavioralScore).HasColumnName("behavioral_score");
                e.Property(x => x.CommunicationScore).HasColumnName("communication_score");
                e.Property(x => x.AttentionScore).HasColumnName("attention_score");
                e.Property(x => x.ConfidenceScore).HasColumnName("confidence_score");
                e.Property(x => x.OverallScore).HasColumnName("overall_score");
                e.Property(x => x.Recommendation).HasColumnName("recommendation").HasMaxLength(20);
                e.Property(x => x.AggregatedReasoning).HasColumnName("aggregated_reasoning");
                e.Property(x => x.ComputedAt).HasColumnName("computed_at");
                e.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.SessionId).IsUnique();
            });

            // === HrInterview ===
            mb.Entity<HrInterview>(e =>
            {
                e.ToTable("hr_interviews");
                e.HasKey(x => x.Id);
                e.Property(x => x.CandidateJourneyId).HasColumnName("candidate_journey_id");
                e.Property(x => x.HirerId).HasColumnName("hirer_id");
                e.Property(x => x.ScheduledAt).HasColumnName("scheduled_at");
                e.Property(x => x.MeetLink).HasColumnName("meet_link");
                e.Property(x => x.HirerNotes).HasColumnName("hirer_notes");
                e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.Decision).HasColumnName("decision").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.CompletedAt).HasColumnName("completed_at");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.CandidateJourney).WithMany().HasForeignKey(x => x.CandidateJourneyId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Hirer).WithMany().HasForeignKey(x => x.HirerId).OnDelete(DeleteBehavior.Restrict);
            });

            // === ConsentRecord ===
            mb.Entity<ConsentRecord>(e =>
            {
                e.ToTable("consent_records");
                e.HasKey(x => x.Id);
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(40);
                e.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
                e.Property(x => x.PolicyVersion).HasColumnName("policy_version").HasMaxLength(20);
                e.Property(x => x.Granted).HasColumnName("granted");
                e.Property(x => x.Withdrawn).HasColumnName("withdrawn");
                e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
                e.Property(x => x.UserAgent).HasColumnName("user_agent");
                e.Property(x => x.RecordedAt).HasColumnName("recorded_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.UserId, x.Kind, x.RelatedEntityId, x.RecordedAt });
            });

            // === HiringDecision ===
            mb.Entity<HiringDecision>(e =>
            {
                e.ToTable("hiring_decisions");
                e.HasKey(x => x.Id);
                e.Property(x => x.CandidateJourneyId).HasColumnName("candidate_journey_id");
                e.Property(x => x.Decision).HasColumnName("decision").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.DecidedBy).HasColumnName("decided_by");
                e.Property(x => x.DecidedAt).HasColumnName("decided_at");
                e.Property(x => x.OfferedSalary).HasColumnName("offered_salary").HasPrecision(14, 2);
                e.Property(x => x.OfferedCurrency).HasColumnName("offered_currency").HasMaxLength(8);
                e.Property(x => x.JoiningDate).HasColumnName("joining_date");
                e.Property(x => x.OfferDetailsJson).HasColumnName("offer_details_json").HasColumnType("jsonb");
                e.Property(x => x.OfferLetterUrl).HasColumnName("offer_letter_url");
                e.Property(x => x.CandidateResponse).HasColumnName("candidate_response").HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.CandidateRespondedAt).HasColumnName("candidate_responded_at");
                e.HasOne(x => x.CandidateJourney).WithMany().HasForeignKey(x => x.CandidateJourneyId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Decider).WithMany().HasForeignKey(x => x.DecidedBy).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => x.CandidateJourneyId).IsUnique();
            });
        }
    }
}