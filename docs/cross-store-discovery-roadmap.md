# Roadmap Arsitektur — Master Product & Penemuan Produk Lintas-Toko

> **Status:** Dokumen strategi (disetujui 2026-06-14). Berisi keputusan arsitektur, prinsip, dan urutan fase — **bukan** spesifikasi implementasi baris-per-baris. Detail kode menyusul saat tiap fase dieksekusi (ikuti checklist TDD di [`CLAUDE.md`](../CLAUDE.md)).

## Context

Saat ini master product **per-store**: setiap toko punya katalog sendiri, dan barang fisik yang sama (mis. "Semen Tiga Roda 50kg") terduplikasi sebagai aggregate `Product` terpisah di tiap toko — `ProductId`, barcode, harga, nama (sering beda ejaan) semuanya independen. Lihat `ProductRepository` yang selalu memfilter `e.StoreId == currentStore.StoreId` dan `ProductReadModel.StoreId`.

Arah produk yang diinginkan:
- Toko-toko **independen**, pemilik berbeda, tanpa HQ/pusat → tidak ada otoritas yang bisa memaksakan katalog seragam.
- Materia menjadi **sumber data produk** untuk aplikasi lain. Aplikasi konsumen mencari sebuah produk lalu menemukan **toko terdekat** yang menjualnya (sudah ada benih: `CatalogController` `[AllowAnonymous]` dipakai integrasi "Renovin AI").

Keputusan yang sudah diambil:
1. Deliverable awal = **roadmap strategi**.
2. Kualitas barcode: **banyak barang tanpa barcode** (curah/kiloan) → pencocokan tidak boleh hanya mengandalkan barcode.
3. Data publik = **ketersediaan + harga + jarak** (perbandingan harga penuh).

## Prinsip Inti

**Jangan bangun master catalog yang mengikat di sisi tulis (write-time).** Karena toko independen tanpa governance pusat, memaksa SKU bersama saat input akan melanggar otonomi dan tidak akan dipatuhi. Sebaliknya:

> **Tiap katalog toko tetap independen & otoritatif. Tambahkan _lapisan penemuan (discovery) di sisi baca_** yang (a) menggeolokasi toko dan (b) mencocokkan produk setara antar-toko, lalu mengeksposnya lewat API publik.

Identitas produk kanonik bersifat **turunan (projection)**, hasil _observasi_ konvergensi antar toko — bukan entitas yang dimiliki/diatur toko.

## Arsitektur Target — "Federated Catalog + Geospatial Discovery"

Lima blok bangunan:

1. **Geolokasi toko** — fondasi "terdekat".
   - Tambah `Latitude`/`Longitude` ke aggregate `Store` + `StoreReadModel` (sekarang hanya ada `Address`, `Phone`, `MaxDeliveryDistanceKm`, tanpa koordinat).
   - **Reuse pola Customer** yang sudah ada: value object `Coordinates`, `DistanceCalculator` (Haversine), dan `CustomerQueryRepository.GetNearbyAsync()` (pra-filter bounding-box → refine Haversine).
   - Event past-tense baru (mis. `StoreLocationUpdated`); endpoint profil toko agar pemilik menaruh pin.

2. **Read model penemuan lintas-toko** — "indeks global".
   - Manfaatkan **query-time join** `ProductReadModel × StoreReadModel` **tanpa store-filter** (filter EF Core sudah null-tolerant: `CurrentStoreId == null` → lihat semua toko). Hindari denormalisasi dini agar tak ada masalah staleness.
   - Naikkan ke read model terdenormalisasi / indeks khusus hanya bila volume menuntut (lihat Fase 4).

3. **Identitas & pencocokan produk antar-toko** — inti jawaban "master product".
   - Karena banyak barang tanpa barcode, pencocokan **berlapis**:
     - **Tier 1 — Barcode/GTIN sama** → klaster kuat, presisi tinggi.
     - **Tier 2 — Nama ternormalisasi** (lowercase, buang tanda baca, normalisasi satuan/ukuran, kamus sinonim domain bahan bangunan) + `BaseUnit` + kategori → klaster kandidat (confidence sedang).
     - **Tier 3 — Tak cocok** → tampil sebagai listing tunggal apa adanya.
   - `CanonicalProduct` = **projection turunan**, menyimpan confidence; sediakan tooling review/merge manual untuk koreksi Tier 2.
   - Ini fundamentalnya **masalah kualitas data**, bukan sekadar skema: kamus normalisasi domain (semen, besi, pipa, cat, ukuran "50kg/50 kg/50KG") adalah aset inti.

4. **API penemuan publik** — evolusi `CatalogController`.
   - Dari `GET /api/catalog/search` → endpoint discovery yang menerima `q` + `lat/lng` + `radiusKm`, mengembalikan produk **dikelompokkan per identitas kanonik**, tiap kelompok berisi listing toko terdekat terurut jarak/harga/ketersediaan.
   - Ekspos **ketersediaan + harga + jarak** (sesuai keputusan).

5. **Auth klien eksternal** — saat ini hanya `[AllowAnonymous]` polos.
   - Tambah konsep **API key** untuk klien eksternal + rate limiting sebelum dibuka luas (Fase 4).

## Reuse yang Sudah Ada (jangan bikin baru)

