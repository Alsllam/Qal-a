using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qala.Framework.Domain.Constants;
using Qala.Framework.EntityFrameworkCore.Configurations;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Entities;

namespace Qala.Game.Matches.EntityFrameworkCore.Configurations;

public sealed class MatchConfiguration : DefaultEntityTypeConfiguration<Match>
{
    public override void Configure(EntityTypeBuilder<Match> builder)
    {
        base.Configure(builder);
        builder.ToTable("Matches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RulesVersion).HasMaxLength(FieldDefinitions.MaxRulesVersionLength).IsRequired();
        builder.Property(x => x.ChallengeCode).HasMaxLength(MatchConsts.ChallengeCodeLength).IsUnicode(false);
        builder.Property(x => x.CreatorDisplayName).HasMaxLength(FieldDefinitions.MaxDisplayNameLength).IsRequired();
        builder.Property(x => x.SouthDisplayName).HasMaxLength(FieldDefinitions.MaxDisplayNameLength);
        builder.Property(x => x.NorthDisplayName).HasMaxLength(FieldDefinitions.MaxDisplayNameLength);
        builder.Property(x => x.Position).HasMaxLength(FieldDefinitions.MaxPositionLength).IsUnicode(false).IsRequired();
        builder.Ignore(x => x.SideToMove);
        builder.Ignore(x => x.TimeControl);

        builder.HasMany(x => x.Moves).WithOne().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Moves).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.ChallengeCode).IsUnique().HasFilter("[ChallengeCode] IS NOT NULL");
        builder.HasIndex(x => new { x.Status, x.FlagFallsAt });
        builder.HasIndex(x => x.SouthPlayerId);
        builder.HasIndex(x => x.NorthPlayerId);
        builder.HasIndex(x => new { x.Status, x.FinishedAt, x.RulesVersion });
    }
}

public sealed class MatchMoveConfiguration : DefaultEntityTypeConfiguration<MatchMove>
{
    public override void Configure(EntityTypeBuilder<MatchMove> builder)
    {
        base.Configure(builder);
        builder.ToTable("MatchMoves");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Notation).HasMaxLength(MatchMove.MaxNotationLength).IsUnicode(false).IsRequired();
        builder.HasIndex(x => new { x.MatchId, x.Ply }).IsUnique();
    }
}

public sealed class PlayerRatingConfiguration : DefaultEntityTypeConfiguration<PlayerRating>
{
    public override void Configure(EntityTypeBuilder<PlayerRating> builder)
    {
        base.Configure(builder);
        builder.ToTable("PlayerRatings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DisplayName).HasMaxLength(FieldDefinitions.MaxDisplayNameLength).IsRequired();
    }
}
