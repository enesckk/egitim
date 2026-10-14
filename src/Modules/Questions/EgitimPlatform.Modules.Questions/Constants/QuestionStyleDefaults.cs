using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Questions.Constants;

public static class QuestionStyleDefaults
{
    public static class Codes
    {
        public const string NewGeneration = "NEW_GENERATION";
        public const string Conceptual = "CONCEPTUAL";
        public const string Calculation = "CALCULATION";
        public const string Interpretation = "INTERPRETATION";
        public const string GraphTable = "GRAPH_TABLE";
        public const string Problem = "PROBLEM";
        public const string Visual = "VISUAL";
        public const string MultiStep = "MULTI_STEP";
    }

    public static readonly (Guid Id, string Code, string Name, string Description, int DisplayOrder)[] ControlledStyles =
    [
        (Guid.Parse("a0000000-0000-0000-0000-000000000001"), "NEW_GENERATION", "Yeni Nesil", "Yeni nesil beceri temelli sorular", 1),
        (Guid.Parse("a0000000-0000-0000-0000-000000000002"), "CONCEPTUAL", "Kavramsal", "Kavram ve tanım odaklı sorular", 2),
        (Guid.Parse("a0000000-0000-0000-0000-000000000003"), "CALCULATION", "İşlem", "Matematiksel ve formül işlem ağırlıklı sorular", 3),
        (Guid.Parse("a0000000-0000-0000-0000-000000000004"), "INTERPRETATION", "Yorum", "Metin, çıkarım ve yorumlama odaklı sorular", 4),
        (Guid.Parse("a0000000-0000-0000-0000-000000000005"), "GRAPH_TABLE", "Grafik / Tablo", "Grafik ve tablo analizi içeren sorular", 5),
        (Guid.Parse("a0000000-0000-0000-0000-000000000006"), "PROBLEM", "Problem", "Günlük hayat senaryolu problem soruları", 6),
        (Guid.Parse("a0000000-0000-0000-0000-000000000007"), "VISUAL", "Görsel", "Şekil, diyagram ve görsel içeren sorular", 7),
        (Guid.Parse("a0000000-0000-0000-0000-000000000008"), "MULTI_STEP", "Çok Adımlı", "Birden fazla işlem/kazanım gerektiren çok adımlı sorular", 8),
    ];

    /// <summary>
    /// Idempotent, safe, non-destructive seeder for initial controlled question styles.
    /// </summary>
    public static async Task SeedInitialStylesAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        var existingCodes = await db.Set<QuestionStyle>()
            .Select(s => s.Code)
            .ToListAsync(ct);

        var existingCodeSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        foreach (var (id, code, name, description, displayOrder) in ControlledStyles)
        {
            if (!existingCodeSet.Contains(code))
            {
                var style = new QuestionStyle
                {
                    Id = id,
                    Code = code,
                    Name = name,
                    Description = description,
                    IsActive = true,
                    DisplayOrder = displayOrder,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Set<QuestionStyle>().Add(style);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