| Kebutuhan | Sudah ada di kode |
|---|---|
| Pencarian lintas-toko anonim | `Materia.Backend/Materia.WebApi/Controllers/Catalog/CatalogController.cs` |
| Jarak geografis | `Materia.Backend/Materia.Domain/Customers/DistanceCalculator.cs` (Haversine), `Materia.Backend/Materia.Domain/Customers/Coordinates.cs` |
| Query "terdekat" | `Materia.Backend/Materia.Infrastructure/Customers/CustomerQueryRepository.cs` → `GetNearbyAsync()` (bounding-box + refine) |
| Baca lintas-toko | Global query filter null-tolerant di `Materia.Backend/Materia.Infrastructure/Persistence/AppDbContext.cs` (`CurrentStoreId == null` → semua toko) |
| Update read model | Pola projection inline di tiap `*Repository.SaveAsync()` (sinkron, satu transaksi) |

## Roadmap (urutan fase)

### Fase 1 — Geolokasi toko + "toko terdekat"
Risiko rendah; hampir seluruhnya meniru pola Customer. Belum menyentuh pencocokan produk.

Langkah (TDD, urutan sesuai `CLAUDE.md`):
1. **Domain** — tambah koordinat ke `Store`: reuse value object `Coordinates`; method `SetLocation(...)` yang me-raise event `StoreLocationUpdated` (past-tense). Tulis tes xUnit dulu (validasi lat/long, idempotensi).
2. **Application** — command `SetStoreLocation` + handler + FluentValidation; query `GetNearestStores(lat, lng, radiusKm, max)` (kontrak repository baru).
3. **Infrastructure** — proyeksikan koordinat ke `StoreReadModel`; migration EF Core (`dotnet ef migrations add AddStoreLocation`); implementasi nearest-store meniru `CustomerQueryRepository.GetNearbyAsync` (bounding-box → Haversine refine).
4. **ApiService** — endpoint set-location di profil toko; endpoint baca `GET .../stores/nearest?lat=&lng=&radiusKm=`.
5. Semua tes hijau sebelum commit.

**Selesai bila:** pemilik toko bisa menaruh pin, dan query mengembalikan toko aktif terdekat terurut jarak.

### Fase 2 — API penemuan lintas-toko
1. Evolusikan `CatalogController` → endpoint discovery: terima `q` + `lat/lng` + `radiusKm`.
2. Query join `ProductReadModel × StoreReadModel` **tanpa store-filter**, kembalikan per listing: nama produk, toko, jarak (dari Fase 1), harga, ketersediaan stok.
3. Pengelompokan awal **barcode-exact** (Tier 1 saja); urut jarak/harga/ketersediaan.
4. Integration test memverifikasi hasil lintas-toko + urutan.

**Selesai bila:** aplikasi konsumen bisa cari produk dan dapat daftar toko terdekat + harga + ketersediaan.

### Fase 3 — Lapisan identitas kanonik
1. Bangun projection `CanonicalProduct` yang mengelompokkan listing setara: **Tier 1** barcode + **Tier 2** nama ternormalisasi (+ `BaseUnit` + kategori).
2. Buat **kamus normalisasi** domain bahan bangunan (sinonim merek, normalisasi satuan/ukuran). Simpan confidence per klaster.
3. Tooling **review/merge manual** untuk mengoreksi/menggabungkan klaster Tier 2.
4. Endpoint discovery (Fase 2) beralih dari grup barcode-exact ke grup identitas kanonik → perbandingan harga lintas-toko untuk barang tanpa barcode.

**Selesai bila:** barang yang sama tapi beda ejaan/tanpa barcode tetap mengelompok jadi satu hasil dengan daftar harga antar toko.

### Fase 4 — Pengerasan & skala
1. **API key + rate limiting** untuk klien eksternal (ganti `[AllowAnonymous]` polos).
2. Indeks geo terdenormalisasi / **PostGIS + GIST** bila jumlah toko/baris besar.
3. Tooling kualitas data GTIN/nama; metrik presisi pencocokan.
4. (Opsional) jalur **opt-out per toko** untuk tampilan publik/harga.

## Keputusan & Risiko yang Harus Diingat

- **Persaingan/privasi:** harga tiap toko jadi publik (sudah diputuskan). Pertimbangkan jalur **opt-out per toko** di kemudian hari bila ada pemilik yang keberatan.
- **Kualitas data:** barang tanpa barcode menurunkan presisi pencocokan → andalkan kamus normalisasi + merge manual; jangan over-promise dedupe otomatis untuk Tier 2.
- **Kesegaran stok:** stok diproyeksikan inline per toko; pembaca lintas-toko melihat state ter-commit, bukan real-time → pertimbangkan label "konfirmasi ke toko" pada hasil.
- **Auth:** `[AllowAnonymous]` cukup untuk satu integrasi internal (Renovin), tidak untuk dibuka publik luas → API key sebelum scale-out.
- **Geo skala:** bounding-box + Haversine memadai untuk awal; naik ke PostGIS/GIST hanya bila jumlah toko/baris besar (catatan upgrade sudah disinggung di `CustomerQueryRepository`).

## Secara Eksplisit DITUNDA / Tidak Dilakukan

- Master catalog dengan governance pusat / pengikatan write-time — **ditolak by design** (toko independen).
- Stok per-varian lintas-toko.
- Streaming event real-time / projection out-of-process (model projection sinkron sekarang sudah cukup).

## Validasi (per fase)

- **TDD** sesuai `CLAUDE.md`: tes xUnit Domain/Application lebih dulu (mis. `DistanceCalculator`/nearest-store untuk Fase 1; aturan klaster kanonik untuk Fase 3).
- **Integration test** endpoint discovery (Fase 2) memverifikasi hasil lintas-toko + urutan jarak/harga.
- Uji manual via Aspire (`dotnet run --project Materia.AppHost`) memanggil endpoint discovery dengan data multi-toko contoh.
