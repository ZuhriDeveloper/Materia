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

    /// <summary>116 realistic products with local brands, specs, and IDR retail prices.</summary>
    private static readonly SeedProduct[] Products =
    [
        // ── Semen & Mortar ──────────────────────────────────────────────────────
        new("Semen & Mortar", "Semen Tiga Roda 40 kg", "Sak", 52_000m,
            "Semen Portland tipe I (PCC), kemasan 40 kg", Conversions: [new Conv("Kg", 40m, 1_500m)]),
        new("Semen & Mortar", "Semen Tiga Roda 50 kg", "Sak", 64_000m,
            "Semen Portland tipe I (PCC), kemasan 50 kg", Conversions: [new Conv("Kg", 50m, 1_450m)]),
        new("Semen & Mortar", "Semen Gresik 40 kg", "Sak", 53_000m,
            "Semen Portland PCC, kemasan 40 kg", Conversions: [new Conv("Kg", 40m, 1_550m)]),
        new("Semen & Mortar", "Semen Padang 50 kg", "Sak", 63_000m, "Semen Portland PCC, kemasan 50 kg"),
        new("Semen & Mortar", "Semen Dynamix (Holcim) 40 kg", "Sak", 51_000m, "Semen serbaguna, kemasan 40 kg"),
        new("Semen & Mortar", "Semen Putih Tiga Roda 40 kg", "Sak", 110_000m, "Semen putih untuk finishing & nat"),
        new("Semen & Mortar", "Mortar Utama MU-301 Pasang Bata 40 kg", "Sak", 72_000m, "Semen instan perekat pasangan bata"),
        new("Semen & Mortar", "Semen Instan Aplus Plester 40 kg", "Sak", 58_000m, "Mortar plester dinding siap pakai"),
        new("Semen & Mortar", "Acian Putih MU-200 40 kg", "Sak", 95_000m, "Semen instan acian halus warna putih"),
        new("Semen & Mortar", "Nat Keramik AM 50 (Tile Grout) 1 kg", "Bungkus", 12_000m, "Pengisi nat keramik aneka warna"),

        // ── Bahan Fondasi & Agregat ────────────────────────────────────────────
        new("Bahan Fondasi & Agregat", "Pasir Pasang (per Kubik)", "Kubik", 230_000m,
            "Pasir pasang halus untuk plesteran, per m³", HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Pasir Beton Cor (per Kubik)", "Kubik", 270_000m,
            "Pasir kasar untuk pengecoran, per m³", HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Pasir Pasang (per Karung ±25 kg)", "Karung", 18_000m, "Pasir pasang kemasan karung eceran"),
        new("Bahan Fondasi & Agregat", "Batu Split 1/2 (per Kubik)", "Kubik", 320_000m,
            "Batu pecah ukuran 1-2 cm untuk cor, per m³", HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Batu Kali Belah (per Kubik)", "Kubik", 250_000m,
            "Batu kali untuk pondasi, per m³", HasBarcode: false),
        new("Bahan Fondasi & Agregat", "Bata Merah Press", "Pcs", 900m, "Bata merah press lokal kualitas baik"),
        new("Bahan Fondasi & Agregat", "Batako Press", "Pcs", 3_500m, "Batako press getar abu-abu"),
        new("Bahan Fondasi & Agregat", "Bata Ringan Hebel 7.5 cm", "Pcs", 9_500m, "Bata ringan AAC tebal 7,5 cm"),
        new("Bahan Fondasi & Agregat", "Bata Ringan Hebel 10 cm", "Pcs", 12_500m, "Bata ringan AAC tebal 10 cm"),
        new("Bahan Fondasi & Agregat", "Roster Beton Minimalis", "Pcs", 8_500m, "Roster/ventilasi beton motif minimalis"),
        new("Bahan Fondasi & Agregat", "Paving Block Bata Abu K-300", "Pcs", 4_500m, "Paving block model bata, mutu K-300"),
        new("Bahan Fondasi & Agregat", "Kanstin Taman 10x20x40", "Pcs", 22_000m, "Kanstin/kanstein pembatas taman"),

        // ── Besi & Baja ─────────────────────────────────────────────────────────
        new("Besi & Baja", "Besi Beton Polos 8 mm x 12 m SNI", "Batang", 58_000m,
            "Besi beton polos SNI, panjang 12 m", Conversions: [new Conv("Kg", 4.74m, 13_500m)]),
        new("Besi & Baja", "Besi Beton Ulir 10 mm x 12 m SNI", "Batang", 92_000m, "Besi beton ulir (deform) SNI 12 m"),
        new("Besi & Baja", "Besi Beton Polos 6 mm x 12 m", "Batang", 34_000m, "Besi beton polos banci/SNI 12 m"),
        new("Besi & Baja", "Besi Beton Ulir 13 mm x 12 m SNI", "Batang", 150_000m, "Besi beton ulir SNI 12 m"),
        new("Besi & Baja", "Besi Hollow Galvalum 4x4 0.3 mm", "Batang", 42_000m, "Hollow galvalum untuk rangka plafon"),
        new("Besi & Baja", "Besi Hollow Hitam 4x2 1.2 mm", "Batang", 65_000m, "Hollow besi hitam untuk pagar/kanopi"),
        new("Besi & Baja", "Baja Ringan Kanal C75 0.75 mm", "Batang", 78_000m, "Kanal C baja ringan rangka atap"),
        new("Besi & Baja", "Reng Baja Ringan R32", "Batang", 38_000m, "Reng baja ringan profil R32"),
        new("Besi & Baja", "Wiremesh M6 2.1x5.4 m", "Lembar", 165_000m, "Besi anyaman wiremesh M6 untuk dak"),
        new("Besi & Baja", "Plat Strip Besi 3x30 mm", "Batang", 55_000m, "Plat strip besi untuk behel/pagar"),
        new("Besi & Baja", "Kawat Bendrat (per Kg)", "Kg", 22_000m, "Kawat ikat tulangan beton"),
        new("Besi & Baja", "Kawat Duri (per Roll)", "Roll", 95_000m, "Kawat duri galvanis untuk pagar"),
        new("Besi & Baja", "Besi Siku 40x40x3 mm 6 m", "Batang", 110_000m, "Besi siku/L untuk konstruksi"),

        // ── Keramik & Lantai ───────────────────────────────────────────────────
        new("Keramik & Lantai", "Keramik Lantai Asia Tile 40x40 Putih", "Dus", 58_000m,
            "Keramik lantai 40x40, 1 dus = 6 keping (±0,96 m²)", Conversions: [new Conv("Pcs", 6m, 11_000m)]),
        new("Keramik & Lantai", "Keramik Lantai Platinum 50x50", "Dus", 72_000m,
            "Keramik lantai 50x50, 1 dus = 4 keping (1 m²)", Conversions: [new Conv("Pcs", 4m, 19_000m)]),
        new("Keramik & Lantai", "Keramik Dinding Roman 25x40", "Dus", 65_000m,
            "Keramik dinding 25x40, 1 dus = 11 keping", Conversions: [new Conv("Pcs", 11m, 6_500m)]),
        new("Keramik & Lantai", "Granit Tile Garuda 60x60 Polished", "Dus", 145_000m,
            "Granit tile 60x60, 1 dus = 4 keping (1,44 m²)", Conversions: [new Conv("Pcs", 4m, 38_000m)]),
        new("Keramik & Lantai", "Keramik Lantai KW2 20x20 Kamar Mandi", "Dus", 48_000m,
            "Keramik kasar 20x20, 1 dus = 25 keping (1 m²)", Conversions: [new Conv("Pcs", 25m, 2_200m)]),
        new("Keramik & Lantai", "Plint Keramik 10x40", "Pcs", 7_500m, "Plint/list keramik dinding lantai"),

        // ── Atap & Plafon ──────────────────────────────────────────────────────
        new("Atap & Plafon", "Genteng Beton Flat Mutiara", "Pcs", 8_500m, "Genteng beton flat warna doff"),
        new("Atap & Plafon", "Genteng Metal Pasir Multiroof 2x4", "Lembar", 42_000m, "Genteng metal berpasir 2x4 daun"),
        new("Atap & Plafon", "Atap Spandek 0.3 mm (per Meter)", "Meter", 55_000m, "Atap spandek galvalum, dijual per meter lari"),
        new("Atap & Plafon", "Asbes Gelombang 150x105 cm", "Lembar", 58_000m, "Atap asbes gelombang besar"),
        new("Atap & Plafon", "Atap UPVC Alderon Single Layer", "Meter", 95_000m, "Atap UPVC peredam panas, per meter"),
        new("Atap & Plafon", "Nok Genteng Beton", "Pcs", 12_000m, "Nok/bubungan genteng beton"),
        new("Atap & Plafon", "Gypsum Board Jayaboard 9 mm 120x240", "Lembar", 62_000m, "Papan gypsum plafon 9 mm"),
        new("Atap & Plafon", "GRC Board 4 mm 120x240", "Lembar", 78_000m, "Papan GRC tahan lembap 4 mm"),
        new("Atap & Plafon", "Kalsiboard 4 mm 120x240", "Lembar", 85_000m, "Papan kalsium silikat 4 mm"),
        new("Atap & Plafon", "Plafon PVC Shunda 4 m", "Batang", 48_000m, "Plafon PVC motif, panjang 4 m"),
        new("Atap & Plafon", "List Plafon Gypsum Profil 4 m", "Batang", 18_000m, "List/lis profil gypsum sudut plafon"),

        // ── Kayu & Material Olahan ─────────────────────────────────────────────
        new("Kayu & Material Olahan", "Triplek 9 mm 122x244", "Lembar", 135_000m, "Plywood/triplek tebal 9 mm"),
        new("Kayu & Material Olahan", "Triplek 4 mm 122x244", "Lembar", 78_000m, "Plywood/triplek tipis 4 mm"),
        new("Kayu & Material Olahan", "Multiplek 12 mm 122x244", "Lembar", 185_000m, "Multipleks 12 mm untuk furnitur"),
        new("Kayu & Material Olahan", "Blockboard 18 mm 122x244", "Lembar", 165_000m, "Blockboard isi kayu 18 mm"),
        new("Kayu & Material Olahan", "Kayu Kaso 5/7 Meranti 4 m", "Batang", 38_000m, "Kayu kaso 5x7 cm untuk rangka"),
        new("Kayu & Material Olahan", "Kayu Reng 3/4 Meranti 4 m", "Batang", 14_000m, "Kayu reng 3x4 cm untuk atap"),
        new("Kayu & Material Olahan", "Papan Cor Meranti 2/20 4 m", "Lembar", 45_000m, "Papan kayu untuk bekisting cor"),
        new("Kayu & Material Olahan", "Bambu Steger 4 m (per Ikat isi 10)", "Ikat", 250_000m, "Bambu steger/perancah, 1 ikat = 10 batang"),

        // ── Perpipaan & Sanitasi ───────────────────────────────────────────────
        new("Perpipaan & Sanitasi", "Pipa PVC Rucika AW 1/2\" 4 m", "Batang", 38_000m, "Pipa PVC tipe AW (tebal) 1/2 inch"),
        new("Perpipaan & Sanitasi", "Pipa PVC Wavin AW 3/4\" 4 m", "Batang", 52_000m, "Pipa PVC tipe AW 3/4 inch"),
        new("Perpipaan & Sanitasi", "Pipa PVC Wavin D 3\" 4 m", "Batang", 78_000m, "Pipa PVC tipe D buangan 3 inch"),
        new("Perpipaan & Sanitasi", "Pipa PVC Maspion AW 4\" 4 m", "Batang", 145_000m, "Pipa PVC tipe AW 4 inch"),
        new("Perpipaan & Sanitasi", "Knee/Elbow PVC 3/4\"", "Pcs", 3_500m, "Sambungan belok PVC 3/4 inch"),
        new("Perpipaan & Sanitasi", "Tee PVC 3/4\"", "Pcs", 4_500m, "Sambungan cabang T PVC 3/4 inch"),
        new("Perpipaan & Sanitasi", "Lem Pipa PVC Isarplas 100 gr", "Kaleng", 15_000m, "Lem perekat pipa PVC"),
        new("Perpipaan & Sanitasi", "Kran Air Tembok Onda 1/2\"", "Buah", 35_000m, "Kran air dinding kuningan 1/2 inch"),
        new("Perpipaan & Sanitasi", "Stop Kran 1/2\" Kuningan", "Buah", 42_000m, "Stop kran/gate valve kuningan"),
        new("Perpipaan & Sanitasi", "Kloset Jongkok INA", "Buah", 185_000m, "Kloset jongkok keramik putih"),
        new("Perpipaan & Sanitasi", "Floor Drain Stainless 4\"", "Buah", 28_000m, "Saringan air lantai stainless"),
        new("Perpipaan & Sanitasi", "Seal Tape (Isolasi Pipa)", "Pcs", 2_500m, "Seal tape penyambung drat pipa"),

        // ── Kelistrikan ────────────────────────────────────────────────────────
        new("Kelistrikan", "Kabel NYM Eterna 2x1.5 mm (Roll 50 m)", "Roll", 380_000m,
            "Kabel listrik NYM 2x1,5 mm, 1 roll = 50 m", Conversions: [new Conv("Meter", 50m, 8_500m)]),
        new("Kelistrikan", "Kabel NYA Supreme 1x2.5 mm (Roll 100 m)", "Roll", 520_000m,
            "Kabel listrik NYA 1x2,5 mm, 1 roll = 100 m", Conversions: [new Conv("Meter", 100m, 5_800m)]),
        new("Kelistrikan", "Saklar Ganda Broco", "Buah", 22_000m, "Saklar seri/ganda merek Broco"),
        new("Kelistrikan", "Stop Kontak Broco Inbow", "Buah", 25_000m, "Stop kontak tanam dinding"),
        new("Kelistrikan", "Fitting Lampu Plafon Broco", "Buah", 12_000m, "Fitting/dudukan lampu E27"),
        new("Kelistrikan", "Lampu LED Philips 12 W", "Buah", 38_000m, "Bohlam LED 12 watt cahaya putih"),
        new("Kelistrikan", "MCB Schneider 1 Phase 6A", "Buah", 65_000m, "MCB pengaman arus 6 ampere"),
        new("Kelistrikan", "Pipa Conduit Listrik 5/8\"", "Batang", 9_000m, "Pelindung kabel/pralon listrik"),
        new("Kelistrikan", "Isolasi Listrik Nitto", "Pcs", 6_000m, "Pita isolasi kabel hitam"),
        new("Kelistrikan", "T-Dus / Kotak Sambung Kabel", "Buah", 4_500m, "Kotak percabangan kabel plafon"),

        // ── Cat & Pengencer ────────────────────────────────────────────────────
        new("Cat & Pengencer", "Cat Tembok Avitex 25 kg", "Pail", 285_000m,
            "Cat tembok interior, kemasan pail 25 kg", Conversions: [new Conv("Kg", 25m, 12_500m)]),
        new("Cat & Pengencer", "Cat Tembok Dulux Pentalite 25 kg", "Pail", 720_000m, "Cat tembok premium 25 kg"),
        new("Cat & Pengencer", "Cat Tembok Catylac 5 kg", "Galon", 95_000m, "Cat tembok ekonomis galon 5 kg"),
        new("Cat & Pengencer", "Cat Kayu & Besi Avian 1 kg", "Kaleng", 68_000m, "Cat minyak kayu/besi kilap 1 kg"),
        new("Cat & Pengencer", "Cat Minyak Emco 1 kg", "Kaleng", 72_000m, "Cat minyak serbaguna 1 kg"),
        new("Cat & Pengencer", "Plamir Tembok 25 kg", "Pail", 165_000m, "Plamir/dempul dasar tembok"),
        new("Cat & Pengencer", "Tiner A Special 1 L", "Botol", 22_000m, "Tiner/pengencer cat minyak"),
        new("Cat & Pengencer", "Pelitur Kayu Mowilex 1 L", "Kaleng", 55_000m, "Politur/plitur finishing kayu"),
        new("Cat & Pengencer", "Waterproofing No Drop 4 kg", "Kaleng", 145_000m, "Pelapis anti bocor elastis"),
        new("Cat & Pengencer", "Kuas Cat 3 inch", "Buah", 12_000m, "Kuas cat bulu nilon 3 inch"),
        new("Cat & Pengencer", "Roll Cat Bulu + Bak", "Set", 35_000m, "Set roll cat tembok 9 inch + bak"),

        // ── Kunci & Perkakas ───────────────────────────────────────────────────
        new("Kunci & Perkakas", "Kunci Pintu Handle Set Dekkson", "Set", 165_000m, "Set handle + body kunci pintu"),
        new("Kunci & Perkakas", "Gembok Yale 50 mm", "Buah", 85_000m, "Gembok kuningan 50 mm 3 kunci"),
        new("Kunci & Perkakas", "Engsel Pintu Stainless 4\"", "Pasang", 28_000m, "Engsel kupu-kupu stainless 4 inch"),
        new("Kunci & Perkakas", "Grendel/Slot Pintu 4\"", "Buah", 15_000m, "Grendel pintu/jendela"),
        new("Kunci & Perkakas", "Palu Kambing 0.5 kg", "Buah", 45_000m, "Palu cabut paku gagang kayu"),
        new("Kunci & Perkakas", "Meteran Roll 5 m", "Buah", 28_000m, "Meteran gulung 5 meter"),
        new("Kunci & Perkakas", "Gergaji Kayu 12\"", "Buah", 38_000m, "Gergaji tangan untuk kayu"),
        new("Kunci & Perkakas", "Obeng Set Tekiro", "Set", 65_000m, "Set obeng plus/minus berbagai ukuran"),
        new("Kunci & Perkakas", "Tang Kombinasi 8\"", "Buah", 42_000m, "Tang kombinasi serbaguna 8 inch"),
        new("Kunci & Perkakas", "Cangkul + Gagang", "Buah", 65_000m, "Cangkul baja lengkap gagang kayu"),
        new("Kunci & Perkakas", "Ember Cor 20 L", "Buah", 25_000m, "Ember plastik tebal untuk adukan"),

        // ── Paku, Sekrup & Pengikat ────────────────────────────────────────────
        new("Paku, Sekrup & Pengikat", "Paku Beton 5 cm (per Kg)", "Kg", 28_000m, "Paku beton baja keras 5 cm"),
        new("Paku, Sekrup & Pengikat", "Paku Kayu 2.5\" / 7 cm (per Kg)", "Kg", 22_000m, "Paku kayu biasa 7 cm"),
        new("Paku, Sekrup & Pengikat", "Paku Payung Seng (per Kg)", "Kg", 26_000m, "Paku payung untuk atap seng/asbes"),
        new("Paku, Sekrup & Pengikat", "Sekrup Gypsum 1\" (per Dus)", "Dus", 45_000m, "Sekrup hitam gypsum, kemasan dus"),
        new("Paku, Sekrup & Pengikat", "Baut + Mur 12 mm", "Pcs", 3_500m, "Baut hex + mur galvanis 12 mm"),
        new("Paku, Sekrup & Pengikat", "Fischer S6 (per Bungkus isi 100)", "Bungkus", 8_500m, "Fischer/fisher tembok S6"),
        new("Paku, Sekrup & Pengikat", "Dynabolt 10 mm", "Pcs", 6_500m, "Angkur dynabolt baja 10 mm"),

        // ── Perekat & Kimia Konstruksi ─────────────────────────────────────────
        new("Perekat & Kimia Konstruksi", "Lem Kayu Fox Putih 600 gr", "Botol", 28_000m, "Lem kayu PVAc putih 600 gr"),
        new("Perekat & Kimia Konstruksi", "Lem Kuning Aibon 700 gr", "Kaleng", 65_000m, "Lem kontak serbaguna 700 gr"),
        new("Perekat & Kimia Konstruksi", "Lem Korea Alteco 3 gr", "Pcs", 8_000m, "Lem cepat kering (super glue)"),
        new("Perekat & Kimia Konstruksi", "Sealant Silikon Dextone", "Tube", 32_000m, "Sealant silikon kaca/sanitasi"),
        new("Perekat & Kimia Konstruksi", "Sika Waterproofing Sikatop (per Kg)", "Kg", 48_000m, "Pelapis kedap air semen modifikasi"),
    ];
}
