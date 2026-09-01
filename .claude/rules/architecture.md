# Mimari Kuralları

## Katman Sorumlulukları

- **Presentation (Frontend):** UI render, kullanıcı etkileşimi, client-side state. İş mantığı taşımaz.
- **Application (Backend):** İş mantığı, yetkilendirme, validation, orkestrasyon.
- **Domain:** İş kuralları, entity'ler, value object'ler. Framework'ten bağımsız.
- **Infrastructure:** Database, external servisler, dosya sistemi, message queue.

## Bağımlılık Yönü

- Dış katmanlar iç katmanlara bağımlı olabilir, tersi olmaz.
- Domain katmanı hiçbir framework'e bağımlı değildir.
- Infrastructure, domain interface'lerini implement eder.

## Modülerlik

- Her modül tek bir sorumluluk alanına sahip olsun.
- Modüller arası iletişim tanımlı interface veya API üzerinden olsun.
- Circular dependency yasak.
- Bir modülün internals'ı diğer modüllerden gizli olsun.

## Ölçeklenebilirlik

- State'i mümkün olduğunca dışarıda tut (stateless tasarım).
- Horizontal scaling'i engelleyecek singleton veya in-process lock kullanma.
- Cache stratejisini baştan düşün, sonradan eklemek maliyetlidir.

## Karar Kaydı

- Büyük mimari kararlar ADR (Architecture Decision Record) formatında belgelenmeli.
- Kullanıcının onayı olmadan mimari yön değiştirme.

## Framework Seçimi

- Framework seçimi yapılmamıştır. Seçim aşamasında: topluluk, ekosistem, ekip aşinalığı, uzun vadeli sürdürülebilirlik değerlendirilecek.
- Framework lock-in riskini minimize et: domain katmanını framework'ten izole tut.
