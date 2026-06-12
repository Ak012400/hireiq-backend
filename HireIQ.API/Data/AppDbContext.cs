using Microsoft.EntityFrameworkCore;
using HireIQ.API.Models;

namespace HireIQ.API.Data
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
    }
}