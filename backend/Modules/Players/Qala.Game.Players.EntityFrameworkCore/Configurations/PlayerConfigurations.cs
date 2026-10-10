using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qala.Framework.Domain.Constants;
using Qala.Framework.EntityFrameworkCore.Configurations;
using Qala.Game.Players.Domain.Entities;

namespace Qala.Game.Players.EntityFrameworkCore.Configurations;

public sealed class PlayerConfiguration : DefaultEntityTypeConfiguration<Player>
{
    public override void Configure(EntityTypeBuilder<Player> builder)
    {
        base.Configure(builder);
        builder.ToTable("Players");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DisplayName).HasMaxLength(FieldDefinitions.MaxDisplayNameLength).IsRequired();
        builder.Property(x => x.AvatarId).HasMaxLength(FieldDefinitions.MaxAvatarIdLength);
        builder.Property(x => x.Locale).HasMaxLength(FieldDefinitions.MaxLocaleLength).IsRequired();
        builder.Ignore(x => x.Glicko);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.Rating });
    }
}

public sealed class RatedMatchConfiguration : DefaultEntityTypeConfiguration<RatedMatch>
{
    public override void Configure(EntityTypeBuilder<RatedMatch> builder)
    {
        base.Configure(builder);
        builder.ToTable("RatedMatches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
