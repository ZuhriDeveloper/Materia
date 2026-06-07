using Materia.Application.Contracts.Inventory;
using Materia.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Materia.Infrastructure.Persistence;

/// <summary>
/// Seeds a realistic Indonesian "Toko Bangunan" catalog — units (satuan),
/// categories (kategori) and products (barang material).
///
/// IMPORTANT — why this is NOT EF Core <c>HasData</c> seeding:
/// the catalog tables are event-sourced projections. <see cref="Projections.UnitReadModel"/>,
/// <see cref="Projections.CategoryReadModel"/> and <see cref="Projections.ProductReadModel"/>
/// are written exclusively by the repositories from domain events appended to the
/// event store. We therefore seed through the aggregate factories + repositories so
/// that the <c>StoredEvents</c> stream (the source of truth) and the read-model
/// projections stay consistent. Inserting read-model rows directly would create
/// orphaned projections with no backing events.
///
/// The seeder is idempotent: every unit / category / product is created only when an
/// entry with the same (unique) name does not already exist.
/// </summary>
public class CatalogSeeder(
    AppDbContext context,
    IUnitRepository unitRepository,
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    ILogger<CatalogSeeder> logger)
{
    private const string SeededBy = "system@materia.local";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var unitCount = await SeedUnitsAsync(ct);
        var categories = await SeedCategoriesAsync(ct);
        var productCount = await SeedProductsAsync(categories, ct);

        logger.LogInformation(
            "Catalog seed finished. New units: {Units}, categories available: {Categories}, new products: {Products}.",
            unitCount, categories.Count, productCount);
    }

    // ── Units (Satuan) ─────────────────────────────────────────────────────────

    private async Task<int> SeedUnitsAsync(CancellationToken ct)
    {
        var created = 0;
        foreach (var (name, symbol) in Units)
        {
            if (await context.UnitReadModels.AnyAsync(u => u.Name == name, ct))
                continue;

            var unit = Unit.Create(name, symbol, SeededBy);
            await unitRepository.SaveAsync(unit, ct);
            created++;
        }
        return created;
    }

    // ── Categories (Kategori) ──────────────────────────────────────────────────

    private async Task<Dictionary<string, CategoryId>> SeedCategoriesAsync(CancellationToken ct)
    {
        var map = new Dictionary<string, CategoryId>(StringComparer.Ordinal);
        foreach (var (name, description) in Categories)
        {
            var existing = await context.CategoryReadModels
                .FirstOrDefaultAsync(c => c.Name == name, ct);

            if (existing is not null)
            {
                map[name] = CategoryId.From(existing.Id);
                continue;
            }

            var category = Category.Create(name, description, SeededBy);
            await categoryRepository.SaveAsync(category, ct);
            map[name] = category.Id;
        }
        return map;
    }

    // ── Products (Barang Material) ─────────────────────────────────────────────

    private async Task<int> SeedProductsAsync(
        IReadOnlyDictionary<string, CategoryId> categories,
        CancellationToken ct)
    {
        var created = 0;
        var seq = 0;
        foreach (var p in Products)
        {
            seq++; // keep barcodes deterministic & unique across runs, even when skipping

            if (await context.ProductReadModels.AnyAsync(pr => pr.Name == p.Name, ct))
                continue;

            var barcode = p.HasBarcode ? GenerateBarcode(seq) : null;
            var product = Product.Create(
                p.Name, p.Description, new UnitName(p.BaseUnit), SeededBy, p.SalePrice, barcode);

            if (categories.TryGetValue(p.Category, out var categoryId))
                product.AssignCategory(categoryId, SeededBy);
            else
                logger.LogWarning("Unknown category '{Category}' for product '{Product}'.", p.Category, p.Name);

            if (p.Conversions is not null)
            {
                foreach (var c in p.Conversions)
                {
                    product.AddUnitConversion(
                        new UnitConversion(new UnitName(p.BaseUnit), new UnitName(c.ToUnit), c.Factor, c.SalePrice),
                        SeededBy);
                }
            }

            await productRepository.SaveAsync(product, ct);
            created++;
        }
        return created;
    }

    /// <summary>Synthetic EAN-13-shaped barcode using Indonesia's GS1 prefix (899).</summary>
    private static string GenerateBarcode(int seq) => $"899{seq:D10}";

    // ── Seed data ──────────────────────────────────────────────────────────────

    private sealed record Conv(string ToUnit, decimal Factor, decimal SalePrice);

    private sealed record SeedProduct(
        string Category,
        string Name,
        string BaseUnit,
        decimal SalePrice,
        string? Description = null,
        bool HasBarcode = true,
        Conv[]? Conversions = null);

    /// <summary>Standard Indonesian retail units of measure. Name is unique.</summary>
    private static readonly (string Name, string? Symbol)[] Units =
    [
        ("Sak",     "sak"),
        ("Karung",  "krg"),
        ("Kubik",   "m³"),
        ("Batang",  "btg"),
        ("Lembar",  "lbr"),
        ("Pcs",     "pcs"),
        ("Buah",    "bh"),
        ("Meter",   "m"),
        ("Roll",    "rol"),
        ("Kg",      "kg"),
        ("Dus",     "dus"),
        ("Kaleng",  "klg"),
        ("Galon",   "gln"),
        ("Pail",    "pail"),
        ("Set",     "set"),
        ("Pasang",  "psg"),
        ("Botol",   "btl"),
        ("Tube",    "tube"),
        ("Bungkus", "bks"),
        ("Ikat",    "ikat"),
        ("Meter Persegi", "m²"),
        ("Colt",    "colt"),
        ("Engkel",  "engkel"),
        ("Titik",   "ttk"),
        ("Unit",    "unit"),
        ("Lusin",   "lsn"),
    ];

    /// <summary>Top-level catalog categories. Name is unique.</summary>
    private static readonly (string Name, string? Description)[] Categories =
    [
        ("Semen & Mortar",            "Semen, mortar instan, acian, dan nat keramik"),
        ("Bahan Fondasi & Agregat",   "Pasir, batu, bata, batako, bata ringan, dan paving"),
        ("Besi & Baja",               "Besi beton, hollow, baja ringan, wiremesh, dan kawat"),
        ("Keramik & Lantai",          "Keramik lantai, keramik dinding, granit, dan nat"),
        ("Atap & Plafon",             "Genteng, atap spandek/UPVC, gypsum, GRC, dan rangka plafon"),
        ("Kayu & Material Olahan",    "Triplek, multiplek, kayu kaso/reng, dan blockboard"),
        ("Perpipaan & Sanitasi",      "Pipa PVC, fitting, kran, kloset, dan floor drain"),
        ("Kelistrikan",               "Kabel, saklar, stop kontak, lampu, MCB, dan conduit"),
        ("Cat & Pengencer",           "Cat tembok, cat kayu/besi, plamir, tiner, dan kuas"),
        ("Kunci & Perkakas",          "Kunci, gembok, engsel, dan perkakas tangan"),
        ("Paku, Sekrup & Pengikat",   "Paku, sekrup, baut, mur, fischer, dan dynabolt"),
        ("Perekat & Kimia Konstruksi","Lem kayu/kuning, sealant, dan waterproofing"),
    ];

    /// <summary>Real catalog imported from the store's price list (DAFTAR LIST BAHAN BANGUNAN). HARGA JUAL is used as sale price; 0 means price to be set later.</summary>
    private static readonly SeedProduct[] Products =
    [
        // -- Semen & Mortar (15) --
        new("Semen & Mortar", "Semen Tiga Roda / 40 kg", "Sak", 62000m, HasBarcode: false),
        new("Semen & Mortar", "Semen Gresik / 40 kg", "Sak", 60000m, HasBarcode: false),
        new("Semen & Mortar", "Semen Rajawali / 40 kg", "Sak", 52000m, HasBarcode: false),
        new("Semen & Mortar", "Semen Putih Tiga Roda / 40 kg", "Sak", 0m, HasBarcode: false),
        new("Semen & Mortar", "MU-380 Perekat Bata Ringan / 40 kg", "Sak", 117000m, HasBarcode: false),
        new("Semen & Mortar", "MU-200 Acian Plesteran & Beton/ 40 kg", "Sak", 110000m, HasBarcode: false),
        new("Semen & Mortar", "MU-301 Pasangan Bata & Plester / 40 kg", "Sak", 0m, HasBarcode: false),
        new("Semen & Mortar", "MU-400 Perekat Keramik Dinding & Lantai/ 25 kg", "Sak", 135000m, HasBarcode: false),
        new("Semen & Mortar", "MU-420 Perekat Keramik Standar / 25 kg", "Sak", 100000m, HasBarcode: false),
        new("Semen & Mortar", "MU-480 Perekat Keramik di atas Keramik / 25 kg", "Sak", 245000m, HasBarcode: false),
        new("Semen & Mortar", "Mortar Indonesia D1 Perekat Bata Ringan / 40 kg", "Sak", 85000m, HasBarcode: false),
        new("Semen & Mortar", "Mortar Indonesia D2 Pasang Bata dan Plester", "Sak", 65000m, HasBarcode: false),
        new("Semen & Mortar", "SikaCeram 150 Perekat Keramik", "Sak", 0m, HasBarcode: false),
        new("Semen & Mortar", "SikaCeram 180 Perekat Granit", "Sak", 0m, HasBarcode: false),
        new("Semen & Mortar", "SikaCeram 200 Perekat Keramik diatas Keramik", "Sak", 175000m, HasBarcode: false),

        // -- Bahan Fondasi & Agregat (12) --
        new("Bahan Fondasi & Agregat", "Pasir Bangka (isi ± 1 m3)", "Colt", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Pasir Cor / Abu (isi ± 1 m3)", "Colt", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Batu Pecah Mesin 1/2 (isi ± 1 m3)", "Colt", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Pasir Bangka (isi ± 2,5 m3)", "Engkel", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Pasir Cor / Abu (isi ± 2,5 m3)", "Engkel", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Batu Pecah Mesin 1/2 (isi ± 2,5 m3)", "Engkel", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Bata Ringan 60x20x10 (isi 83)", "Kubik", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Bata Ringan 60x20x7,5 (isi 111)", "Kubik", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Bata Merah Bakar kelas I", "Buah", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Grass Blok 45 x 30", "Buah", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Con Blok Hexagonal 21 cm (isi 25)", "Meter Persegi", 0m, HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Paving Blok Warna 6 cm (isi 44)", "Meter Persegi", 0m, HasBarcode: false),

        // -- Besi & Baja (14) --
        new("Besi & Baja", "Besi 6 mmTj", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Besi 8 mmTj", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Besi 10 mmTj", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Besi 12 mmTj", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Besi 8 mm KPS / SPI", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Besi 10 mm KPS / SPI", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Besi 12 mm KPS / SPI", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Besi Ulir 10 mm Tj", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Besi Ulir 13 mm Tj", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Besi Ulir 16 mm Tj", "Batang", 0m, HasBarcode: false),
        new("Besi & Baja", "Bondek 0,7 CBM (2, 3, 4, 5, 6 meter)", "Meter", 0m, HasBarcode: false),
        new("Besi & Baja", "Spandex Bukit Duri 0,3 (6 meter)", "Kg", 0m, HasBarcode: false),
        new("Besi & Baja", "Wiremesh M8 (7,5)", "Lembar", 0m, HasBarcode: false),
        new("Besi & Baja", "Wiremesh M6", "Lembar", 0m, HasBarcode: false),

        // -- Keramik & Lantai () --
        new("Keramik & Lantai", "Glass Block DN 20 x 20 Mulia", "Buah", 0m, HasBarcode: false),

        // -- Atap & Plafon (17) --
        new("Atap & Plafon", "Gypsum 120 x 240 t= 9 mm Jayaboard", "Lembar", 75000m, HasBarcode: false),
        new("Atap & Plafon", "Gypsum 120 x 240 t= 9 mm Aplus", "Lembar", 55000m, HasBarcode: false),
        new("Atap & Plafon", "GRC (120 x 240) cm, t = 4 mm", "Lembar", 66000m, HasBarcode: false),
        new("Atap & Plafon", "GRC (120 x 240) cm, t = 6 mm", "Lembar", 125000m, HasBarcode: false),
        new("Atap & Plafon", "GRC (120 x 240) cm, t = 8 mm", "Lembar", 180000m, HasBarcode: false),
        new("Atap & Plafon", "Cornice Coumpound A Plus / 20 kg", "Sak", 0m, HasBarcode: false),
        new("Atap & Plafon", "Atap PVC Gelombang 80 x 180 Go Green", "Lembar", 65000m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Kecil 120", "Lembar", 0m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Kecil 150", "Lembar", 50000m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Kecil 180", "Lembar", 58500m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Kecil 210", "Lembar", 68000m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Kecil 240", "Lembar", 78000m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Kecil 270", "Lembar", 88000m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Kecil 300", "Lembar", 100000m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Besar 200", "Lembar", 92000m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Besar 250", "Lembar", 112000m, HasBarcode: false),
        new("Atap & Plafon", "Asbes Djabesmen Gel. Besar 300", "Lembar", 143000m, HasBarcode: false),

        // -- Kayu & Material Olahan (27) --
        new("Kayu & Material Olahan", "Kaso 4/6", "Batang", 42500m, HasBarcode: false),
        new("Kayu & Material Olahan", "Kaso 5/7", "Batang", 52500m, HasBarcode: false),
        new("Kayu & Material Olahan", "Kaso 4/6 isi 6", "Ikat", 165000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Galar 5/10", "Batang", 85000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Balok 6/12", "Batang", 115000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Balok 8/12", "Batang", 160000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Papan 2/20", "Lembar", 70000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Papan 3/20", "Lembar", 100000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Papan 3/30", "Lembar", 225000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Kaso 4/6 (104 batang)", "Kubik", 3600000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Kaso 4/6 isi 6 ( 17 ikat + 2 batang)", "Kubik", 2350000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Reng 3/4", "Batang", 0m, HasBarcode: false),
        new("Kayu & Material Olahan", "Reng 2/3 isi 20", "Ikat", 0m, HasBarcode: false),
        new("Kayu & Material Olahan", "Reng 3/4 isi 10", "Ikat", 0m, HasBarcode: false),
        new("Kayu & Material Olahan", "Triplek 3 mm 120 x 240 MC", "Lembar", 64000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Triplek 4 mm 120 x 240 MC", "Lembar", 75000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Triplek 6 mm 120 x 240 MC", "Lembar", 92000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Triplek 8 mm 120 x 240 MC", "Lembar", 115000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Triplek 9 mm 120 x 240 MC", "Lembar", 135000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Triplek 12 mm 120 x 240 MC", "Lembar", 165000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Triplek 15 mm 120 x 240 MC", "Lembar", 210000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Triplek 18 mm 120 x 240 MC", "Lembar", 230000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Triplek 3 mm ukuran pintu", "Lembar", 64000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Teak Wood 3 mm 120 x 240", "Lembar", 0m, HasBarcode: false),
        new("Kayu & Material Olahan", "Teak Wood ukuran Pintu 3 mm", "Lembar", 0m, HasBarcode: false),
        new("Kayu & Material Olahan", "Melamin Putih Kilap 3mm 120 x 240", "Lembar", 145000m, HasBarcode: false),
        new("Kayu & Material Olahan", "Melamin Putih Doff 3mm 120 x 240 cap Kunci", "Lembar", 0m, HasBarcode: false),

        // -- Perpipaan & Sanitasi (147) --
        new("Perpipaan & Sanitasi", "Besi Pipa Untuk Hydrant BSP 1\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Untuk Hydrant BSP 1.25\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Untuk Hydrant BSP 1.5\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Untuk Hydrant BSP 2\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Untuk Hydrant BSP 2.5\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Untuk Hydrant BSP 3\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Untuk Hydrant BSP 4\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Untuk Hydrant BSP 6\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Hitam 1\" t = 2 mm", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Hitam 2\" t = 2 mm", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Hitam 3\" t = 2mm", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Hitam 4\" t = 2 mm", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Besi Pipa Hitam 6\" t = 2 mm", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 1/2\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 3/4\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 1\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 1 1/4\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 1 1/2\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 1 3/4\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 2\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 2 1/2\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 3\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa GIP Medium A 4\" ( 6 m1 )", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 3/4\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 1\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 1 1/4\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 1 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 1 3/4\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 2 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 3\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Macam2 Sambungan GIP 4\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 1/2\"", "Batang", 26500m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 3/4\"", "Batang", 35000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 1\"", "Batang", 46000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 1 1/4\"", "Batang", 66000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 1 1/2\"", "Batang", 76000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 2\"", "Batang", 96000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 2 1/2\"", "Batang", 138000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 3\"", "Batang", 195000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 4\"", "Batang", 318000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 5\"", "Batang", 500000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 6\"", "Batang", 695000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard AW 8\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard D 1 1/4\"", "Batang", 43500m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard D 1 1/2\"", "Batang", 48500m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard D 2\"", "Batang", 61500m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard D 2 1/2\"", "Batang", 83000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard D 3\"", "Batang", 110000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard D 4\"", "Batang", 170000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard D 5\"", "Batang", 275000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard D 6\"", "Batang", 340000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA Standard D 8\"", "Batang", 590000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA JIS AW 1/2\"", "Batang", 43000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA JIS AW 3/4\"", "Batang", 52000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA JIS AW 1\"", "Batang", 72000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA JIS AW 1 1/4\"", "Batang", 95000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA JIS AW 1 1/2\"", "Batang", 125000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA JIS AW 2\"", "Batang", 180000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC RUCIKA JIS D 3\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC Icon AW 3\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC Icon AW 4\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC TM 4\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa PVC TM 3\"", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Stop Kran 3/4\" KIT", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Stop Kran 1\" KIT", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Stop Kran 1 1/2\" KIT", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Stop Kran 2\" KIT", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Stop Kran 2 1/2\" KIT", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Stop Kran 3\" KIT", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Check Valve 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Double Neple 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Water Mur 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Gate walve 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Saringan Air Lt. KM Stainless Steel", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Apooer Bath Tube", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Stainless Lokal Kait", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Shower Dengan Tiang", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Shower Tanpa Tiang", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Tembok Sun Eui dia. 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Tembok ITAP dia. 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Bebek Sun Eui 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Bebek ITAP 1/2\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Panas Dingin San Eui Standard", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Bath Cape Washteren", "Unit", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tempat Sabun Poslin", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Wastafel Lengkap TOTO LW 230", "Unit", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Wastafel Lengkap INA", "Unit", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Closet Jongkok Poslin Warna TOTO", "Unit", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Closet Jongkok Standard Putih Poslin TOTO", "Unit", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Closet Jongkok Lengkap Sistem Jet TOTO", "Unit", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Closet Duduk Warna Standard TOTO C 240 Lengkap", "Unit", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Wastafel Bulat Warna Standard Lengkap", "Unit", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Closet Duduk Warna Standard INA Lengkap", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Urinoir Lengkap TOTO Warna Standard Lengkap", "Unit", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Penyekat Poslin Urinoar TOTO", "Lembar", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kitchen Zink Stainless Standard Lokal ( 1 Lubang )", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kitchen Zink Stainless Non Standard Franke ( 1 Lubang )", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kitchen Zink Stainless Non Standard Franke ( 2 Lubang )", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Talang PVC U - 15 cm", "Batang", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Tembok BC 1/2\"", "Buah", 37000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Tembok BL 1/2\"", "Buah", 45000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran CLS 02", "Buah", 90000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Tembok A801 T", "Buah", 77500m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Cabang K Tul", "Buah", 130000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Cabang K 406 CTG", "Buah", 185000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Wastafel Y 316 FA", "Buah", 130000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Wastafel Y 321 C", "Buah", 185000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Dapur V 632 CA", "Buah", 135000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Dapur V Tul Flexi Tembok", "Buah", 195000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Dapur V Tul Flexi Tanam", "Buah", 195000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Jet Shower S 75 WCS Putih", "Buah", 130000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Jet Shower S 88 BM Hitam", "Buah", 150000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Hand Shower SO 250", "Buah", 190000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Hand Shower So Fres BM Hitam", "Buah", 240000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Stop Kran JF 03 TA (cabang 1)", "Buah", 80000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Stop Kran JF 08 ST (cabang 2)", "Buah", 95000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Shower Mixer K 306 CB", "Buah", 315000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "BathTub Mixer Bath Tul 1/2\"", "Buah", 575000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Bathtub Mixer Bath SUS BM Hitam", "Buah", 540000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Kran Cabang K Sus BM Hitam", "Buah", 230000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tusen Klep 1\"", "Buah", 120000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tusen Klep 3/4\"", "Buah", 90000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tusen Klep 1/2\"", "Buah", 75000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Radar", "Buah", 80000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Selang Superflex 1/2\" (50 m)", "Buah", 8500m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Selang Superflex 5/8\" (50 m)", "Buah", 10000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Selang Superflex 3/4\" (50 m)", "Buah", 13000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Sealtape", "Buah", 6000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Check Valve 1/2\" (Buah)", "Buah", 115000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Check Valve 3/4\"", "Buah", 175000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Check Valve 1\"", "Buah", 290000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "V SUS Flexi Tanam 1/2\"", "Buah", 240000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "V SUS Flexi Tembok 1/2\"", "Buah", 240000m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tangki Air Fiber Glass 300 liter TB32 ( Penguin)", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tangki Air Fiber Glass 520 liter TB55 ( Penguin)", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tangki Air Fiber Glass 650 liter TB70 ( Penguin)", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tangki Air Fiber Glass 1050 liter TB110 ( Penguin)", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Otomatis Pompa KIP 3/8\"", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Sock Kran Bulat 3 cm", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Sock Kran Bulat 4 cm", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tee Shower Stainless", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Tee Shower Putar Stainless", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pelampung Closet Hidrolic kd", "Buah", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Pipa Air Panas Pinstar 16 mm (100m)", "Roll", 0m, HasBarcode: false),
        new("Perpipaan & Sanitasi", "Jet Shower Putih - Biasa", "Buah", 0m, HasBarcode: false),

        // -- Kelistrikan (77) --
        new("Kelistrikan", "Kabel NYA 1 x 1.5 Prima ( 1 rol = 50 m' )", "Roll", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYA 1 x 2.5 Prima ( 1 rol = 50 m' )", "Roll", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 2 x 1.5 Prima ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 3 x 1.5 Prima ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 2 x 2.5 Prima ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 3 x 2.5 Prima ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 4 x 2.5 Prima ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 2 x 4 Prima ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 3 x 4 Prima ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 4 x 4 Prima ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 2 x 6 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 3 x 6 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 4 x 6 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 2 x 10 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 3 x 10 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 4 x 10 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYM 4 x 16 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 2 x 4 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 3 x 4 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 4 x 4 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 2 x 6 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 3 x 6 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 4 x 6 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 2 x 10 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 3 x 10 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 4 x 10 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "Kabel NYY 4 x 16 Supreme ( 1 rol = 50 m' )", "Meter", 0m, HasBarcode: false),
        new("Kelistrikan", "NSFB FUJI EA - 100 A", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "NSFB FUJI EA - 150 A", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Rumah Panel 30 x 60 cm ( Kosong )", "Unit", 0m, HasBarcode: false),
        new("Kelistrikan", "Skring Kas 2 grop Biasa", "Unit", 0m, HasBarcode: false),
        new("Kelistrikan", "Skring Kas 3 grop Biasa", "Unit", 0m, HasBarcode: false),
        new("Kelistrikan", "Skring Kas 5 grop Biasa", "Unit", 0m, HasBarcode: false),
        new("Kelistrikan", "MCB 1 PAS", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "MCB 3 PAS", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Tahanan 50 A Merk Fuji", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Saklar Broko Tunggal Standard ( 1 Phase )", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Saklar Broko Seri Standard ( 1 Phase )", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stop Kontak Broko Standard ( 1 Phase )", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stop Kontak Broko 3 Phase (Out Bow)", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stop Kontak Broko 3 Phase ( In Bow)", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stop Kontak Handle 3 Phase", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Instalas Titik Lampu / Stop Kontak (Upah dan Alat)", "Titik", 0m, HasBarcode: false),
        new("Kelistrikan", "Lampu pijar 25 Watt s/d 100 Watt", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Lampu Neon Phillips 20 Watt", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Lampu Neon Phillips 40 Watt", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Trapo TL 20 W (Phillips)", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Trapo TL 40 W (Phillips)", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Trapo TL 20 W (Sinar)", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Trapo TL 40 W (sinar)", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stater Neon Phillips", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stater Neon Biasa", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Rumah TL In Bow / Out Bow 2 x 20 W (kosongan)", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Down Light + SL 25 W", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Lampu SL Philip 25 W", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Lampu Sirkel TL 20 W Lengkap", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Lampu Mercuri 80 W", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Lampu Taman + Tiang + Lampu 1 Buah", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Lampu Baret 30 cm + Neon", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Lampu Neon Arcrilik 2 x 40 W Lengkap", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Jarum Penangkal Petir 16", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Kawat BC ( Tembaga )", "Kg", 0m, HasBarcode: false),
        new("Kelistrikan", "Pentanahan Penangkal Petir", "Titik", 0m, HasBarcode: false),
        new("Kelistrikan", "Pentanahan Panel", "Titik", 0m, HasBarcode: false),
        new("Kelistrikan", "Fiting Plafon Besar Broco 1210", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stop Kontak Arde 2 lubang Broco 15321", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stop Kontak 3 Lubang Broco 15330", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stop Kontak 4 Lubang Broco 15340", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Stop Kontak Arde 4 Lubang + Saklar", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "NYM 2x1,5 Eterna", "Roll", 0m, HasBarcode: false),
        new("Kelistrikan", "Inbodus Panasonic", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Fitting Plafon Hitam Broco 210L", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Contra Steker Arde 13410", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "AC Set Komplit OB STD Broco", "Set", 0m, HasBarcode: false),
        new("Kelistrikan", "Stop Kontak Arde IB New Gee Urea 5511U", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Saklar Seri New Gee Urea 6622U", "Buah", 0m, HasBarcode: false),
        new("Kelistrikan", "Steker Arde White Broco 13310", "Buah", 0m, HasBarcode: false),

        // -- Cat & Pengencer (13) --
        new("Cat & Pengencer", "Plamir Tembok Matex 1 kg", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Plamir Tembok Matex 4 kg", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Plamir Tembok Decoplamur 5 kg", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Tembok Vinilex 1 kg", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Tembok Vinilex 5 kg", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Tembok Vinilex 25 kg", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Tembok Decoplus 5 kg", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Tembok Q Luc 4,5 kg", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Tembok Nucolour 5 kg", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Kayu/Besi Decolux 0,9 Liter", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Kayu/Besi Q-Ton 0,8 Liter", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Kayu/Besi Ftalit 1 liter", "Kaleng", 0m, HasBarcode: false),
        new("Cat & Pengencer", "Cat Semprot Samurai Warna Standar / 400 cc", "Kaleng", 0m, HasBarcode: false),

        // -- Kunci & Perkakas (27) --
        new("Kunci & Perkakas", "Paku Rivet 440", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Sanding Disc Taiyo 240", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Sanding Disc Taiyo 400", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Amplas Duco 400 Taiyo", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Tembakan Lem Kaleng", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Karpet Talang Merah 90 Hanza", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Sarung Tangan Karet GDO", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Pelampung Sanho 1\"", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Palu Karet Kecil", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Palu Karet Besar", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Kape Set DN", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Sanding Pad Abus", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Terpal Jadi 2x3", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Terpal Jadi 3x4", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Terpal Jadi 4x6", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Terpal Jadi 5x7", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Terpal Jadi 6x8", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Engsel Tipis RRC 3\"", "Lusin", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Fibre Masterline Riben 0,8 mm Tipis (1m) 40 m", "Roll", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Ember Hijau Kecil Benteng", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Roda Karet 4\" H/M IZM", "Set", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Roda Karet 3\" H/M IZM", "Set", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Roda Karet 4\" Hidup + Rem IZM", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Roda Karet 3\" Hidup + Rem IZM", "Buah", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Kawat Parabola Baja Hanzpro 14 kg Hitam", "Roll", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Tarikan Laci Knop Kayu Besar Natural", "Lusin", 0m, HasBarcode: false),
        new("Kunci & Perkakas", "Kunci Pintu Alumunium Stainless OCD", "Buah", 0m, HasBarcode: false),

        // -- Paku, Sekrup & Pengikat (2) --
        new("Paku, Sekrup & Pengikat", "Fisher S6 HPP", "Buah", 0m, HasBarcode: false),
        new("Paku, Sekrup & Pengikat", "Fisher S8", "Buah", 0m, HasBarcode: false),

        // -- Perekat & Kimia Konstruksi (8) --
        new("Perekat & Kimia Konstruksi", "Aquaproof 1 Kg", "Kg", 67000m, HasBarcode: false),
        new("Perekat & Kimia Konstruksi", "Aquaproof 4 Kg", "Kg", 245000m, HasBarcode: false),
        new("Perekat & Kimia Konstruksi", "Aquaproof 20 Kg", "Kg", 1100000m, HasBarcode: false),
        new("Perekat & Kimia Konstruksi", "Wallmate Quickseal 1 Kg", "Kg", 82500m, HasBarcode: false),
        new("Perekat & Kimia Konstruksi", "Wallmate Quickseal 4 Kg", "Kg", 295000m, HasBarcode: false),
        new("Perekat & Kimia Konstruksi", "Wallmate Quickseal 20 Kg", "Kg", 1325000m, HasBarcode: false),
        new("Perekat & Kimia Konstruksi", "Wallmate Hydroblock 1 Kg", "Kg", 150000m, HasBarcode: false),
        new("Perekat & Kimia Konstruksi", "Wallmate Hydroblock 4 Kg", "Kg", 450000m, HasBarcode: false),
    ];
}
