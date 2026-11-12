namespace SurveyBasket.API.Persistence.EntitiesConfigurations;

public class VoteAnswerConfiguration : IEntityTypeConfiguration<VoteAnswer>
{
    public void Configure(EntityTypeBuilder<VoteAnswer> builder)
    {
        builder.HasKey(answer => new { answer.VoteId, answer.AnswerId });
        builder.HasOne(answer => answer.Answer)
            .WithMany(answer => answer.VoteAnswers)
            .HasForeignKey(answer => answer.AnswerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
