namespace SurveyBasket.API.Persistence.EntitiesConfigurations;

public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.Property(answer => answer.Content).HasMaxLength(300).IsRequired();
    }
}
