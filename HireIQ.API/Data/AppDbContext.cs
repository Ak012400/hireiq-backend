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
        }
    }
}