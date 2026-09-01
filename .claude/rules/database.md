# Database Kuralları

## Schema Tasarımı

- Database türü (relational/document) mimari aşamasında seçilecek.
- Normalizasyon uygulanacak; gerekçeli denormalizasyon istisna.
- Her tablo/entity birincil anahtara sahip olmalı.
- Soft delete mi hard delete mi mimari aşamasında kararlaştırılacak.

## Migration Disiplini

- Her schema değişikliği migration ile yapılmalı.
- Migration'lar geri alınabilir (reversible) olmalı.
- Breaking change içeren migration'lar önceden belirtilmeli.
- Production'da canlı veri üzerinde DDL çalıştırılmamalı; online migration stratejisi kullanılmalı.
- Migration dosyaları versiyon kontrolünde tutulmalı.

## Query Disiplini

- Raw SQL veya query builder tercihi mimari aşamasında belirlenecek.
- N+1 query yasak. Eager loading veya batch fetch kullanılmalı.
- Index stratejisi sorgu paternlerine göre planlanmalı.
- Transaction'lar minimum gerekli scope'ta tutulmalı.

## Veri Bütünlüğü

- Foreign key, unique constraint, check constraint veritabanı düzeyinde uygulanmalı.
- Application-level validation veritabanı constraint'lerinin yerine geçmez.
- Audit trail gerektiren entity'ler için created_at, updated_at zorunlu.

## Güvenlik

- Connection string veya credential kaynak koda yazılmamalı.
- SQL injection'a karşı parameterized query zorunlu.
- Hassas veriler (şifre, kişisel bilgi) encryption/at-rest stratejisi belirlenmeli.

## Yedekleme

- Backup ve recovery stratejisi production öncesi tanımlanmalı.
