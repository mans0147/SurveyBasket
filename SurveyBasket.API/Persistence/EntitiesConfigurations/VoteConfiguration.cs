namespace SurveyBasket.API.Persistence.EntitiesConfigurations;

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasIndex(vote => new { vote.PollId, vote.UserId }).IsUnique();
        builder.Property(vote => vote.SubmittedAtUtc).IsRequired();
        builder.HasMany(vote => vote.Answers)
            .WithOne(answer => answer.Vote)
            .HasForeignKey(answer => answer.VoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
